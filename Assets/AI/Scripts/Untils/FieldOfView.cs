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
    public float MeshResolution = 1;
    public int EdgeResolveIterations = 10;
    public float EdgeDstThreshold = 0.5f;
    public Material ViewMaterial;

    [HideInInspector]
    public List<Transform> VisibleTargets = new List<Transform>();

    Mesh viewMesh;

    void Start()
    {
#if UNITY_EDITOR
        CreateViewMesh();
#endif

        StartCoroutine(FindTargetsWithDelay(.2f));
    }

    void Update()
    {
    }

    void LateUpdate()
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
        if (stepCount == 0) return;
        float stepAngleSize = ViewAngle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();

        ViewCastInfo oldCastInfo = new ViewCastInfo();
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - ViewAngle/2 + i * stepAngleSize;
            ViewCastInfo newCastInfo = ViewCast(angle);

            if (i > 0)
            {
                // 计算边界的点
                // 两点之间距离超过阈值也加入检测
                bool edgeDstThresholdExceed = Mathf.Abs(oldCastInfo.dst - newCastInfo.dst) > EdgeDstThreshold;
                if (oldCastInfo.hit != newCastInfo.hit && (oldCastInfo.hit && newCastInfo.hit && edgeDstThresholdExceed))
                {
                    // 只在有障碍物和无障碍物之间的点检测
                    EdgetInfo edgetInfo = FindEdget(oldCastInfo, newCastInfo);
                    if (edgetInfo.minPointer != Vector3.zero)
                    {
                        viewPoints.Add(edgetInfo.minPointer);
                        Debug.DrawLine(transform.position, edgetInfo.minPointer, Color.black);
                    }
                    if (edgetInfo.maxPointer != Vector3.zero)
                    {
                        viewPoints.Add(edgetInfo.maxPointer);
                        Debug.DrawLine(transform.position, edgetInfo.maxPointer, Color.black);
                    }
                }
            }

            viewPoints.Add(newCastInfo.pointer);
            oldCastInfo = newCastInfo;

            Debug.DrawLine(transform.position, newCastInfo.pointer, Color.black);
        }
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount]; // 绘制Mesh顶点集合
        int[] triangles = new int[(vertexCount - 2) * 3]; // 绘制Mesh的三角形信息

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);
            if (i < vertexCount - 2)
            {
                triangles[i*3] = 0;
                triangles[i*3+1] = i+1;
                triangles[i*3+2] = i+2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    /// <summary>
    /// 计算视野内与障碍物的碰撞点
    /// </summary>
    /// <param name="globalAngle">视野内范围内的角度值</param>
    /// <returns></returns>
    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;

        if(Physics.Raycast(transform.position, dir, out hit, ViewRadius, ObstacleMask)){
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }else{
            return new ViewCastInfo(false, transform.position + dir * ViewRadius, hit.distance, globalAngle);
        }
    }

    /// <summary>
    /// 找到障碍物与视野相交的边界
    /// </summary>
    /// <param name="minCastInfo">最小点的信息</param>
    /// <param name="maxCastInfo">最大点的信息</param>
    /// <returns></returns>
    EdgetInfo FindEdget(ViewCastInfo minCastInfo, ViewCastInfo maxCastInfo)
    {
        float minAngle = minCastInfo.angle;
        float maxAngle = maxCastInfo.angle;
        Vector3 minPointer = Vector3.zero;
        Vector3 maxPointer = Vector3.zero;

        for (int i = 0; i < EdgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo castInfo = ViewCast(angle);
            bool edgeDstThresholdExceed = Mathf.Abs(castInfo.dst - maxCastInfo.dst) > EdgeDstThreshold;
            // 接触到障碍物的为最低点
            if (castInfo.hit == minCastInfo.hit && !edgeDstThresholdExceed)
            {
                minAngle = castInfo.angle;
                minPointer = castInfo.pointer;
            }
            else
            {
                maxAngle = castInfo.angle;
                maxPointer = castInfo.pointer;
            }
        }

        return new EdgetInfo(minPointer, maxPointer);
    }

    // 创建ViewMesh
    void CreateViewMesh()
    {
        GameObject viewMeshObject = new GameObject("ViewMesh");
        viewMeshObject.transform.parent = transform;
        viewMeshObject.transform.localScale = Vector3.one;
        viewMeshObject.transform.localPosition = new Vector3(0, 1, 0);

        MeshRenderer meshRenderer = viewMeshObject.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.material = ViewMaterial;

        MeshFilter meshFilter = viewMeshObject.AddComponent<MeshFilter>();
        viewMesh = new Mesh();
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


    // 碰撞信息
    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 pointer;
        public float dst;
        public float angle;

        public ViewCastInfo(bool hit, Vector3 pointer, float dst, float angle)
        {
            this.hit = hit;
            this.pointer = pointer;
            this.dst = dst;
            this.angle = angle;
        }
    }

    // 边界信息
    public struct EdgetInfo
    {
        public Vector3 minPointer;
        public Vector3 maxPointer;

        public EdgetInfo(Vector3 minPointer, Vector3 maxPointer)
        {
            this.minPointer = minPointer;
            this.maxPointer = maxPointer;
        }

        public override string ToString()
        {
            return minPointer + "  " + maxPointer;
        }
    }
}
