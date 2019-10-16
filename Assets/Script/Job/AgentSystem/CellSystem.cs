using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

namespace ESCAgent
{
    [UpdateAfter(typeof(AgentSpawnerSystem))]
    public class CellSystem : JobComponentSystem
    {

        private EntityQuery m_AgentQuery;


        private List<NativeMultiHashMap<int, int>> m_PrevFrameHashmaps = new List<NativeMultiHashMap<int, int>>();



        // Populates a hash map, where each bucket contains the indices of all Boids whose positions quantize
        // to the same value for a given cell radius so that the information can be randomly accessed by
        // the `MergeCells` and `Steer` jobs.
        [BurstCompile]
        [RequireComponentTag(typeof(AgentESC))]
        struct HashPositions : IJobForEachWithEntity<LocalToWorld>
        {
            public NativeMultiHashMap<int, int>.ParallelWriter hashMap;
            public float cellRadius;

            public NativeArray<float3> cellPositions;

            public void Execute(Entity entity, int index, [ReadOnly]ref LocalToWorld localToWorld)
            {
                var hash = (int)math.hash(new int3(math.floor(localToWorld.Position / cellRadius)));
                cellPositions[index] = localToWorld.Position;

                hashMap.Add(hash, index);
            }
        }

        // This accumulates the `positions` (separations) and `headings` (alignments) of all the Boids in each cell
        // in order to do the following:
        // 1) count the number of Boids in each cell
        // 2) find the nearest obstacle and target to each boid cell
        // 3) track which array entry contains the accumulated values for each Boid's cell
        [BurstCompile]
        struct MergeCells : IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            public NativeArray<int> cellIndices;
            public NativeArray<int> cellCount;
            public NativeArray<int> nextIndex;
            public NativeArray<int> lastIterateIndex;

            void NearestPosition(NativeArray<float3> targets, float3 position, out int nearestPositionIndex, out float nearestDistance)
            {
                nearestPositionIndex = 0;
                nearestDistance = math.lengthsq(position - targets[0]);
                for (int i = 1; i < targets.Length; i++)
                {
                    var targetPosition = targets[i];
                    var distance = math.lengthsq(position - targetPosition);
                    var nearest = distance < nearestDistance;

                    nearestDistance = math.select(nearestDistance, distance, nearest);
                    nearestPositionIndex = math.select(nearestPositionIndex, i, nearest);
                }
                nearestDistance = math.sqrt(nearestDistance);
            }

            // Resolves the distance of the nearest obstacle and target and stores the cell index.
            public void ExecuteFirst(int index)
            {
                nextIndex[index] = -2;
                lastIterateIndex[index] = index;
                cellIndices[index] = index;
                cellCount[index] = 1;
            }

            // Sums the alignment and separation of the actual index being considered and stores
            // the index of this first value where we're storing the cells.
            public void ExecuteNext(int cellIndex, int index)
            {
                cellCount[cellIndex] += 1;
                cellIndices[index] = cellIndex;
                nextIndex[index] = lastIterateIndex[cellIndex];
                lastIterateIndex[cellIndex] = index;

            }
        }

        [BurstCompile]
        struct SetupCellDataJob : IJobForEachWithEntity<CellData>
        {
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<int> cellIndices;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<int> cellCount;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<int> nextIndex;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<float3> cellPositions;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<int> lastIterateIndex;

            public void Execute(Entity entity, int index, ref CellData cell)
            {

                cell.cellIndex = cellIndices[index];
                cell.headIndex = lastIterateIndex[cell.cellIndex];
                cell.nextIndex = nextIndex[index];
                cell.cellCount = cellCount[cell.cellIndex];
            }

        }

        protected override void OnStopRunning()
        {
            for (var i = 0; i < m_PrevFrameHashmaps.Count; ++i)
            {
                m_PrevFrameHashmaps[i].Dispose();
            }
            m_PrevFrameHashmaps.Clear();
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var cellRadius = 1f;
            var agentCount = m_AgentQuery.CalculateEntityCount();

            // Cannot call [DeallocateOnJobCompletion] on Hashmaps yet, so doing own cleanup here
            // of the hashes created in the previous iteration.
            for (int i = 0; i < m_PrevFrameHashmaps.Count; ++i)
            {
                m_PrevFrameHashmaps[i].Dispose();
            }
            m_PrevFrameHashmaps.Clear();
            
            var hashMap = new NativeMultiHashMap<int, int>(agentCount, Allocator.TempJob);

            var cellIndices = new NativeArray<int>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellCount = new NativeArray<int>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var nextIndex = new NativeArray<int>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var lastIterateIndex = new NativeArray<int>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var cellPosition = new NativeArray<float3>(agentCount
                , Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            // Cannot call [DeallocateOnJobCompletion] on Hashmaps yet, so adding resolved hashes to the list
            // so that theyre usable in the upcoming cell jobs and also have a straight forward cleanup.
            m_PrevFrameHashmaps.Add(hashMap);
            
            // ======================= Set up Hash Map ==================================

            var hashPositionsJob = new HashPositions
            {
                hashMap = hashMap.AsParallelWriter(),
                cellPositions = cellPosition,
                cellRadius = cellRadius
            };
            var hashPositionsJobHandle = hashPositionsJob.Schedule(m_AgentQuery, inputDeps);
            
            var initialCellCountJob = new MemsetNativeArray<int>
            {
                Source = cellCount,
                Value = 1
            };
            var initialCellCountJobHandle = initialCellCountJob.Schedule(agentCount, 64, inputDeps);
            

            var initialCellBarrierJobHandle = JobHandle.CombineDependencies(hashPositionsJobHandle, initialCellCountJobHandle);

            var mergeCellsJob = new MergeCells
            {
                cellIndices = cellIndices,
                cellCount = cellCount,
                nextIndex = nextIndex,
                lastIterateIndex = lastIterateIndex,
            };
            var mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap, 64, initialCellBarrierJobHandle);


            var setupCellJob = new SetupCellDataJob()
            {
                cellIndices = cellIndices,
                cellCount = cellCount,
                nextIndex = nextIndex,
                cellPositions = cellPosition,
                lastIterateIndex = lastIterateIndex,
            };
            var setupCellJobHandle = setupCellJob.Schedule(m_AgentQuery, mergeCellsJobHandle);

            m_AgentQuery.AddDependency(inputDeps);

            return setupCellJobHandle;

        }


        protected override void OnCreate()
        {
            m_AgentQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<AgentESC>(), ComponentType.ReadOnly<CellData>(), ComponentType.ReadOnly<LocalToWorld>() },
            });
        }

    }

}