using System;
using UnityEngine;
using System.Collections.Generic;
public class GlobalEffectsTable : MonoBehaviour {
    
    static GlobalEffectsTable _fxTable;
    public static GlobalEffectsTable fxTable {get{if(!_fxTable) _fxTable = FindObjectOfType<GlobalEffectsTable>(); return _fxTable;}}
    
    public FXEntry[] globalEffects;
    
    Dictionary<string, Effect> globalTrigFX;
    Dictionary<string, ToggleEffect> globalTogFX;

    void Start(){
        globalTrigFX = new Dictionary<string, Effect>();
        globalTogFX = new Dictionary<string, ToggleEffect>();
        foreach(FXEntry f in globalEffects){
            switch(f.type){
            case FXType.Trigger:
            Effect e = Instantiate(f.value).GetComponent<Effect>();
            globalTrigFX.Add(f.label, e);
            break;
            case FXType.Toggle:
            ToggleEffect t = Instantiate(f.value).GetComponent<ToggleEffect>();
            globalTogFX.Add(f.label, t);
            break;
            }
        }
    }



    public static void TriggerEffect(string effectKey, Vector3 position, Vector3 direction){
        fxTable.globalTrigFX[effectKey].Trigger(position, direction);
    }

    public static void ToggleEffect(string effectKey,bool toggle ){
        fxTable.globalTogFX[effectKey].SetActive(toggle);
    }
}