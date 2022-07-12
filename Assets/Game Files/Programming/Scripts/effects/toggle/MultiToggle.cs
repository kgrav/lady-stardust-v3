using System;
using UnityEngine;

public class MultiToggle : ToggleEffect {
    bool childrenInit = false;

    ToggleEffect[] _children;
    ToggleEffect[] children {get{
        if(!childrenInit){
            childrenInit=true;
            _children = GetComponentsInChildren<ToggleEffect>();
        }
        return _children;
    }}


    protected override void OnSwitchActivity(bool active){
        foreach(ToggleEffect t in children){
            if(t.GetHashCode() != this.GetHashCode())
            t.SetActive(active);
        }
    }
}