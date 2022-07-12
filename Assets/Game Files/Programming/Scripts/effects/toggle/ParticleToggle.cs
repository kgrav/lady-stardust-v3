using UnityEngine;
using System;

public class ParticleToggle : ToggleEffect {
    ParticleSystem psys => GetComponent<ParticleSystem>();
    protected override void OnSwitchActivity(bool newActivity){
        var e = psys.emission;
        e.enabled=newActivity;
    }
}