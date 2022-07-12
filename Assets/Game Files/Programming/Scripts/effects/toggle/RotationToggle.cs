using UnityEngine;
using System;

public class RotationToggle : ToggleEffect{
    public Vector3 euler;
    public bool transformEuler;

    void Start(){
        if(transformEuler)
            euler = tform.TransformDirection(euler);
    }
    void Update(){
        if(active){
        tform.rotation = Quaternion.Euler(euler*Time.deltaTime)*tform.rotation;
    }
    }
}