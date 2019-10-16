using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ESCAgent
{

    public class AgentSpawnerAuthoring : MonoBehaviour , IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public AgentSpawnerData data;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.SetComponentData(entity, new LocalToWorld
            {
                Value = float4x4.TRS(
                    transform.position,
                    transform.rotation,
                    transform.lossyScale
                )

            });
            dstManager.AddComponentData(entity,new AgentSpawner
            {
                prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(data.prefab,World.Active),
                radius = data.radius,
                rate = data.rate,
                team = data.team,
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
        }
    }
}