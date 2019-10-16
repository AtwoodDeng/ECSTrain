using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ESCAgent
{
    [System.Serializable]
    public struct AgentSpawner : IComponentData
    {
        public Entity prefab;
        public int prefabType;

        public float radius;
        public float rate;
        public float counter;

        public int team;
    }

    /*namespace Authoring
    {
        public class AgentSpawnerAuthoring : MonoBehaviour
        {
            [System.Serializable]
            public struct AgentSpawnerData
            { 
                public GameObject prefab;
                public float radius;
                public float rate;

            }

            public List<AgentSpawnerData> agentList;

            public int index;

            void Start()
            {
                SwitchAgentTo(agentList[0]);
            }

            void Update()
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Debug.Log("Change input to " + agentList[index].prefab.name );

                    SwitchAgentTo(agentList[index]);

                    index = (index + 1) % agentList.Count;
                }
            }

            public void SwitchAgentTo(AgentSpawnerData data)
            {
                var entityManager = World.Active.EntityManager;

                var target = entityManager.CreateEntity();
                entityManager.SetComponentData(target, new Translation {Value = transform.position});
                var spawnerData = new AgentSpawner
                {
                    prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(data.prefab, World.Active),
                    radius = data.radius,
                    rate = data.rate
                };
                entityManager.AddComponentData(target,spawnerData);
            }
        }
    }
    */
}