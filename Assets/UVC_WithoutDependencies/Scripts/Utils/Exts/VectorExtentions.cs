using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExtentions
{
    public static Vector3 ZeroHeight (this Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }

    public static Vector3 SuperSmoothLerp (Vector3 currentPos, Vector3 targetOldPos, Vector3 targetCurrentPos, float time, float speed)
    {
        Vector3 f = currentPos - targetOldPos + (targetCurrentPos - targetOldPos) / (speed * time);
        return targetCurrentPos - (targetCurrentPos - targetOldPos) / (speed * time) + f * Mathf.Exp (-speed * time);
    }
}
