using UnityEngine;

public class CustomAgent : BaseAgent
{
    public void Awake()
    {
        Init();
    }
    public void LateUpdate()
    {
        UpdatePath();
        UpdatePosition();
    }

    #region MOVEMENT

    public Transform destination;
    public float speed = 5f;
    public void UpdatePosition()
    {
        transform.forward = navForward;
        transform.position += navForward * speed * Time.deltaTime;
    }


    #endregion

    #region NAVIGATION



    [HideInInspector]
    public Vector3 navForward;
    [HideInInspector]
    public Vector3 nextCorner;
    [HideInInspector]
    public bool hasPath;

    public float updateNavPathInterval = 1f;

    public override Vector3 GetDestination()
    {
        return destination.localPosition;
    }

    public bool CloseToCorner()
    {
        var distanceToDestination = (nextCorner - GetTemPosition()).magnitude;
        const float threshold = 0.1f;
        return distanceToDestination < threshold;
    }

    protected float updateNavPathTimer = 0;
    virtual public void UpdatePath()
    {
        updateNavPathTimer += Time.deltaTime;
        if (updateNavPathTimer > 0)
        {
            RefreshNavPath();
            updateNavPathTimer -= updateNavPathInterval * Random.Range(0.5f,1.5f);
        }

        if (CloseToCorner())
            RefreshNavPath();

    }

    public void RefreshNavPath()
    {
        hasPath = NaviManger.Instance.UpdateNaviMeshPath(this);

        if (hasPath)
        {
            nextCorner = navPath.corners[1];
            navForward = (nextCorner - navPath.corners[0]).normalized;
        }

        Debug.Log("Refresh Nav Path ");
    }

    #endregion
}
