using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ESCAgent
{
    [SerializeField]
    public struct TeamData : IComponentData
    {
        public int team;
    }
}