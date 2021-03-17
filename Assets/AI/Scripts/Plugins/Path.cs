//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//[System.Serializable]
//public class Path
//{
//    [SerializeField, HideInInspector]
//    List<Vector3> points;
//    [SerializeField, HideInInspector]
//    bool isClosed; // 闭合
//    [SerializeField, HideInInspector]
//    bool autoSetControllPoints; // 是否自动调整其他点

//    float StepLenth = 3;

//    public Path(Vector3 centre)
//    {
//        // 初始化四个控制点
//        points = new List<Vector3>{
//            centre + Vector3.left * StepLenth,
//            centre + (Vector3.left + Vector3.forward)*StepLenth/2,
//            centre + (Vector3.right + Vector3.back)*StepLenth/2,
//            centre + Vector3.right*StepLenth,
//        };
//    }

//    public int NumPoints { get { return points.Count; } }
//    public int NumSegments { get { return points.Count/3; } }
//    public Vector3 this[int i]
//    {
//        get { return isClosed ? points[LoopIndex(i)] : points[i]; }
//        set {
//            if (isClosed)
//            {
//                points[LoopIndex(i)] = value;
//            }
//            else
//            {
//                points[i] = value;
//            }
//        }
//    }
//    public bool AutoSetControlPoints
//    {
//        get { return autoSetControllPoints; }
//        set
//        {
//            if (autoSetControllPoints != value)
//            {
//                autoSetControllPoints = value;
//                if (autoSetControllPoints)
//                {
//                    AutoSetAllControlPoints();
//                }
//            }
//        }
//    }
//    public bool IsClosed
//    {
//        get { return isClosed; }
//        set
//        {
//            if (isClosed != value)
//            {
//                isClosed = value;
//                ToggleClosed();
//            }
//        }
//    }

//    public Vector3[] GetPointsInSegment(int i)
//    {
//        return new Vector3[]{ points[i*3], points[i*3+1], points[i*3+2], this[i*3+3] };
//    }

//    /// <summary>
//    /// 分割曲线
//    /// </summary>
//    /// <param name="anchorPos">分割的锚点</param>
//    /// <param name="segmentIndex">分割曲线的索引</param>
//    public void SplitSegment(Vector3 anchorPos, int segmentIndex)
//    {
//        points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPos, Vector3.zero });
//        if (autoSetControllPoints)
//        {
//            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
//        }
//        else
//        {
//            AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
//        }
//    }

//    public void DeleteSegment(int anchorIndex)
//    {
//        // 闭合时需要有两端
//        if (NumSegments > 2 || !isClosed && NumSegments > 1)
//        {
//            if (anchorIndex == 0)
//            {
//                if (isClosed)
//                {
//                    points[points.Count - 1] = points[2];
//                }
//                points.RemoveRange(anchorIndex, 3);
//            }
//            else if (anchorIndex == points.Count-1 && !isClosed)
//            {
//                points.RemoveRange(anchorIndex - 3, 3);
//            }
//            else
//            {
//                points.RemoveRange(anchorIndex - 1, 3);
//            }

//            if (autoSetControllPoints)
//            {
//                AutoSetAllAffectedControlPoints(anchorIndex);
//            }
//        }
//    }

//    public void AddSegment(Vector3 anchorPos)
//    {
//        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
//        points.Add((points[points.Count - 1] + anchorPos) / 2);
//        points.Add(anchorPos);
//        if (autoSetControllPoints)
//        {
//            AutoSetAllAffectedControlPoints(points.Count - 1);
//        }
//    }

//    public void ToggleClosed()
//    {
//        // 是否闭合
//        if (isClosed)
//        {
//            // 加入两个控制点
//            points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
//            points.Add(points[0] * 2 - points[1]);
//        }
//        else
//        {
//            // 移除
//            points.RemoveRange(points.Count - 2, 2);
//        }

//    }

//    public void MovePoint(int i, Vector3 pos)
//    {
//        Vector3 detalPos = pos - points[i];
//        // 当设置自动调整控制点时， 只能移动锚点
//        if (i % 3 == 0 || !autoSetControllPoints)
//        {
//            points[i] = pos;
//            if (autoSetControllPoints)
//            {
//                AutoSetAllAffectedControlPoints(i);
//            }
//            else
//            {
//                // 联动
//                if (i % 3 == 0)
//                {
//                    // 控制点
//                    if (IndexInRange(i+1))
//                    {
//                        this[i + 1] += detalPos;
//                    }
//                    if (IndexInRange(i-1))
//                    {
//                        this[i - 1] += detalPos;
//                    }
//                }
//                else
//                {
//                    // 控制杆
//                    bool nextPointIsAnchor = (i + 1) % 3 == 0;
//                    int correspondingControlIndex = nextPointIsAnchor ? (i + 2) : (i - 2);
//                    int anchorIndex = nextPointIsAnchor ? (i + 1) : (i - 1);

//                    if (IndexInRange(correspondingControlIndex))
//                    {
//                        float dst = (this[anchorIndex] - this[correspondingControlIndex]).magnitude;
//                        Vector3 dir = (this[anchorIndex] - pos).normalized;

//                        this[correspondingControlIndex] = this[anchorIndex] + dst * dir;
//                    }
//                }
//            }
//        }
//    }

//    void AutoSetAllAffectedControlPoints(int updateAnchorIndex)
//    {
//        // 当一点发生更改时  影响前后两个锚点
//        for (int i = updateAnchorIndex - 3; i < updateAnchorIndex+3; i+=3)
//        {
//            if (i >= 0 && i < points.Count)
//            {
//                AutoSetAnchorControlPoints(updateAnchorIndex);
//            }
//        }

//        AutoSetStartAndEndControls();
//    }

//    void AutoSetAllControlPoints()
//    {
//        for (int i = 0; i < points.Count; i+=3)
//        {
//            AutoSetAnchorControlPoints(i);
//        }
//        AutoSetStartAndEndControls();
//    }

//    void AutoSetAnchorControlPoints(int anchorIndex)
//    {
//        float[] neighhourDistance = new float[2]; // 相邻两点的长度
//        Vector3 dir = Vector3.zero; // 控制杆的方向
//        // 得到前一个控制点
//        if (IndexInRange(anchorIndex - 3))
//        {
//            Vector3 offset = this[anchorIndex - 3] - this[anchorIndex];
//            dir += offset.normalized;
//            neighhourDistance[0] = offset.magnitude;
//        }

//        // 得到后一个控制点
//        if (IndexInRange(anchorIndex + 3))
//        {
//            Vector3 offset = this[anchorIndex + 3] - this[anchorIndex];
//            dir -= offset.normalized;
//            neighhourDistance[1] = -offset.magnitude;
//        }

//        dir.Normalize();

//        // 计算锚点的前后控制点
//        for (int i = 0; i < 2; i++)
//        {
//            int controlIndex = anchorIndex + i * 2 - 1;
//            if (IndexInRange(controlIndex))
//            {
//                this[controlIndex] = this[anchorIndex] + dir * neighhourDistance[i] * 0.5f;
//            }
//        }
//    }

//    void AutoSetStartAndEndControls()
//    {
//        if (!isClosed)
//        {
//            // 设置起始锚点的控制点
//            points[1] = (points[0] + points[2]) * 0.5f;
//            points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * 0.5f;
//        }
//    }

//    int LoopIndex(int i)
//    {
//        return (i + points.Count) % points.Count;
//    }

//    bool IndexInRange(int i)
//    {
//        return i >= 0 && i < points.Count || isClosed;
//    }
//}
