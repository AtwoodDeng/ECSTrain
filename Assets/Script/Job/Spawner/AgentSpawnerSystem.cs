
using Sirenix.OdinInspector.Editor;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ESCAgent
{
    public class AgentSpawnerSystem : ComponentSystem
    {
        private EntityQuery m_AgentQuery;
        
    
        protected override void OnUpdate()
        {
            
            Entities.ForEach((Entity e, ref AgentSpawner spawner, ref LocalToWorld location) =>
            {

                spawner.counter += Time.deltaTime * spawner.rate;

                while (spawner.counter > 1f )
                {
                    var random = new Random(math.asuint(Time.timeSinceLevelLoad * 1000f + 10f));
                    var position = math.transform(location.Value, new float3(random.NextFloat(-1f, 1f), 0, random.NextFloat(-1f, 1f)) * spawner.radius);
                    var newEntity = PostUpdateCommands.Instantiate(spawner.prefab);


                    // updates based on the new heading
                    var localToWorld = new LocalToWorld
                    {
                        Value = float4x4.TRS(
                            new float3(position),
                            quaternion.Euler(0,random.NextFloat(0,360f),0),
                            new float3(1.0f, 1.0f, 1.0f))
                    };

                    PostUpdateCommands.SetComponent(newEntity, localToWorld);

                    var team = new TeamData
                    {
                        team = spawner.team
                    };
                    PostUpdateCommands.AddComponent(newEntity,team);

                    var cell = new CellData
                    {
                        cellCount = 1,
                        headIndex = -1,
                        nextIndex = -1,
                    };
                    PostUpdateCommands.AddComponent(newEntity, cell);

                    spawner.counter -= 1f;
                }
            });
        }
    }
    
}