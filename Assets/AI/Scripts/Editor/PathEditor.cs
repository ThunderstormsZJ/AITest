using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor 
{
    Path path;
    PathCreator creator;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // 创新创建
        if(GUILayout.Button("Create New"))
        {
            Undo.RecordObject(creator, "Create New");
            creator.CreatePath();
            path = creator.path;
        }

        // 是否闭合
        bool isClosed = GUILayout.Toggle(path.IsClosed, "Set Closed");
        if (isClosed != path.IsClosed)
        {
            Undo.RecordObject(creator, "Set Closed");
            path.IsClosed = isClosed;
        }

        // 是否自动调整控制点
        bool autoSetControlPoints = GUILayout.Toggle(path.AutoSetControlPoints, "Auto Set Control Points");
        if (autoSetControlPoints != path.AutoSetControlPoints)
        {
            Undo.RecordObject(creator, "Auto Set Control Points");
            path.AutoSetControlPoints = autoSetControlPoints;
        }
    }

    private void OnSceneGUI()
    {
        Input();
        Draw();   
    }

    void Input()
    {
        Event guiEvent = Event.current;
        Vector3 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
        {
            mousePos.y = 0;
            Undo.RecordObject(creator, "Add Segment");
            path.AddSegment(mousePos);
        }
    }

    void Draw()
    {
      
        // 画贝塞尔曲线
        Handles.color = Color.black;
        for (int i = 0; i < path.NumSegments; i++)
        {
            Vector3[] points = path.GetPointsInSegment(i);
            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[3], points[2]);
            Handles.DrawBezier(points[0], points[3], points[1], points[2], Color.green, null, 2);
        }

        // 画控制点
        for (int i = 0; i < path.NumPoints; i++)
        {
            Handles.color = i % 3 == 0 ? creator.AnchorColor : creator.ControlColor;
            float handleSize = i % 3 == 0 ? creator.AnchorDiameter : creator.ControlDiameter;
            Vector3 newPos = Handles.FreeMoveHandle(path[i], Quaternion.identity, handleSize, Vector3.zero, Handles.CylinderHandleCap);
            // 固定在x/z轴上移动
            if (path[i] != newPos && newPos.y==0)
            {
                Undo.RecordObject(creator, "Move Point");
                path.MovePoint(i, newPos);
            }
        }
    }

    private void OnEnable()
    {
        creator = (PathCreator)target;
        if (creator.path == null)
        {
            creator.CreatePath();
        }
        path = creator.path;
    }
}
