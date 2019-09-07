//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;

//[CustomEditor(typeof(PathCreator))]
//public class PathEditor : Editor
//{
//    PathCreator creator;
//    Path Path {
//        get { return creator.path; }
//    }

//    float segmentSelectedDistanceThrehold = 0.5f; // 可以选择到线段的最小阈值
//    float anchorSelectedDistanceThrehold = 0.5f; // 可以选择到锚点的最小阈值
//    int selectedSegmentIndex = -1; // 当前选择的分段

//    public override void OnInspectorGUI()
//    {
//        base.OnInspectorGUI();

//        EditorGUI.BeginChangeCheck();
//        // 创新创建
//        if (GUILayout.Button("Create New"))
//        {
//            Undo.RecordObject(creator, "Create New");
//            creator.CreatePath();
//        }

//        // 是否闭合
//        bool isClosed = GUILayout.Toggle(Path.IsClosed, "Set Closed");
//        if (isClosed != Path.IsClosed)
//        {
//            Undo.RecordObject(creator, "Set Closed");
//            Path.IsClosed = isClosed;
//        }

//        // 是否自动调整控制点
//        bool autoSetControlPoints = GUILayout.Toggle(Path.AutoSetControlPoints, "Auto Set Control Points");
//        if (autoSetControlPoints != Path.AutoSetControlPoints)
//        {
//            Undo.RecordObject(creator, "Auto Set Control Points");
//            Path.AutoSetControlPoints = autoSetControlPoints;
//        }

//        if (EditorGUI.EndChangeCheck())
//        {
//            SceneView.RepaintAll();
//        }
//    }

//    private void OnSceneGUI()
//    {
//        Input();
//        Draw();   
//    }

//    void Input()
//    {
//        Event guiEvent = Event.current;
//        Vector3 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

//        mousePos.y = 0;
//        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
//        {
//            if (selectedSegmentIndex != -1)
//            {
//                // 分割曲线
//                Undo.RecordObject(creator, "Split Segment");
//                Path.SplitSegment(mousePos, selectedSegmentIndex);
//            }
//            else if (!Path.IsClosed)
//            {
//                // 增加锚点
//                Undo.RecordObject(creator, "Add Segment");
//                Path.AddSegment(mousePos);
//            }
//        }

//        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
//        {
//            // 删除锚点

//            // 找到最近的锚点
//            float minDstToAnchor = anchorSelectedDistanceThrehold;
//            int closetAnchorIndex = -1;
//            for (int i = 0; i < Path.NumPoints; i+=3)
//            {
//                float dst = Vector3.Distance(mousePos, Path[i]);
//                if (dst < minDstToAnchor)
//                {
//                    closetAnchorIndex = i;
//                }
//            }
//            if (closetAnchorIndex != -1)
//            {
//                Undo.RecordObject(creator, "Delete Segment");
//                Path.DeleteSegment(closetAnchorIndex);
//            }
//        }

//        if (guiEvent.type == EventType.MouseMove)
//        {
//            // 选择曲线

//            float minDstToSegment = segmentSelectedDistanceThrehold;
//            int newSelectedSegmentIndex = -1;

//            // 检测是否靠近曲线
//            for (int i = 0; i < Path.NumSegments; i++)
//            {
//                Vector3[] points = Path.GetPointsInSegment(i);
//                float dst = HandleUtility.DistancePointBezier(mousePos, points[0], points[3], points[1], points[2]);
//                if (dst < minDstToSegment)
//                {
//                    minDstToSegment = dst;
//                    newSelectedSegmentIndex = i;
//                }
//            }

//            if (selectedSegmentIndex != newSelectedSegmentIndex)
//            {
//                selectedSegmentIndex = newSelectedSegmentIndex;
//            }
//        }
//    }

//    void Draw()
//    {
      
//        // 画贝塞尔曲线
//        Handles.color = Color.black;
//        for (int i = 0; i < Path.NumSegments; i++)
//        {
//            Vector3[] points = Path.GetPointsInSegment(i);
//            if (creator.DisplayControlPoints)
//            {
//                Handles.DrawLine(points[0], points[1]);
//                Handles.DrawLine(points[3], points[2]);
//            }
//            Color segmentColor = (i == selectedSegmentIndex && Event.current.shift) ? creator.SelectedSegmentColor : creator.SegmentColor;
//            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColor, null, 2);
//        }

//        // 画控制点
//        for (int i = 0; i < Path.NumPoints; i++)
//        {
//            if (i % 3 == 0 || creator.DisplayControlPoints)
//            {
//                Handles.color = i % 3 == 0 ? creator.AnchorColor : creator.ControlColor;
//                float handleSize = i % 3 == 0 ? creator.AnchorDiameter : creator.ControlDiameter;
//                Vector3 newPos = Handles.FreeMoveHandle(Path[i], Quaternion.identity, handleSize, Vector3.zero, Handles.CylinderHandleCap);
//                // 固定在x/z轴上移动
//                newPos.y = 0;
//                if (Path[i] != newPos)
//                {
//                    Undo.RecordObject(creator, "Move Point");
//                    Path.MovePoint(i, newPos);
//                }
//            }
//        }
//    }

//    private void OnEnable()
//    {
//        creator = (PathCreator)target;
//        if (creator.path == null)
//        {
//            creator.CreatePath();
//        }
//    }
//}
