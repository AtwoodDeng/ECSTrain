using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BaseAgent : MonoBehaviour
{
    [HideInInspector]
    public NavMeshPath navPath;

    public void Init()
    {
        navPath = new NavMeshPath();
    }
    virtual public Vector3 GetTemPosition()
    {
        return transform.localPosition ; // use local Position to avoid additional calculation 

    }

    virtual public Vector3 GetDestination()
    {
        return Vector3.zero;
    }

    
}
