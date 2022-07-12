using UnityEngine;
using System;

public class FloraAnimationState : ActorAnimationState {
    SmartObject con;

    public void SetController(SmartObject fmc){
        con=fmc;
    }
}