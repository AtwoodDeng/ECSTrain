using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentTest : MonoBehaviour
{
    public Transform target;
    private NavMeshPath path;

    public float interval;
    public float timer;
    public float speed = 5f;
    public bool hasPath = false;

    void Start()
    {
        path = new NavMeshPath();
        timer = 0;

    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer > 0 )
        {
            
            timer -= 1f;
            hasPath = NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);

            Debug.Log("Calculate " + hasPath );
        }

        for (int i = 0; i < path.corners.Length - 1 ; i++)
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
    }

    void LateUpdate()
    {
        if (hasPath)
        {
            var moveForward = (path.corners[1] - transform.position).normalized;
            transform.forward = moveForward;
            transform.position += moveForward * speed * Time.deltaTime;
        }

    }

}
