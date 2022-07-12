using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

public abstract class FLocomotionState : ScriptableObject
{

	public SmartState[] SmartStates;
	[SerializeField]
	private BonusOrientationMethod bonusOrientationMethod = BonusOrientationMethod.TowardsGravity;
	public float OrientationSharpness;
	public float BonusOrientationSharpness;
	public virtual void OnEnter(FloraSmartObject smartObject)
	{
	}

	public virtual void OnExit(FloraSmartObject smartObject)
	{

	}

	public virtual void UpdateRotation(FloraSmartObject smartObject, ref Quaternion currentRotation, float deltaTime)
	{

	}

	public virtual void UpdateVelocity(FloraSmartObject smartObject, ref Vector3 currentVelocity, float deltaTime)
	{

	}

	public virtual void BeforeCharacterUpdate(FloraSmartObject smartObject, float deltaTime)
	{

	}

	public virtual void PostGroundingUpdate(FloraSmartObject smartObject, float deltaTime)
	{

	}

	public virtual void AfterCharacterUpdate(FloraSmartObject smartObject, float deltaTime)
	{

	}

	public virtual bool IsColliderValidForCollisions(FloraSmartObject smartObject, Collider coll)
	{
		return true;
	}

	public virtual void OnGroundHit(FloraSmartObject smartObject, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{

	}

	public virtual void OnMovementHit(FloraSmartObject smartObject, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{

	}

	public virtual void ProcessHitStabilityReport(FloraSmartObject smartObject, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
	{

	}

	public virtual void OnDiscreteCollisionDetected(FloraSmartObject smartObject, Collider hitCollider)
	{

	}

	public virtual void CalculateStateVelocity(FloraSmartObject smartObject, ref Vector3 currentVelocity, float deltaTime)
	{

	}

	public virtual void CalculateCharacterUp(FloraSmartObject smartObject,ref Quaternion currentRotation, float deltaTime)
	{
		Vector3 currentUp = (currentRotation * Vector3.up);
		if (bonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
		{
			// Rotate from current up to invert gravity
			Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -smartObject.Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
			currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
		}
		else if (bonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
		{
			if (smartObject.Motor.GroundingStatus.IsStableOnGround)
			{
				Vector3 initialCharacterBottomHemiCenter = smartObject.Motor.TransientPosition + (currentUp * smartObject.Motor.Capsule.radius);

				Vector3 smoothedGroundNormal = Vector3.Slerp(smartObject.Motor.CharacterUp, smartObject.Motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
				currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

				// Move the position to create a rotation around the bottom hemi center instead of around the pivot
				smartObject.Motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * smartObject.Motor.Capsule.radius));
			}
			else
			{
				Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -smartObject.Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
				currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
			}
		}
		else
		{
			Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
			currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
		}
	}
}
