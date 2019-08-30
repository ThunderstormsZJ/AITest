using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(FieldOfView))]
public class FieldOfViewEditor : Editor 
{
    private void OnSceneGUI()
    {
        FieldOfView fow = (FieldOfView)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360, fow.ViewRadius);

        Vector3 viewDirA = fow.DirFromAngle(-fow.ViewAngle/2, false);
        Vector3 viewDirB = fow.DirFromAngle(fow.ViewAngle/2, false);
        // 视野角度
        Handles.DrawLine(fow.transform.position, fow.transform.position + viewDirA * fow.ViewRadius);
        Handles.DrawLine(fow.transform.position, fow.transform.position + viewDirB * fow.ViewRadius);

        Handles.color = Color.red;
        foreach (Transform visibleTarget in fow.VisibleTargets)
        {
            Handles.DrawLine(fow.transform.position, visibleTarget.position);
        }
    }
}
