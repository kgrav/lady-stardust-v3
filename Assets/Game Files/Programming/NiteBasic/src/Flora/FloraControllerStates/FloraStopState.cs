using UnityEngine;
using System;

[System.Serializable]
public class FloraStopState : FloraControllerState {

    public override bool BypassInputMovement(ref Vector3 currentVelocity, float deltaTime){
        return true;
    }

    public override bool BypassRotation(ref Quaternion currentRotation, float deltaTime){
        return true;
    }
}