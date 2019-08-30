using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 视野
public class FieldOfView : MonoBehaviour
{
    public float ViewRadius;
    [Range(0, 360)]
    public float ViewAngle;
    public LayerMask TargetMask; // 目标
    public LayerMask ObstacleMask; // 障碍物
    public float MeshResolution;

    [HideInInspector]
    public List<Transform> VisibleTargets = new List<Transform>();

    //MeshFilter

    void Start()
    {
#if UNITY_EDITOR
        CreateViewMesh();
#endif

        StartCoroutine(FindTargetsWithDelay(.2f));
    }

    void Update()
    {
#if UNITY_EDITOR
        DrawFieldOfView();
#endif
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    // 找到目标
    void FindVisibleTargets()
    {
        VisibleTargets.Clear();
         Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, ViewRadius, TargetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 vecToTarget = target.position - transform.position;
            Vector3 dirToTarget = vecToTarget.normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < ViewAngle / 2)
            {
                // 判断是否在视野范围之内
                if (!Physics.Raycast(transform.position, dirToTarget, vecToTarget.magnitude, ObstacleMask))
                {
                    VisibleTargets.Add(target);
                }
            }
        }
    }

    // 画网格
    void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(ViewAngle * MeshResolution);
        float stepAngleSize = ViewAngle / stepCount;

        //Vector3[] vertices = new Vector3[view];
        for (int i = 0; i < stepCount; i++)
        {
            float angle = transform.eulerAngles.y - ViewAngle/2 + i * stepAngleSize;
            Debug.DrawLine(transform.position, transform.position + DirFromAngle(angle, true) * ViewRadius, Color.red);
        }
    }

    // 创建ViewMesh
    void CreateViewMesh()
    {
        GameObject viewMeshObject = new GameObject("ViewMesh");
        viewMeshObject.transform.parent = transform;
        viewMeshObject.transform.localScale = Vector3.one;
        viewMeshObject.transform.localPosition = Vector3.zero;

        MeshRenderer meshRenderer = viewMeshObject.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        MeshFilter meshFilter = viewMeshObject.AddComponent<MeshFilter>();
        Mesh viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        meshFilter.mesh = viewMesh;
    }

    // 角度转换为方向
    public Vector3 DirFromAngle(float angleInDegree, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegree += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegree * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegree * Mathf.Deg2Rad));
    }
}
