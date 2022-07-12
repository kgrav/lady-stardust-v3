using System;
using UnityEngine;


public class MultiFireEffect : Effect{

    public int times;
    public string fxKey;

    public override void Trigger( Vector3 pos, Vector3 dir){
        for(int i = 0; i < times; ++i){
            GlobalEffectsTable.TriggerEffect(fxKey, pos, dir);
        }
    }
}