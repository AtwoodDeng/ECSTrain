using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace ESCAgent
{
    [RequiresEntityConversion]
    public class AgentESCAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public AgentESCLocalData data;
        // Lets you convert the editor data representation to the entity optimal runtime representation
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new AgentESC
            {
                type = (int)data.type,
                team = (int)data.type,
                moveSpeed = data.moveSpeed,
                health = data.health,
                attack = data.attack,
                attackRadius = data.attackRadius,
                navigationTimer = 0,
                collisionRadius = data.collideRadius,
            });

            //var local = dstManager.GetComponentData<LocalToWorld>(entity);
            //Debug.Log("Local in Convert" + local.Position);
        }
    }

}
