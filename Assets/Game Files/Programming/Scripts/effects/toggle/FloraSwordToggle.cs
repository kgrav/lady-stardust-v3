using UnityEngine;
using System;

public class FloraSwordToggle : ToggleEffect {
    public float intime, extime;
    float tptr;
    public int particles;
    public float radius;
    public float goalSize;
    ParticleSystem.Particle[] buffer;
    Vector3[] bufdir;
    ParticleSystem psys;
    public MeshRenderer swordTog;
    SmartObject smartObject => transform.root.GetComponent<SmartObject>();
    public string onsound,offsound;
    bool drawn = false;
    public bool isDrawn { get { return drawn; } }
    bool _animating = false;
    public bool animating { get { return _animating; } }

    void Start()
    {
        psys = GetComponent<ParticleSystem>();
        var emitter = psys.emission;
        emitter.enabled = false;
    }
    float scalespeed, movespeed;
    protected override void OnSwitchActivity(bool newActivity)
    {
        
        if(!_animating)
        {
            _animating = true;
            psys.Emit(particles);
            buffer = new ParticleSystem.Particle[psys.particleCount];
            psys.GetParticles(buffer);
            if(smartObject){
                smartObject.fxStateMachine.PlaySound(newActivity ? onsound : offsound);
            }
            bufdir = new Vector3[buffer.Length];
            scalespeed = !active ? (-goalSize / extime) : (goalSize / intime);
            movespeed = !active ? (-radius / extime) : (radius / intime);
            tptr = 0;
            for (int i = 0; i < buffer.Length; ++i)
            {
                buffer[i].startSize = !active ? goalSize : 0;
                buffer[i].position = !active ? buffer[i].position : buffer[i].position - buffer[i].velocity.normalized * radius;
                bufdir[i] = buffer[i].velocity.normalized;
                buffer[i].velocity = Vector3.zero;
            }
            psys.SetParticles(buffer, buffer.Length);
        }
    }
    
    void Update()
    {
        if(animating)
        {
            tptr += Time.deltaTime;
            if(tptr > (drawn ? extime : intime))
            {
                _animating = false;
                swordTog.enabled=active;
                psys.SetParticles(new ParticleSystem.Particle[0], 0);
                return;
            }
            for(int i = 0; i < buffer.Length; ++i)
            {
                buffer[i].startSize += scalespeed * Time.deltaTime;
                buffer[i].position += movespeed*bufdir[i] * Time.deltaTime;
            }
            psys.SetParticles(buffer, buffer.Length);
        }
    }
}