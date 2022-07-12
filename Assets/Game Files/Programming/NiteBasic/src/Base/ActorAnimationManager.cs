using UnityEngine;
using System;

public class ActorAnimationManager : NVComponent {
    public void CallAnimationMethod(string method){
        Invoke(method,0f);
    }

    public void Sound(string key){
        
    }
}