using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace ESCAgent
{
    [UpdateAfter(typeof(CellSystem))]
    public class AgentESCMoveSystem : JobComponentSystem
    {

        private EntityQuery m_TargetQuery;
        private EntityQuery m_AgentQuery;

        static public NavMeshPath navPath;

        [BurstCompile]
        struct CopyPositions : IJobForEachWithEntity<LocalToWorld>
        {
            public NativeArray<float3> positions;

            public void Execute(Entity entity, int index, [ReadOnly]ref LocalToWorld localToWorld)
            {
                positions[index] = localToWorld.Position;
            }
        }



        [BurstCompile]
        struct CopyTarget : IJobForEachWithEntity<TargetSpot>
        {
            public NativeArray<TargetSpot> targets;

            public void Execute(Entity entity, int index, ref TargetSpot spot)
            {
                targets[index] = spot;
            }
        }

        [BurstCompile]
        struct CopyCell : IJobForEachWithEntity<CellData>
        {
            public NativeArray<CellData> cellArray;

            public void Execute(Entity entity, int index, ref CellData cell)
            {
                cellArray[index] = cell;
            }
        }
        [BurstCompile]
        struct SetTarget : IJobForEachWithEntity<AgentESC>
        {
            [ReadOnly]
            public NativeArray<TargetSpot> targetData;
            [ReadOnly]
            public NativeArray<float3> targetPosition;

            public NativeArray<int> targetID;

            public void Execute(Entity entity, int index, ref AgentESC agent )
            {
                for (int i = 0; i < targetData.Length; ++i)
                {
                    // TODO : set up the target position based on the team ID 
                    targetID[index] = targetData[i].team != agent.team ? targetID[index] : i;
                }
            }
        }

        [BurstCompile]
        struct CalculateForward : IJobForEachWithEntity<CellData>
        {
            [ReadOnly] public NativeArray<int> cellTargetPositionIndex;

            [ReadOnly]
            public NativeArray<float3> agentPositions;

            public float cellRadius;
            
            [ReadOnly]
            public NativeArray<float3> targetPositions;
            
            public void Execute(Entity entity, int index, ref CellData agent)
            {
                if (agent.cellIndex == index ) // head index
                {
                    var pos = math.floor(agentPositions[index] / cellRadius ) * cellRadius;
                    var target = targetPositions[cellTargetPositionIndex[index]];

                    if (NavMesh.CalculatePath(pos, target, NavMesh.AllAreas, AgentESCMoveSystem.navPath))
                    {
                        agent.forward = (navPath.corners[1] - navPath.corners[0]).normalized;
                    }
                    
                }
            }

        }


        [BurstCompile]
        struct SetupCellData : IJobForEachWithEntity<CellData>
        {
            [ReadOnly]
            public NativeArray<CellData> cellArray;
            
            public void Execute(Entity entity, int index, ref CellData agent)
            {
                var headIndex = agent.cellIndex;
                agent.forward = cellArray[headIndex].forward;
            }

        }


        [BurstCompile]
        struct MoveJob : IJobForEachWithEntity<AgentESC, LocalToWorld>
        {
            public float dt;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<float3> targetPositions;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<TargetSpot> targetData;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<int> cellTargetPositionIndex;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<float3> positionArray;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<CellData> cellArray;
            
            public void Execute(Entity entity, int index, ref AgentESC agent, ref LocalToWorld localToWorld)
            {
                var pos = positionArray[index];

                
                var forward = cellArray[index].forward;
                
                var iterateIndex = cellArray[index].headIndex;
                var nearestDistance = 999f;
                float3 nearestPos = new float3(0, 0, 0);
                while (iterateIndex >= 0)
                {
                    var distance = math.length(positionArray[iterateIndex] - localToWorld.Position);

                    if (distance < nearestDistance && distance > math.FLT_MIN_NORMAL )
                    {
                        nearestDistance = distance;
                        nearestPos = new float3(0,0,0);
                    }

                    iterateIndex = cellArray[iterateIndex].nextIndex;
                }

                var offset = (nearestPos - pos)/nearestDistance;

                forward = math.normalize(math.lerp(forward, offset, nearestDistance > 998f? 0 : 0.5f ));

                localToWorld = new LocalToWorld
                {
                    Value = float4x4.TRS(
                        new float3(localToWorld.Position + (forward * agent.moveSpeed * dt)),
                        quaternion.LookRotationSafe(forward, math.up()),
                        new float3(1.0f, 1.0f, 1.0f))
                };


                //var forward = math.normalize(targetPos - localToWorld.Position);

                //// updates based on the new heading
                //localToWorld = new LocalToWorld
                //{
                //    Value = float4x4.TRS(
                //        new float3(localToWorld.Position + (forward * agent.moveSpeed * dt)),
                //        quaternion.LookRotationSafe(forward, math.up()),
                //        new float3(1.0f, 1.0f, 1.0f))
                //};
            }


        }
        


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // set up array
            var targetCount = m_TargetQuery.CalculateEntityCount();
            var agentCount = m_AgentQuery.CalculateEntityCount();


            // ===================== Set up ==========================
            var cellTargetPositionIndex = new NativeArray<int>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var copyTargetPositions = new NativeArray<float3>(targetCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var targetData =
                new NativeArray<TargetSpot>(targetCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellArray =
                new NativeArray<CellData>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var positionArray =
                new NativeArray<float3>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);


            // ======================= Set up m_targets =================================
            var copyTargetPositionsJob = new CopyPositions
            {
                positions = copyTargetPositions
            };
            var copyTargetPositionsJobHandle = copyTargetPositionsJob.Schedule(m_TargetQuery, inputDeps);

            var copyPositionsJob = new CopyPositions
            {
                positions = positionArray
            };
            var copyPositionsJobHandle = copyPositionsJob.Schedule(m_AgentQuery, inputDeps);

            var copyTargetJob = new CopyTarget
            {
                targets = targetData
            };
            var copyTargetJobHandle = copyTargetJob.Schedule(m_TargetQuery, inputDeps);

            var copyCellJob = new CopyCell
            {
                cellArray = cellArray
            };
            var copyCellJobHandle = copyCellJob.Schedule(m_AgentQuery, inputDeps);

            var initTargetJob = JobHandle.CombineDependencies(copyTargetPositionsJobHandle, copyTargetJobHandle, copyCellJobHandle);

            var initJob = JobHandle.CombineDependencies(initTargetJob,copyPositionsJobHandle);
            
            // ======================= Set up Target ===================================
            var setTargetJob = new SetTarget
            {
                targetID = cellTargetPositionIndex,
                targetData = targetData,
                targetPosition = copyTargetPositions,
                
            };
            var setTargetJobHandle = setTargetJob.Schedule(m_AgentQuery, initJob);
            // ===================================================================================


            var calculateForwardJob = new CalculateForward
            {
                cellTargetPositionIndex = cellTargetPositionIndex,
                agentPositions = positionArray,
                cellRadius = 1f,
                targetPositions = copyTargetPositions,
            };
            var calculateForwardJobHandle = calculateForwardJob.Schedule(m_AgentQuery, setTargetJobHandle);


            var setupCellJob = new SetupCellData
            {
                cellArray = cellArray,
            };
            var setupCellJobHandle = setupCellJob.Schedule(m_AgentQuery, calculateForwardJobHandle);

            // ========================== Move =============================================

            var moveBarrier = JobHandle.CombineDependencies(setTargetJobHandle, setupCellJobHandle);

            var moveJobHandle = new MoveJob()
            {
                dt = Time.deltaTime,
                targetPositions = copyTargetPositions,
                targetData = targetData,
                cellTargetPositionIndex = cellTargetPositionIndex,
                cellArray = cellArray,
                positionArray = positionArray,
            }.Schedule(this, moveBarrier);
            // ===========================================================================
            
            m_AgentQuery.AddDependency(inputDeps);

            return moveJobHandle;
        }

        protected override void OnCreate()
        {

            m_TargetQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<TargetSpot>(), ComponentType.ReadOnly<LocalToWorld>() },
            });


            m_AgentQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<AgentESC>(), ComponentType.ReadOnly<TeamData>(),ComponentType.ReadOnly<CellData>(),
                    ComponentType.ReadOnly<LocalToWorld>() },
            });

            navPath = new NavMeshPath();
        }
    }
}