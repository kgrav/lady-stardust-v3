using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartState : ScriptableObject
{
    public string AnimationState;
    public float AnimationTransitionTime;
    public float AnimationTransitionOffset;
	public ActionStates pronoun;
    public int MaxTime;

    //public VFXContainer[] VFX;
    //public BodyVFXContainer[] BodyVFX;
    //public SFXContainer[] SFX;
	public FloraFace FaceOnEnter;

	public FXFrame[] FXFrames;
	public FXFrame[] onEnter, onExit;
	bool frameTableInit = false;
	public int MaxFrame;
	List<FXFrame>[] _frameTable;
	List<FXFrame>[] frameTable{
		get{
			if(!frameTableInit){
				_frameTable=new List<FXFrame>[MaxTime+1];
				for(int i = 0; i < _frameTable.Length; ++i){
					_frameTable[i] = new List<FXFrame>();
				}
                if(FXFrames!=null){
				foreach(FXFrame fx in FXFrames){
					if(fx.frame < _frameTable.Length)
					_frameTable[fx.frame].Add(fx);
				}}
				frameTableInit=true;
			}
			return _frameTable;
		}
	}

	protected virtual void SetFace(){
		if(FaceOnEnter)
		FloraFaceManager.mgr.SetFace(FaceOnEnter);
	}

    public virtual void OnEnter(SmartObject smartObject)
    {
		SetFace();
        smartObject.CurrentTime = -1;
        smartObject.CurrentFrame = -1;
		if(onEnter!=null){
		foreach(FXFrame fx in onEnter){
			smartObject.fxStateMachine.ResolveFXFrame(fx);
		}}
        if (AnimationState.Length > 0)
        {
            if (AnimationTransitionTime != 0)
            {
                smartObject.Animator.CrossFadeInFixedTime(AnimationState, AnimationTransitionTime, 0, AnimationTransitionOffset);

            }
            else
            {
                smartObject.Animator.Play(AnimationState, 0, 0);
            }
        }
    }

    public void CreateVFX(SmartObject smartObject)
    {
        /*if (VFX == null || VFX.Length == 0)
            return;

        for (int i = 0; i < VFX.Length; i++)
            if (VFX[i].Time == smartObject.CurrentFrame)
            {
                GameObject obj = Instantiate(VFX[i].VFX, smartObject.transform.position, smartObject.transform.rotation, smartObject.transform);

                obj.transform.localPosition = VFX[i].Position;
                obj.transform.localEulerAngles = VFX[i].Rotation;
                if (!VFX[i].Local)
                    obj.transform.parent = null;

            }*/
    }

    public void CreateBodyVFX(SmartObject smartObject)
    {
        /*if (BodyVFX == null || BodyVFX.Length == 0)
            return;

        for (int i = 0; i < BodyVFX.Length; i++)
            if (BodyVFX[i].Time == smartObject.CurrentFrame)
                smartObject.ToggleBodyVFX(BodyVFX[i].BodyVFX, BodyVFX[i].Toggle);*/
    }

    public void CreateSFX(SmartObject smartObject)
    {
        /*if (SFX == null || SFX.Length == 0)
            return;

        for (int i = 0; i < SFX.Length; i++)
            if (SFX[i].Time == smartObject.CurrentFrame)
                SFX[i].SFX.PlaySFX(smartObject);*/
    }

    public virtual void OnExit(SmartObject smartObject)
    {
		if(onExit!=null){
		foreach(FXFrame fx in onExit){
			
			smartObject.fxStateMachine.ResolveFXFrame(fx);
		}}
    }

    public virtual void HandleState(SmartObject smartObject)
    {

    }

    public virtual void UpdateRotation(SmartObject smartObject, ref Quaternion currentRotation, float deltaTime)
    {

    }

    public virtual void UpdateVelocity(SmartObject smartObject, ref Vector3 currentVelocity, float deltaTime)
    {

    }

    public virtual void BeforeCharacterUpdate(SmartObject smartObject, float deltaTime)
    {

    }

    public virtual void PostGroundingUpdate(SmartObject smartObject, float deltaTime)
    {

    }

    public virtual void AfterCharacterUpdate(SmartObject smartObject, float deltaTime)
    {
		if(frameTable != null && smartObject.CurrentFrame < frameTable.Length){
			foreach(FXFrame fx in frameTable[smartObject.CurrentFrame]){

				smartObject.fxStateMachine.ResolveFXFrame(fx);
			}
		}
		
    }

    public virtual bool IsColliderValidForCollisions(SmartObject smartObject, Collider coll)
    {
        return true;
    }

    public virtual void OnGroundHit(SmartObject smartObject, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {

    }

    public virtual void OnMovementHit(SmartObject smartObject, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {

    }

    public virtual void ProcessHitStabilityReport(SmartObject smartObject, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {

    }

    public virtual void OnDiscreteCollisionDetected(SmartObject smartObject, Collider hitCollider)
    {

    }
}