using UnityEngine;
using System;

using KinematicCharacterController;

public abstract class FloraControllerState {
    protected FloraController con;
    public bool blockActions=false;
    public void SetController(FloraController con){
        this.con=con;
    }

    public virtual void UpdateLoop(){
        
    }

    public virtual void OnStateEnter(){

    }

    public virtual void OnStateExit(){

    }

    public virtual bool BypassGroundHit(Collider c, Vector3 normal, Vector3 point, ref HitStabilityReport hsr){
        return false;
    }

    public virtual bool BypassAnimationExitCallback(string state){
        return false;
    }

    public virtual bool BypassMovementHit(Collider c, Vector3 normal, Vector3 point, ref HitStabilityReport hsr){
        return false;
    }

    public virtual bool BypassAnimation(ref string state, float deltaTime){
        return false;
    }

    public virtual bool BypassRotation(ref Quaternion currentRotation, float deltaTime){
        return false;
    }
    public virtual bool BypassInputMovement(ref Vector3 currentVelocity, float deltaTime){
        return false;
    }

    public virtual bool BypassGravity(ref Vector3 currentVelocity, float deltaTime){
        return false;
    }

    public virtual bool BypassExForce(ref Vector3 currentVelocity, float deltaTime){
        return false;
    }
    public virtual bool BypassJump(ref Vector3 currentVelocity, float deltaTime){
        return false;
    }
}