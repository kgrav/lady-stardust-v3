using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
public enum OrientationMethod
{
    TowardsCamera,
    TowardsMovement,
}

public enum BonusOrientationMethod
{
    None,
    TowardsGravity,
    TowardsGroundSlopeAndGravity,
}

public enum FLORASTATE
{
    NONE = -1,
    DEFAULT = 0,
    SLIDE = 1,
    STOP = 2,
    KNOCKBACK=3,
    ATTACK=4
}

public class FloraController : ActorController, ICharacterController
{
    public Animator anim => GetComponentInChildren<Animator>();
    public FloraAnimationManager animgr => GetComponent<FloraAnimationManager>();
    public KinematicCharacterMotor Motor => GetComponent<KinematicCharacterMotor>();

    public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
    public float BonusOrientationSharpness = 10f;
    public Vector3 Gravity = new Vector3(0, -30f, 0);
    public float MaxStableMoveSpeed = 10f;
    public float StableMovementSharpness = 15f;
    public float poise, knockbackGravity;
    public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;
    [Header("Air Movement")]
    public float MaxAirMoveSpeed = 15f;
    public float AirAccelerationSpeed = 15f;
    public float Drag = 0.1f;

    public float OrientationSharpness = 10f;
    [Header("Jumping")]
    public bool AllowJumpingWhenSliding = false;
    public float JumpUpSpeed = 10f;
    public float JumpScalableForwardSpeed = 10f;
    public float JumpPreGroundingGraceTime = 0f;
    public float JumpPostGroundingGraceTime = 0f; private bool _jumpRequested = false;
    [Header("Slide")]
    public float attack;
    public float decay;
    public float punishForceScale;

    public Vector2 wallJumpDist;
    public float wallJumpScale;

    private bool _jumpConsumed = false;
    private bool _jumpReleased = false;
    private bool _jumpedThisFrame = false;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private float _timeSinceLastAbleToJump = 0f;
    private Vector3 _internalVelocityAdd = Vector3.zero;

    public Vector3 moveInputVector
    {
        get
        {
            return _moveInputVector;
        }
    }
    public Vector3 lookInputVector
    {
        get
        {
            return _lookInputVector;
        }
    }

    Vector3 _moveInputVector, _lookInputVector;
    Vector3 _forceInputVector;
    FloraInputs inputs;
    Transform cam => FindObjectOfType<NVCam>().transform;

    //State machine
    [Header("States")]
    //public FloraSlideState slideState;
    public FloraStopState stopState;
    public FloraKnockbackState knockbackState;
    FloraControllerState[] states;
    FLORASTATE cstate = FLORASTATE.DEFAULT;
    FLORASTATE nextState = FLORASTATE.NONE;
    string nextAnim = "idle";

    public Vector3 exForce {get; private set;}

    bool _slideDown=false;


    //default animation setting
    public bool jumpanim {get; private set;}
    float yVelocity=0f, planarVelocity=0f;
    Vector3 defaultLookDir = Vector3.zero;

    //

    public void SetInputs(ref FloraInputs inputs)
    {
        Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);
        // Calculate camera direction and rotation on the character plane
        Vector3 xdir = moveInputVector.x * cam.right;
        Vector3 zdir = moveInputVector.z * cam.forward;
        moveInputVector = (xdir + zdir);
        moveInputVector.y = 0;
        _lookInputVector = moveInputVector.normalized;
        _moveInputVector = moveInputVector;// Jumping input
        if (inputs.JumpDown)
        {
            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;
        }
        _jumpReleased = inputs.JumpUp;
        _slideDown = inputs.SlideDown;

    }

    public void SetNextControllerState(FLORASTATE nextState){
        this.nextState=nextState;
    }

    void SetControllerState(FLORASTATE newState)
    {
        print(newState);
        if (newState == FLORASTATE.NONE || newState == cstate)
            return;
        states[(int)cstate].OnStateExit();
        states[(int)newState].OnStateEnter();
        cstate = newState;
    }
    void Awake()
    {
        Motor.CharacterController = this;
        states = new FloraControllerState[4];
        states[(int)FLORASTATE.DEFAULT] = new FloraDefaultCS();
        //states[(int)FLORASTATE.SLIDE] = slideState;
        states[(int)FLORASTATE.STOP] = stopState;
        states[(int)FLORASTATE.KNOCKBACK] = knockbackState;
        foreach (FloraControllerState s in states)
        {
            s.SetController(this);
        } 
    }

    // Update is called once per frame
    void Update()
    {   
        states[(int)cstate].UpdateLoop();
        if(cstate != FLORASTATE.SLIDE && _slideDown && _moveInputVector.magnitude > 0.01f && Motor.GroundingStatus.IsStableOnGround){
            nextState=FLORASTATE.SLIDE;
        }
    }

    public void OnDiscreteCollisionDetected(Collider c)
    {
    }

    public void ProcessHitStabilityReport(Collider c, Vector3 v1, Vector3 v2, Vector3 v3, Quaternion rotation, ref HitStabilityReport hsr)
    {

    }
    

    public void HitWithAttack(float damage, float force, Vector3 direction){
        Vector3 forcei = force*direction;
        if(force > poise){
            forcei += Vector3.up*(force*0.5f);
            exForce += forcei*punishForceScale;
            SetControllerState(FLORASTATE.KNOCKBACK);
        }
        else if(force > 0.25f*poise && damage > 0){
            exForce += forcei*punishForceScale;
            SetControllerState(FLORASTATE.STOP);
        }
    }

    public void OnMovementHit(Collider c, Vector3 normal, Vector3 point, ref HitStabilityReport hsr)
    {
        if(!states[(int)cstate].BypassMovementHit(c,normal,point,ref hsr)){
            DefaultMoveHit(c,normal,point, ref hsr);    
        }
    }

    public void OnGroundHit(Collider c, Vector3 normal, Vector3 point, ref HitStabilityReport hsr)
    {
    }

    public bool IsColliderValidForCollisions(Collider c)
    {
        return true;
    }
    

    public void AfterCharacterUpdate(float deltaTime)
    {
        
        string nextAnim = "";
        if(!states[(int)cstate].BypassAnimation(ref nextAnim, deltaTime)){
            DefaultAnimation(ref nextAnim, deltaTime);
        }
        
        if (nextAnim.Length > 0)
        {
            animgr.SetState(nextAnim);
            nextAnim = "";
        }
        // Handle jump-related values
        // Handle jumping pre-ground grace period
        if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
        {
            _jumpRequested = false;
        }

        if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
        {
            // If we're on a ground surface, reset jumping values
            if (!_jumpedThisFrame)
            {
                _jumpConsumed = false;
            }
            _timeSinceLastAbleToJump = 0f;
        }
        else
        {
            // Keep track of time since we were last able to jump (for grace period)
            _timeSinceLastAbleToJump += deltaTime;
        }
    }
    bool _grounded = true;
    public void PostGroundingUpdate(float f)
    {
        if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
        {
            _grounded = true;
        }
        else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
        {
            _grounded = false;
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (!states[(int)cstate].BypassRotation(ref currentRotation, deltaTime)) 
        { 
            DefaultRotation(ref currentRotation, deltaTime); 
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (!states[(int)cstate].BypassInputMovement(ref currentVelocity, deltaTime))
        {
            DefaultInputMovement(ref currentVelocity, deltaTime);
        }
        if (!states[(int)cstate].BypassGravity(ref currentVelocity, deltaTime))
        {
            DefaultGravity(ref currentVelocity, deltaTime);
        }
        if (!states[(int)cstate].BypassJump(ref currentVelocity, deltaTime))
        {
            DefaultJump(ref currentVelocity, deltaTime);
        }
        if(!states[(int)cstate].BypassExForce(ref currentVelocity,deltaTime)){
            DefaultExForce(ref currentVelocity, deltaTime);
        }
    }



    public void BeforeCharacterUpdate(float f)
    {
        jumpanim=false;
        if(nextState != FLORASTATE.NONE && nextState != cstate){
            SetControllerState(nextState);
        }
        nextState=FLORASTATE.NONE;

    }

    public void AnimationExitCallback(string state){
        if(!states[(int)cstate].BypassAnimationExitCallback(state)){
            DefaultAnimationExitCallback(state);
        }
    }

    public void DefaultAnimationExitCallback(string state){

    }
    void DefaultExForce(ref Vector3 currentVelocity, float deltaTime){
        currentVelocity += exForce*deltaTime;
        float exmag = exForce.magnitude;
        exmag = Mathf.Max(0f, exmag-knockbackGravity*deltaTime);
        exForce = exForce.normalized * exmag;
    }
    void DefaultMoveHit(Collider c, Vector3 normal, Vector3 point, ref HitStabilityReport hsr){
        if (!Motor.GroundingStatus.IsStableOnGround && c.GetComponent<WorldObject>() && NVMath.IsHorizontal(normal))
            {                
                HitWithAttack(0f, planarVelocity, normal);
            }
    }

    void DefaultAnimation(ref string state, float deltaTime){
        if (!_grounded && yVelocity < 0)
        {
            state="fall";
        }
        else if(jumpanim){
            state="jump";
        }
        else if(Motor.GroundingStatus.IsStableOnGround){
            state="idle";
        }
    }

    void DefaultJump(ref Vector3 currentVelocity, float deltaTime)
    {
        _jumpedThisFrame = false;
        _timeSinceJumpRequested += deltaTime;
        RaycastHit rch = default(RaycastHit);
        bool wallcheck = false;
        if(Physics.Raycast(tform.position, tform.forward, out rch, wallJumpDist.y)){
            if(rch.distance > wallJumpDist.x && (_moveInputVector != Vector3.zero && Vector3.Angle(_moveInputVector, rch.normal)< 25)){
                wallcheck = true;
                print("WALL JUMP OK");
            }
        }
        if (_jumpRequested)
        {
            if(!Motor.GroundingStatus.FoundAnyGround && wallcheck && !_jumpedThisFrame){
                _jumpRequested = false;
                _jumpConsumed = true;
                _jumpedThisFrame = true;
                Vector3 jumpDirection = Motor.CharacterUp;
                if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                {
                    jumpDirection = Motor.GroundingStatus.GroundNormal;
                }
                jumpanim=true;
                
                currentVelocity = rch.normal*MaxStableMoveSpeed;
                defaultLookDir = currentVelocity.normalized;
                currentVelocity += (jumpDirection * JumpUpSpeed*wallJumpScale);
            }
            // See if we actually are allowed to jump
            else if (!_jumpConsumed && (((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime)))
            {
                // Calculate jump direction before ungrounding
                Vector3 jumpDirection = Motor.CharacterUp;
                if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                {
                    jumpDirection = Motor.GroundingStatus.GroundNormal;
                }

                // Makes the character skip ground probing/snapping on its next update. 
                // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                Motor.ForceUnground();
                jumpanim=true;
                // Add to the return velocity and reset jump state
                currentVelocity += (jumpDirection * JumpUpSpeed);
                currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
                _jumpRequested = false;
                _jumpConsumed = true;
                _jumpedThisFrame = true;
            }
        }
    }

    void DefaultGravity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (!Motor.GroundingStatus.IsStableOnGround)
        {
            currentVelocity += Gravity * deltaTime;

            if (_jumpReleased && currentVelocity.y > 0)
            {
                currentVelocity.y = 0;
            }
            // Drag
            currentVelocity *= (1f / (1f + (Drag * deltaTime)));
        }

        yVelocity = currentVelocity.y;
        planarVelocity = NVMath.Planarized(currentVelocity).magnitude;
    }

    void DefaultRotation(ref Quaternion currentRotation, float deltaTime)
    {
        //horizontal orientation
        if (Motor.GroundingStatus.IsStableOnGround)
        {

            defaultLookDir = _lookInputVector;
            
        }
        if (defaultLookDir.sqrMagnitude > 0f && OrientationSharpness > 0f)
            {
                // Smoothly interpolate from current to target look direction
                Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, defaultLookDir, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                // Set the current rotation (which will be used by the KinematicCharacterMotor)
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }
        //vertical orientation
        Vector3 currentUp = (currentRotation * Vector3.up);
        Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
        currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
    }

    void DefaultInputMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            float currentVelocityMagnitude = currentVelocity.magnitude;

            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

            // Reorient velocity on slope
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

            // Calculate target velocity 
            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
            Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

            // Smooth movement Velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));

            anim.SetFloat("movespd", currentVelocity.magnitude / MaxStableMoveSpeed);
        }
        else
        {
            // Add move input
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;

                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                // Limit air velocity from inputs
                if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
                {
                    // clamp addedVel to make total vel not exceed max vel on inputs plane
                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                }
                else
                {
                    // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                    if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                    {
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                    }
                }

                // Prevent air-climbing sloped walls
                if (Motor.GroundingStatus.FoundAnyGround)
                {
                    if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                    {
                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                    }
                }

                // Apply added velocity
                currentVelocity += addedVelocity;
            }
        }
    }
}
