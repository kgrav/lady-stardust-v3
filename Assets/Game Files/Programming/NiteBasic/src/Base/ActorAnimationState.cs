using System;
using System.Collections.Generic;
using UnityEngine;


public enum DTYPE {BOOL, FLOAT, INT, TRIG, RETRIG}
[Serializable]
public class ParamSetting {
    public DTYPE type;
    public string label,value;
    public void Apply(Animator anim){
        switch(type){
            case DTYPE.BOOL:
                anim.SetBool(label,bool.Parse(value));
            break;
            case DTYPE.FLOAT:
                anim.SetFloat(label,float.Parse(value));
            break;
            case DTYPE.INT:
                anim.SetInteger(label,int.Parse(value));
            break;
            case DTYPE.TRIG:
                anim.SetTrigger(label);
            break;
            case DTYPE.RETRIG:
                anim.ResetTrigger(label);
            break;
        }
    }
}

public class ActorAnimationState : StateMachineBehaviour
{
    public string enterStateMethod, exitStateMethod;
 
    public bool loopPercentFunctions;

    
    public PercentFunction[] soundClips;
    [Serializable]
    public class PercentFunction{
        public float pct;
        public string method;
    }
    int pertptr=0,audtptr=0;
    float lastTime = 0;
    
    // Start is called before the first frame update
    protected SmartObject controller;

 
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(!controller){
            controller=animator.transform.root.GetComponent<SmartObject>();
        }
        pertptr = 0;
        audtptr=0;
        ChStart(animator,stateInfo,layerIndex);
    }

    protected virtual void ChStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
        
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ChExit(animator, stateInfo,layerIndex);
    }
    protected virtual void ChExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float newTime = stateInfo.normalizedTime - Mathf.Floor(stateInfo.normalizedTime);
        if(soundClips.Length > 0){

            if(audtptr < soundClips.Length && newTime > soundClips[audtptr].pct){
                controller.fxStateMachine.PlaySound(soundClips[audtptr].method);
                audtptr++;
            }
            else if(loopPercentFunctions && audtptr >= soundClips.Length && newTime < lastTime){
                audtptr = 0;
            }
        }
        lastTime = newTime;
        ChUpdate(animator, stateInfo, layerIndex);
    }

    protected virtual void ChUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }
}
