using UnityEngine;
using System;

[System.Serializable]
public class FloraKnockbackState : FloraControllerState{
    
    string anim = "";
    Vector3 dir = Vector3.zero;
    public override void OnStateEnter()
    {
        dir = NVMath.Planarized(con.exForce).normalized;
        anim = "hit_sailback";
               
    }

    public override bool BypassAnimation(ref string next, float deltaTime){
        if(anim!=""){
            next = anim;
            anim="";
        }
        return true;
    }

    public override bool BypassInputMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        return true;
    }

    

    public override bool BypassRotation(ref Quaternion currentRotation, float deltaTime)
    {
        currentRotation = Quaternion.LookRotation(-dir, con.Motor.CharacterUp);
        return true;
    }

    public override bool BypassAnimationExitCallback(string state)
    {
        if(state.Equals("hit_grounded_getup")){
            con.SetNextControllerState(FLORASTATE.DEFAULT);
        }
        return true;
    }

    public override void UpdateLoop(){
        if(con.Motor.GroundingStatus.FoundAnyGround){
            
            anim = "hit_grounded";
            if(con.exForce.magnitude < 0.1f){
                anim = "hit_grounded_getup";
            }
        }
    }
        
}