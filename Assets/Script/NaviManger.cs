using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NaviManger : MonoBehaviour
{
    static public NaviManger Instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = FindObjectOfType<NaviManger>();
            return m_Instance;
        }
    }
    static private NaviManger m_Instance;

    public bool UpdateNaviMeshPath(BaseAgent agent)
    {
        
        // now just do the normal navigation 
        return NavMesh.CalculatePath(agent.GetTemPosition(), agent.GetDestination(), NavMesh.AllAreas, agent.navPath);
        
    }
}
