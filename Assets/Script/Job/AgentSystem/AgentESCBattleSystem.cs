using System.IO;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ESCAgent
{
    [UpdateAfter(typeof(AgentESCMoveSystem))]
    public class AgentESCBattleSystem : JobComponentSystem
    {
        private EntityQuery m_AgentQuery;

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
        struct CopyAgent : IJobForEachWithEntity<AgentESC, CellData, LocalToWorld>
        {
            public NativeArray<AgentESC> agentList;
            public NativeArray<CellData> cellList;
            public NativeArray<float3> agentPosition;
            public void Execute(Entity entity, int index, ref AgentESC agent, ref CellData cellData , ref LocalToWorld localToWorld)
            {
                agentList[index] = agent;
                cellList[index] = cellData;
                agentPosition[index] = localToWorld.Position;
            }
        }

        [BurstCompile]
        struct BattleJob : IJobForEachWithEntity<AgentESC, CellData, LocalToWorld>
        {
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<AgentESC> agentList;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<CellData> cellList;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<float3> agentPosition;

            [ReadOnly]
            public int agentCount;
            [ReadOnly]
            public float deltaTime;
            public void Execute(Entity entity, int index, ref AgentESC agent, [ReadOnly] ref CellData cellData , ref LocalToWorld localToWorld)
            {
                var iterateIndex = cellData.headIndex;

                while (iterateIndex >= 0)
                {
                    if (agentList[iterateIndex].team != agent.team)
                    {

                        var distance = math.length(agentPosition[iterateIndex] - localToWorld.Position);
                        var threshold = agent.collisionRadius + agentList[iterateIndex].attackRadius;


                        if (distance < threshold)
                        {
                            agent.health -= agentList[iterateIndex].attack * deltaTime;

                            localToWorld = new LocalToWorld
                            {
                                Value = float4x4.TRS(
                                    new float3(localToWorld.Position + (new float3(0, 1f, 0) * deltaTime)),
                                    quaternion.LookRotationSafe(localToWorld.Forward, localToWorld.Up),
                                    new float3(1.0f, 1.0f, 1.0f))
                            };
                        }
                    }

                    iterateIndex = cellList[iterateIndex].nextIndex;
                }


                //for (int i = 0; i < agentCount; ++i)
                //{
                //    if (agentList[i].team != agent.team)
                //    {
                //        var distance = math.length(agentPosition[i] - localToWorld.Position);
                //        var threshold = agent.collisionRadius + agentList[i].attackRadius;

                //        if (distance < threshold)
                //        {
                //            agent.health -= agentList[i].attack * deltaTime;

                //            localToWorld = new LocalToWorld
                //            {
                //                Value = float4x4.TRS(
                //                    new float3(localToWorld.Position + (new float3(0, 1f, 0) * deltaTime)),
                //                    quaternion.LookRotationSafe(localToWorld.Forward, localToWorld.Up),
                //                    new float3(1.0f, 1.0f, 1.0f))
                //            };
                //        }
                //    }
                //}
            }
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var agentCount = m_AgentQuery.CalculateEntityCount();

            var agentArray =
                new NativeArray<AgentESC>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellArray =
                new NativeArray<CellData>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var agentPosition = new NativeArray<float3>(agentCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var copyAgentJob = new CopyAgent
            {
                agentList = agentArray,
                agentPosition = agentPosition,
                cellList = cellArray,

            };
            var copyAgentJobHandle = copyAgentJob.Schedule(m_AgentQuery, inputDeps);

            var battleJob = new BattleJob
            {
                agentList = agentArray,
                agentPosition = agentPosition,
                cellList = cellArray,
                agentCount = agentCount,
                deltaTime = Time.deltaTime,
            };
            var battleJobHandle = battleJob.Schedule(this, copyAgentJobHandle);

            m_AgentQuery.AddDependency(inputDeps);
            return battleJobHandle;
        }



        protected override void OnCreate()
        {
            m_AgentQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<AgentESC>(), ComponentType.ReadOnly<TeamData>(), ComponentType.ReadOnly<CellData>(), ComponentType.ReadOnly<LocalToWorld>() },
            });

        }
    }
}