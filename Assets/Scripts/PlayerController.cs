using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    Rigidbody RB;
    CapsuleCollider PlayerCollider;
    [Header("Movement")]
    [SerializeField] float RunSpeed;
    [SerializeField] float StrafeSpeed;
    [SerializeField] float BackSpeed;
    [SerializeField] float Acceleration;
    [SerializeField] float Deceleration;
    [SerializeField] MovementState State;
    [SerializeField] PostureState Posture;
    [SerializeField] float StateLerpTime;

    public enum MovementState {
        Walk,
        Sprint,
        Slide
    }

    public enum PostureState {
        Stand,
        Crouch,
        Air
    }

    [Header("Aiming")]
    [SerializeField] Vector2 MouseSensitivity;
    [SerializeField] Transform CameraHolder;
    [SerializeField] float CameraFOV;
    [SerializeField] float CamLerpSpeed;
    Camera Cam;
    Vector3 CameraDefaultPos;
    float xRot = 0f;

    [Header("Sprinting")]
    [SerializeField] float SprintSpeedModifier;
    [SerializeField] float SprintFOV;
    [SerializeField] float FOVLerpPower;
    [SerializeField] float MinSprintVel;
    [SerializeField] float MaxSprintVel;
    [SerializeField] float SprintAccelModifier;
    [SerializeField] float SprintLerpTime;
    float TargetSpeedModifier;

    [Header("Crouching & Sliding")]
    [SerializeField] float CrouchSpeedModifier;
    [SerializeField] float CrouchCamModifier;
    [SerializeField] Transform CrouchOverlapPoint;
    [SerializeField] float CrouchOverlapRayDist;
    [SerializeField] float ColliderHeightModifier;
    [SerializeField] float SlideAccelModifier;
    [SerializeField] float SlideSpeedModifier;
    [SerializeField] float MinSlideVel;
    [SerializeField] float MaxSlideVel;
    [SerializeField] float CrouchLerpTime;
    [SerializeField] float SlideLerpTime;
    Vector3 DefaultColliderCentre;
    float DefaultColliderHeight;
    

    [Header("Grounding")]
    [SerializeField] Transform GroundCheckOrigin;
    [SerializeField] int RayNum;
    [SerializeField] float RayOriginDist;
    [SerializeField] float GroundRayDist;
    [SerializeField] LayerMask GroundLayer;
    [SerializeField] float JumpCoyoteTime;
    [SerializeField] float JumpBufferTime;
    float LastGroundedTime;
    float LastPressedJumpTime;
    
    [SerializeField] float GroundFriction;
    [SerializeField] float AirFriction;

    [Header("Jumping")]
    [SerializeField] float JumpForce;
    [SerializeField] float JumpCutForce;
    [SerializeField] float FallGravityForce;
    [SerializeField] float ApexModifier;
    [SerializeField] float ApexThreshold;
    [SerializeField] float ApexGravityForce;
    [Header("Edge Detection")]
    [SerializeField] float EdgeDetectionDistance = 0.5f;
    [SerializeField] float EdgeStepHeight = 0.1f;
    [SerializeField] float EdgeStepLerpSpeed;
    [SerializeField] float MaxEdgeStepHeight = 0.3f;
    bool IsJumping;
    public TMP_Text UI;//FOR DEBUGGING STATES
    public TMP_Text VelMeter;
    void Awake() {
        RB = GetComponent<Rigidbody>();
        Cam = CameraHolder.GetComponentInChildren<Camera>();
        PlayerCollider = GetComponent<CapsuleCollider>();

        CameraDefaultPos = CameraHolder.transform.localPosition;
        DefaultColliderHeight = PlayerCollider.height;
        DefaultColliderCentre = PlayerCollider.center;

        CrouchOverlapPoint.localPosition = CameraHolder.localPosition + Vector3.down * CrouchCamModifier;

    }

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update() {
        Look();
        CheckJump();

        HandleCrouch();
        DEBUGLOGSTATES();

        HandleStates();
        HandleEdgeDetection();
        Timer();
    }

    void FixedUpdate() {
        Run();
        ApplyFriction();
        CheckGrounded();
    }

    void Run() {
        Vector2 Input = InputManager.Instance.MoveInput;

        float zSpeed = Input.y > 0 ? RunSpeed : (State == MovementState.Sprint || State == MovementState.Slide) ? 0 : BackSpeed;
        Vector3 TargetVel = new Vector3(Input.x * StrafeSpeed, 0f, Input.y * zSpeed);


        var T = Time.deltaTime / StateLerpTime;
        var CurrentModifier = 1f;

        if(State == MovementState.Sprint) {
            CurrentModifier = SprintSpeedModifier;
            T = Time.deltaTime / SprintLerpTime;
        }

        if(Posture == PostureState.Crouch) {
            CurrentModifier = CrouchSpeedModifier;
            T = Time.deltaTime / CrouchLerpTime;
        }

        if(State == MovementState.Slide) {
            CurrentModifier = SlideSpeedModifier;
            T = Time.deltaTime / SlideLerpTime;
        }

        if(IsJumping && Mathf.Abs(RB.linearVelocity.y) < ApexThreshold) {
            CurrentModifier *= ApexModifier;
            RB.AddForce(Vector3.up * ApexGravityForce);
        }

        TargetSpeedModifier = Mathf.Lerp(TargetSpeedModifier, 
                                        CurrentModifier, 
                                        T);

        TargetVel *= TargetSpeedModifier;
        
        Vector3 SpeedDiff = transform.TransformDirection(TargetVel) - RB.linearVelocity;

        Vector2 AccelRate = new Vector2(
            Mathf.Abs(Input.x) > 0.01f ? Acceleration : Deceleration,
            Mathf.Abs(Input.y) > 0.01f ? Acceleration : Deceleration
        );

        if(State == MovementState.Slide) {
            AccelRate *= SlideAccelModifier;
        }

        Vector3 RunForce = new Vector3(SpeedDiff.x * AccelRate.x, 0f, SpeedDiff.z * AccelRate.y);
        
        RB.AddForce(RunForce);
    }

    void Look() {
        Vector2 Input = InputManager.Instance.ViewInput;

        float MouseX = Input.x * MouseSensitivity.x * Time.deltaTime;
        float MouseY = Input.y * MouseSensitivity.y * Time.deltaTime;

        xRot -= MouseY;
        xRot = Mathf.Clamp(xRot, -75f, 75f);

        CameraHolder.localRotation = Quaternion.Slerp(CameraHolder.localRotation, Quaternion.Euler(xRot, 0f, 0f), CamLerpSpeed);
        transform.Rotate(Vector3.up * MouseX);
        RB.MoveRotation(Quaternion.Slerp(RB.rotation, Quaternion.Euler(RB.rotation.eulerAngles + Vector3.up * MouseX), CamLerpSpeed));

        var T = Time.deltaTime / StateLerpTime;

        var TargetFOV = CameraFOV;

        if(State == MovementState.Sprint) {
            TargetFOV = SprintFOV;
            T = Time.deltaTime / SprintLerpTime;
        }

        T = 1 - Mathf.Exp(-FOVLerpPower * T);

        Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView,
                                    TargetFOV,
                                    T);
    }

    void HandleStates() {
        bool OverlapWhileCrouching = CheckOverlap(CrouchOverlapPoint.position, Vector3.up, CrouchOverlapRayDist) && Posture == PostureState.Crouch;
        
        if (LastGroundedTime < 0 && State != MovementState.Sprint) {
            Posture = PostureState.Air;
        } else if(InputManager.Instance.CrouchInput || OverlapWhileCrouching) {
            Posture = PostureState.Crouch;
        } else {
            Posture = PostureState.Stand;
        }

        bool VelAboveMinBound = State == MovementState.Sprint && RB.linearVelocity.magnitude >= MinSprintVel;
        bool VelAboveMaxBound = State != MovementState.Sprint && RB.linearVelocity.magnitude >= MaxSprintVel; // This is to prevent instant switching over the boundary (Schmitt Trigger)

        bool SprintVelWithinBounds = VelAboveMinBound || VelAboveMaxBound;
        bool ShouldSprint = false;

        VelAboveMinBound = State == MovementState.Slide && RB.linearVelocity.magnitude >= MinSlideVel;
        VelAboveMaxBound = State != MovementState.Slide && RB.linearVelocity.magnitude >= MaxSlideVel;

        bool SlideVelWithinBounds = VelAboveMinBound || VelAboveMaxBound;
        
        if (InputManager.Instance.SprintInput && SprintVelWithinBounds) {
            ShouldSprint = true;
        }
        else {
            State = MovementState.Walk;
        }
        

        if(Posture == PostureState.Crouch && SlideVelWithinBounds) {
            State = MovementState.Slide;
        } else if(ShouldSprint) {
            State = MovementState.Sprint;
        }
    }

    void HandleEdgeDetection()
    {
        if (Posture != PostureState.Air) return;

        Vector3 MoveDir = new Vector3(
            RB.linearVelocity.x, 
            0, 
            RB.linearVelocity.z
        ).normalized;

        if (Physics.Raycast(transform.position, MoveDir, out RaycastHit HitInfo, EdgeDetectionDistance))
        {
            Vector3 StepUpOrigin = transform.position + MoveDir * EdgeDetectionDistance;
            StepUpOrigin.y += EdgeStepHeight;

            if (!Physics.Raycast(StepUpOrigin, Vector3.down, GroundRayDist, GroundLayer))
            {
                Vector3 TargetPos = RB.position + Vector3.up * Mathf.Min(EdgeStepHeight, MaxEdgeStepHeight);
                RB.position = Vector3.Lerp(RB.position, TargetPos, EdgeStepLerpSpeed * Time.deltaTime);
            }
        }
    }

    void ApplyFriction() {
        var Friction = Posture != PostureState.Air ? GroundFriction : AirFriction;
        if(Mathf.Abs(InputManager.Instance.MoveInput.x) < 0.01f) {
            float FrictionMagnitude = Mathf.Min(Mathf.Abs(RB.linearVelocity.x), Friction);
            RB.AddForce(FrictionMagnitude * -Mathf.Sign(RB.linearVelocity.x), 0, 0);
        }

        if(Mathf.Abs(InputManager.Instance.MoveInput.y) < 0.01f) {
            float FrictionMagnitude = Mathf.Min(Mathf.Abs(RB.linearVelocity.z), Friction);
            RB.AddForce(0, 0, FrictionMagnitude * -Mathf.Sign(RB.linearVelocity.z));
        }
    }

    List<Vector3> SpreadSpawnPositionsAroundOrigin() {
        List<Vector3> RaySpawnPositions = new List<Vector3>
        {
            Vector3.zero
        };

        for (int i = 0; i < RayNum; i++)
        {
            float Angle = i * (Mathf.PI * 2 / RayNum);

            Vector3 Offset = new Vector3(Mathf.Sin(Angle), 0f, Mathf.Cos(Angle));
            Offset *= RayOriginDist;

            RaySpawnPositions.Add(Offset);
        }

        return RaySpawnPositions;
    }

    void HandleCrouch() {
        var T = Time.deltaTime / CrouchLerpTime;
        
        Vector3 TargetCamPos;
        Vector3 TargetColliderCentre;
        float TargetColliderHeight;

        if (!InputManager.Instance.CrouchInput && !CheckOverlap(CrouchOverlapPoint.position, Vector3.up, CrouchOverlapRayDist)) {
            TargetCamPos = CameraDefaultPos;
            TargetColliderCentre = DefaultColliderCentre;
            TargetColliderHeight = DefaultColliderHeight;
        }
        else {
            TargetCamPos = CameraDefaultPos + Vector3.down * CrouchCamModifier;
            TargetColliderHeight = DefaultColliderHeight * ColliderHeightModifier;
            TargetColliderCentre = DefaultColliderCentre + Vector3.down * ((DefaultColliderHeight - TargetColliderHeight) / 2f);
        }
        
        CameraHolder.localPosition = Vector3.Lerp(CameraHolder.localPosition, TargetCamPos, T);
        PlayerCollider.height = Mathf.Lerp(PlayerCollider.height, TargetColliderHeight, T);
        PlayerCollider.center = Vector3.Lerp(PlayerCollider.center, TargetColliderCentre, T); 
    }

    void DEBUGLOGSTATES() {
        string UITXT;
        if (Posture == PostureState.Air) {
            if(IsJumping) {
                if(State == MovementState.Walk) {
                    UITXT = "Jumping";
                } else if(State == MovementState.Sprint) {
                    UITXT = "Sprint Jumping";
                } else {
                    UITXT = "Slide Jumping";
                }
            } else {
                if(State == MovementState.Walk) {
                    UITXT = "Falling";
                } else if(State == MovementState.Sprint) {
                    UITXT = "Sprint Falling";
                } else {
                    UITXT = "Slide Falling";
                }
            }
        } else if(Posture == PostureState.Crouch) {
            if(State == MovementState.Walk) {
                UITXT = "Crouching";
            } else {
                UITXT = "Sliding";
            }
        } else {
            if(State == MovementState.Walk) {
                UITXT = "Walking";
            } else if(State == MovementState.Sprint) {
                UITXT = "Sprinting";
            } else {
                UITXT = "Sliding";
            }
        }

        UI.text = UITXT;
        VelMeter.text = Mathf.Round(RB.linearVelocity.magnitude * 100f) / 100f  + " M/S";
    }


    bool CheckOverlap(Vector3 Origin, Vector3 Direction, float RayDist) {
        List<Vector3> RaySpawnPositions = SpreadSpawnPositionsAroundOrigin();

        foreach (var Offset in RaySpawnPositions)
        {
            if(Physics.RaycastAll(Origin + Offset, Direction, RayDist).Where(Coll => Coll.transform != transform).ToArray().Length > 0) { return true; }
        }

        return false;
    }

    void CheckGrounded() {
        if(CheckOverlap(GroundCheckOrigin.position, Vector3.down, GroundRayDist)) {
            LastGroundedTime = JumpCoyoteTime;
            
            if (IsJumping && RB.linearVelocity.y <= 0.1f) {
                IsJumping = false;
            }
        }
    }

    void CheckJump() {
        if (InputManager.Instance.JumpInputPress) {
            OnJump();
        }

        if (Posture != PostureState.Air && LastPressedJumpTime > 0 && !IsJumping) {
            Jump();
        }

        if(IsJumping) {
            if(InputManager.Instance.JumpInputRelease && RB.linearVelocity.y > 0) {
                RB.AddForce(Vector3.down * JumpCutForce, ForceMode.Impulse);
            }

            if(RB.linearVelocity.y < 0) {
                RB.AddForce(Vector3.down * FallGravityForce);
            }
        }
    }

    void OnJump() {
        LastPressedJumpTime = JumpBufferTime;
    }

    void Jump() {
        RB.linearVelocity = new Vector3(RB.linearVelocity.x, JumpForce, RB.linearVelocity.z);
        LastGroundedTime = 0;
        LastPressedJumpTime = 0;
        IsJumping = true;
    }

    void Timer() {
        LastGroundedTime -= Time.deltaTime;
        LastPressedJumpTime -= Time.deltaTime;
    }
}
