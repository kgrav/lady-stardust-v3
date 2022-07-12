using UnityEngine;
using System;

public class NVMath {
    const float horizontalTolerance = 5f;
    public static bool IsHorizontal(Vector3 normal){
        return (Vector3.Angle(normal, Vector3.up) > 90-horizontalTolerance) && (Vector3.Angle(normal, Vector3.down) > 90-horizontalTolerance);

    }

    public static Vector3 Planarized(Vector3 v){
        v.y=0;
        return v;
    }

    public static Vector3 NVLerp(Vector3 from, Vector3 to, float speed, float deltaTime){
        Vector3 r = from;
        float dist = Vector3.Distance(from, to);
        if(dist>0){
        float projectedMoveDist = speed*deltaTime;
        if(projectedMoveDist < dist){
            Vector3 dir = (to-from).normalized;
            r += dir*projectedMoveDist;
        }
        else{
            r = to;
        }
        }
        return r;
    }
}