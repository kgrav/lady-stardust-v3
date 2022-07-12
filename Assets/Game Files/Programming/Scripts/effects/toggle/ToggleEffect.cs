using UnityEngine;
using System;

public abstract class ToggleEffect : NVComponent{
    public bool startActive;
    public string label;
    void Awake(){
        if(startActive){
            SetActive(true);
        }
    }

    public bool active {get; private set;}
    public void SetActive(bool toggle){
        bool _active = active;
        active = toggle;
        if(active != _active ){
            OnSwitchActivity(toggle);
        }
    }

    protected virtual bool CanToggle(bool newActivity){
        return true;
    }


    protected virtual void OnSwitchActivity(bool newActivity){}
}