using System;
using UnityEngine;

public static class AIUtils
{
    public static double GetTimeStamp(bool isMillisecond = true)
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1);
        if (isMillisecond) { return ts.TotalMilliseconds; } else { return ts.TotalSeconds; }
    }

    public static Vector3 TransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var localToWorldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        return localToWorldMatrix.MultiplyPoint3x4(position);
    }

    public static Vector3 InverseTransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
        return worldToLocalMatrix.MultiplyPoint3x4(position);
    }
}