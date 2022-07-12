using UnityEngine;
using System;

public class Floor : MonoBehaviour{
    public static string globfootstepFX = "default";
    public string footstepFX = "default";


    void OnCollisionEnter(Collision c){
        if(!footstepFX.Equals(globfootstepFX)){
            if(c.transform.GetComponent<FloraController>()){
                print("set footstep effects " + footstepFX);
            }
        }
    }
}