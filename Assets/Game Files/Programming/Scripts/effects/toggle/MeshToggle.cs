using System;
using UnityEngine;

public class MeshToggle : ToggleEffect {

    MeshRenderer _rend;
    MeshRenderer rend {get{if(!_rend)_rend = GetComponent<MeshRenderer>(); return _rend;}}

    protected override void OnSwitchActivity(bool active){
        rend.enabled=active;
    }
}