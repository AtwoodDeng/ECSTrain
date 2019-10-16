using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ESCAgent
{


    [CreateAssetMenu(fileName = "AgentData", menuName = "Agent/AgentData" ,order =  1)]
    public class AgentESCLocalData : ScriptableObject
    {
        public enum Type
        {
            None,
            Red,
            Blue,
            Green,
        }
        public Type type;
        public float moveSpeed;
        public float health;
        public float attack;

        public float collideRadius;
        public float attackRadius;
    }

}