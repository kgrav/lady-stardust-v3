using UnityEngine;
using System;

public class FootstepAudioEffect : Effect {
    public string step1,step2;
    bool toggle=false;
    public override void Trigger(Vector3 position, Vector3 direction)
    {
        WorldAudioManager.PlayClipInWorld(toggle ? step1 : step2, position);
        toggle = !toggle;
    }
}