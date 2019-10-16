using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ESCAgent
{
    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    public struct AgentESC : IComponentData
    {
        public int type;
        public int team;
        public float moveSpeed;
        public float health;
        public float attack;
        public float collisionRadius;
        public float attackRadius;

        public float navigationTimer;

    }



}