using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ESCAgent
{
    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    public struct CellData : IComponentData
    {
        public int cellIndex; // cell index ( tail index ) of the link
        public int headIndex; // head index of the link
        public int nextIndex; // next index of the link
        public int cellCount;

        // for test 
        public float3 forward;

    }
}