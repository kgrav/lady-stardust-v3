using UnityEngine;
using System;

public class ParticleBurstEffect : Effect{
    ParticleSystem _psys;
    public ParticleSystem psys {get{if(!_psys) _psys=GetComponent<ParticleSystem>(); return _psys;}}
    public int particles;
    public override void Trigger(Vector3 position, Vector3 direction)
    {
        if(position!=Vector3.zero)
        tform.position = position;
        if(direction!=Vector3.zero)
            tform.LookAt(position + direction);
        psys.Emit(particles);
    }
}