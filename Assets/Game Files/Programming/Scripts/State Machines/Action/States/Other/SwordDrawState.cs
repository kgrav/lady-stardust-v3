using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CharacterState/ActionState/Other/DrawSword")]
public class SwordDrawState : SmartState {
    public int drawLag;
    public string vfxlabel;
    public SmartState attackOnExit;

    public override void OnEnter(SmartObject smartObject){
        base.OnEnter(smartObject);
        if(smartObject.fxStateMachine.GetToggleState(vfxlabel)){
            if(attackOnExit)
            smartObject.ActionStateMachine.ChangeActionState(attackOnExit);
            else
            smartObject.ActionStateMachine.ChangeActionState(ActionStates.Idle);
        }
    }

    public override void AfterCharacterUpdate(SmartObject smartObject, float deltaTime){
        base.AfterCharacterUpdate(smartObject, deltaTime);
        if(smartObject.CurrentFrame >= drawLag){
            if(attackOnExit)
            smartObject.ActionStateMachine.ChangeActionState(attackOnExit);
            else
            smartObject.ActionStateMachine.ChangeActionState(ActionStates.Idle);
        }
    }
}
