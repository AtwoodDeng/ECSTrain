using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ESCAgent
{
    [Serializable]
    public struct TargetSpot : IComponentData
    {
        public int team;
    }

    [DisallowMultipleComponent]
    public class TargetSpotProxy : ComponentDataProxy<TargetSpot> { }

}