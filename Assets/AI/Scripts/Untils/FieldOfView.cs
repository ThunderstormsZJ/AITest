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
    public float MeshResolution = 1;
    public int EdgeResolveIterations = 10;
    public float EdgeDstThreshold = 0.5f;
    public Material ViewMaterial;
    public LayerMask TargetMask; // 目标
    public LayerMask ObstacleMask; // 障碍物
    public LayerMask WallMask; // 墙壁

    [HideInInspector]
    public List<Transform> VisibleTargets = new List<Transform>();

    Mesh viewMesh;

    void Start()
    {
#if UNITY_EDITOR
        CreateViewMesh();
#endif

        StartCoroutine(FindWithDelay(.2f));
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

    IEnumerator FindWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            //FindVisibleTargets();
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
            if (CheckInView(target, ObstacleMask))
            {
                VisibleTargets.Add(target);
            }
        }
    }

    /// <summary>
    /// 检测目标是否在视野范围内
    /// 检测中间是否有障碍物
    /// </summary>
    /// <param name="target">目标物体</param>
    /// <param name="ObstacleMask">障碍物体</param>
    /// <returns>bool</returns>
    bool CheckInView(Transform target, int ObstaclesMask=-1)
    {
        Vector3 vecToTarget = target.position - transform.position;
        Vector3 dirToTarget = vecToTarget.normalized;
        if (Vector3.Angle(transform.forward, dirToTarget) < ViewAngle / 2)
        {
            if(ObstaclesMask >= 0)
            {
                if (!Physics.Raycast(transform.position, dirToTarget, vecToTarget.magnitude, ObstaclesMask)){
                    return true;
                }

                return false;
            }
            return true;
        }
        return false;
    }

    // 根据扇形区域获取所有碰撞点
    public List<ViewCastInfo> GetAllViewCastInfo(int viewMask, bool isHitPoint = false)
    {
        List<ViewCastInfo> viewCastInfoList = new List<ViewCastInfo>();
        int stepCount = Mathf.RoundToInt(ViewAngle * MeshResolution);
        if (stepCount == 0) return viewCastInfoList;
        float stepAngleSize = ViewAngle / stepCount;

        ViewCastInfo oldCastInfo = new ViewCastInfo();
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - ViewAngle / 2 + i * stepAngleSize;
            ViewCastInfo newCastInfo = ViewCast(angle, viewMask);

            if (i > 0)
            {
                // 计算边界的点
                // 两点之间距离超过阈值也加入检测
                bool edgeDstThresholdExceed = Mathf.Abs(oldCastInfo.dst - newCastInfo.dst) > EdgeDstThreshold;
                if (oldCastInfo.hit != newCastInfo.hit || (oldCastInfo.hit && newCastInfo.hit && edgeDstThresholdExceed))
                {
                    // 只在有障碍物和无障碍物之间的点检测
                    EdgetInfo edgetInfo = FindEdget(oldCastInfo, newCastInfo, viewMask);
                    if (edgetInfo.minCastInfo.pointer != Vector3.zero)
                    {
                        viewCastInfoList.Add(edgetInfo.minCastInfo);
                        Debug.DrawLine(transform.position, edgetInfo.minCastInfo.pointer, Color.black);
                    }
                    if (edgetInfo.maxCastInfo.pointer != Vector3.zero)
                    {
                        viewCastInfoList.Add(edgetInfo.maxCastInfo);
                        Debug.DrawLine(transform.position, edgetInfo.maxCastInfo.pointer, Color.black);
                    }
                }
            }

            if (!isHitPoint || newCastInfo.hit)
            {
                viewCastInfoList.Add(newCastInfo);
            }
            oldCastInfo = newCastInfo;

            Debug.DrawLine(transform.position, newCastInfo.pointer, Color.black);
        }

        return viewCastInfoList;
    }

    // 画网格
    void DrawFieldOfView()
    {
        List<ViewCastInfo> viewCastInfoList = GetAllViewCastInfo(ObstacleMask | WallMask);
        int vertexCount = viewCastInfoList.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount]; // 绘制Mesh顶点集合
        int[] triangles = new int[(vertexCount - 2) * 3]; // 绘制Mesh的三角形信息

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewCastInfoList[i].pointer);
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
    ViewCastInfo ViewCast(float globalAngle, int viewMask)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;

        if(Physics.Raycast(transform.position, dir, out hit, ViewRadius, viewMask)){
            return new ViewCastInfo(true, hit.point, hit.normal, hit.distance, globalAngle);
        }else{
            return new ViewCastInfo(false, transform.position + dir * ViewRadius, hit.normal, hit.distance, globalAngle);
        }
    }

    /// <summary>
    /// 找到障碍物与视野相交的边界
    /// </summary>
    /// <param name="minCastInfo">最小点的信息</param>
    /// <param name="maxCastInfo">最大点的信息</param>
    /// <returns></returns>
    EdgetInfo FindEdget(ViewCastInfo minCastInfo, ViewCastInfo maxCastInfo, int viewMask)
    {
        float minAngle = minCastInfo.angle;
        float maxAngle = maxCastInfo.angle;
        ViewCastInfo minVCI = new ViewCastInfo();
        ViewCastInfo maxVCI = new ViewCastInfo();

        for (int i = 0; i < EdgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo castInfo = ViewCast(angle, viewMask);
            bool edgeDstThresholdExceed = Mathf.Abs(castInfo.dst - maxCastInfo.dst) > EdgeDstThreshold;
            // 接触到障碍物的为最低点
            if (castInfo.hit == minCastInfo.hit && !edgeDstThresholdExceed)
            {
                minAngle = castInfo.angle;
                minVCI = castInfo;
            }
            else
            {
                maxAngle = castInfo.angle;
                maxVCI = castInfo;
            }
        }

        return new EdgetInfo(minVCI, maxVCI);
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
        public Vector3 normal;
        public float dst;
        public float angle;

        public ViewCastInfo(bool hit, Vector3 pointer, Vector3 normal, float dst, float angle)
        {
            this.hit = hit;
            this.pointer = pointer;
            this.normal = normal;
            this.dst = dst;
            this.angle = angle;
        }
    }

    // 边界信息
    public struct EdgetInfo
    {
        public ViewCastInfo minCastInfo;
        public ViewCastInfo maxCastInfo;

        public EdgetInfo(ViewCastInfo minCastInfo, ViewCastInfo maxCastInfo)
        {
            this.minCastInfo = minCastInfo;
            this.maxCastInfo = maxCastInfo;
        }

        public override string ToString()
        {
            return minCastInfo.pointer + "  " + maxCastInfo.pointer;
        }
    }
}
