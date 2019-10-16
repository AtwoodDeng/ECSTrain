using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;

namespace ESCAgent
{

    [System.Serializable]
    public class AgentSpawnerData
    {
        public GameObject prefab;
        public AgentType type;
        public AgentESCLocalData data;
        public float radius;
        public float rate;
        public int team;
        public Transform trans;
    }

    [System.Serializable]
    public class AgentTargetData
    {
        public int team;
        public Transform trans;
    }
    public enum AgentType
    {
        None = 0,
        Blue = 1,
        Red = 2,
    }

    public class AgentSpawnerController : MonoBehaviour
    {
        public List<AgentSpawnerData> agentList;
        public List<AgentTargetData> targetList;
        //public List<GameObject> agentPrefabList;

        public List<SwitchableSpwaner> m_spwaners;
        public List<AgentTarget> m_targets;

        public int index;
        
        void Start()
        {
            Init();
        }

        public void Init()
        {
            m_spwaners = new List<SwitchableSpwaner>();
            for (int i = 0; i < agentList.Count; i++)
            {
                var spwaner = new SwitchableSpwaner();
                spwaner.Init(agentList[i].trans,agentList[i]);
                
                m_spwaners.Add(spwaner);
            }
            m_targets = new List<AgentTarget>();
            for (int i = 0; i < targetList.Count; i++)
            {
                var target = new AgentTarget();
                target.Init(targetList[i].trans, targetList[i]);

                m_targets.Add(target);
            }
        }

        void Update()
        {
            
            //if (Input.GetKeyDown(KeyCode.Space))
            //{
            //    Debug.Log("Change input to " + agentList[index].prefab.name);
                
            //    index = (index + 1) % agentList.Count;
            //}
        }

        void OnGUI()
        {
            GUI.color = Color.yellow;
            GUILayout.Label("Entity Count" + World.Active.EntityManager.GetAllEntities().Length);
        }


        public class SwitchableSpwaner
        {
            public Entity spawner;
            public Transform trans;

            public void Init(Transform _trans, AgentSpawnerData data)
            {
                this.trans = _trans;

                var entityManager = World.Active.EntityManager;
                spawner = entityManager.CreateEntity();
                entityManager.SetName(spawner, "AgentSpawner " + data.team + " | " + data.prefab.name );

                var localToWorld = new LocalToWorld
                {
                    Value = float4x4.TRS(
                        trans.position,
                        trans.rotation,
                        trans.lossyScale
                    )
                };
                entityManager.AddComponentData(spawner, localToWorld);

                var spawnerData = new AgentSpawner
                {
                    prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(data.prefab, World.Active),
                    //prefabType = (int)data.type,
                    radius = data.radius,
                    rate = data.rate
                };
                entityManager.AddComponentData(spawner, spawnerData);

            }

            public void SwitchAgentTo(AgentSpawnerData data)
            {

                var entityManager = World.Active.EntityManager;
                entityManager.RemoveComponent<AgentSpawner>(spawner);

                var spawnerData = new AgentSpawner
                {
                    prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(data.prefab, World.Active),
                    radius = data.radius,
                    rate = data.rate
                };
                entityManager.AddComponentData(spawner, spawnerData);

            }


        }

        public class AgentTarget
        {
            public Entity target;
            public Transform trans;

            public void Init( Transform _trans , AgentTargetData data )
            {
                this.trans = _trans;

                var entityManager = World.Active.EntityManager;
                target = entityManager.CreateEntity();
                entityManager.SetName(target, "AgentTarget " + data.team );

                var localToWorld = new LocalToWorld
                {
                    Value = float4x4.TRS(
                        trans.position,
                        trans.rotation,
                        trans.lossyScale
                    )
                };
                entityManager.AddComponentData(target, localToWorld);

                var targetSpot = new TargetSpot
                {
                    team = data.team
                };
                entityManager.AddComponentData(target, targetSpot);
                
            }
            


        }

    }
}