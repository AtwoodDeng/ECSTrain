
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace ESCAgent
{
    [UpdateAfter(typeof(AgentESCBattleSystem))]
    public class AgentESCLifeSystem : JobComponentSystem
    {

        EntityCommandBufferSystem m_Barrier;
        private EntityQuery m_AgentQuery;

        [BurstCompile]
        struct CheckDeath : IJobForEachWithEntity<AgentESC>
        {

            [WriteOnly]
            public EntityCommandBuffer.Concurrent CommandBuffer;
            public void Execute(Entity entity, int index, [ReadOnly]ref AgentESC agent)
            {
                if (agent.health < 0)
                {
                    CommandBuffer.DestroyEntity(index, entity);
                }
            }
        }
    
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var agentCount = m_AgentQuery.CalculateEntityCount();

            var commandBuffer = m_Barrier.CreateCommandBuffer().ToConcurrent();

            var checkDeathJob = new AgentESCLifeSystem.CheckDeath
            {
                CommandBuffer = commandBuffer,
            };
            var checkDeathJobHandle = checkDeathJob.Schedule(m_AgentQuery, inputDeps);
            
            m_Barrier.AddJobHandleForProducer(checkDeathJobHandle);

            return checkDeathJobHandle;
        }

        protected override void OnCreate()
        {
            m_AgentQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<AgentESC>(), ComponentType.ReadOnly<TeamData>(), ComponentType.ReadOnly<LocalToWorld>() },
            });
            m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        }

    }
}