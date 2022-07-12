using UnityEngine;
using System;
using System.Collections.Generic;



public class FloraAnimationManager : ActorAnimationManager
{
    Animator anim => GetComponent<FloraController>().anim;
    FloraController con => GetComponent<FloraController>();

    public FloraAnimation[] animations;

    Dictionary<string, FloraAnimation> anims;
    string curAnimState = "";
    string nextAnimState = "";


    float lastTime = 0;
    int pptr = 0, sptr = 0;
    bool exitptr = false;
    void Awake()
    {
        FloraAnimationState[] flast = anim.GetBehaviours<FloraAnimationState>();
        foreach (FloraAnimationState f in flast)
        {
        }
        anims = new Dictionary<string, FloraAnimation>();
        foreach (FloraAnimation a in animations)
        {
            anims.Add(a.label, a);
        }
    }
    public float normalizedTime { get { return anim.GetCurrentAnimatorStateInfo(0).normalizedTime; } }
    void Update()
    {
        if (curAnimState.Length > 0)
        {
            float ntime = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
            FloraAnimation fla = anims[curAnimState];
            if (ntime % 1 < lastTime && fla.loopData)
            {
                pptr = 0;
                sptr = 0;
            }
            if (pptr < fla.percentFunctions.Length && ntime % 1 > fla.percentFunctions[pptr].pct)
            {
                CallAnimationMethod(fla.percentFunctions[pptr].method);
                pptr++;
            }
            if (sptr < fla.soundClips.Length && ntime % 1 > fla.soundClips[sptr].pct)
            {
                Sound(fla.soundClips[sptr].method);
                sptr++;
            }
            if(fla.callbackOnExit && ntime > fla.exitTime && !exitptr){
                exitptr=true;
                con.AnimationExitCallback(fla.label);
            }
            lastTime = ntime % 1;
        }
    }

    public void SetState(string state)
    {
        if (!state.Equals(curAnimState) && anims.ContainsKey(state))
        {
            //Debug.Log(state);
            float xfade = anims[state].fadeTo;
            if (xfade > 0)
            {
                anim.CrossFadeInFixedTime(state, xfade);
            }
            else
            {
                anim.Play(state, 0);
            }
            curAnimState = state;
            pptr = 0;
            sptr = 0;
            exitptr=false;
        }
        else{
            //Debug.Log("invalid Flora Animation State transition \"" + state +"\" requested and ignored");
        }
    }



    public void SetControllerState(FLORASTATE newState)
    {
        con.SetNextControllerState(newState);
    }
}