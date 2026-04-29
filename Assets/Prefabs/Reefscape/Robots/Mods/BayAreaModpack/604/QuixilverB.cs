using System.Collections;
using System.Diagnostics;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._604
{
    public class QuixilverB: ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint shooterPivot;
        [SerializeField] private GenericJoint intakePivot;
        [SerializeField] private GenericRoller leftIntakeRollerJoint;
        [SerializeField] private GenericRoller rightIntakeRollerJoint;
        [SerializeField] private GenericRoller topIntakeRoller;
        [SerializeField] private Transform leftIntakeSensor;
        [SerializeField] private Transform rightIntakeSensor;
        [SerializeField] private GenericJoint slideJoint;
        
        [Header("Animation Joints (Wheels)")]
        [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private float wheelIntakeSpeed = 500f;

        [Header("PIDs")]
        [SerializeField] private PidConstants shooterPivotPid;
        [SerializeField] private PidConstants intakePivotPid;

        [Header("Intakes")] [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        
        [Header("Game Piece Stow States")]
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState intakeStowState;
        
        [Header("Setpoints")]
        [SerializeField] private QuixilverBSetpoint stow;
        [SerializeField] private QuixilverBSetpoint intake;
        [SerializeField] private QuixilverBSetpoint transfer;
        [SerializeField] private QuixilverBSetpoint l1;
        [SerializeField] private QuixilverBSetpoint l2;
        [SerializeField] private QuixilverBSetpoint l3;
        [SerializeField] private QuixilverBSetpoint l4;
        
        [Header("Shooting Forces")]
        [SerializeField] private Vector3 l1Force;
        [SerializeField] private Vector3 l2Force;
        [SerializeField] private Vector2 l2DelayTorque;
        [SerializeField] private Vector3 l3Force;
        [SerializeField] private Vector3 l4Force;
        
        [Header("Intake Audio")]
        [SerializeField] private AudioSource intakeAudioSource;
        [SerializeField] private AudioClip intakeClip;
        
        [Header("Miscellaneous")]
        [SerializeField] private GameObject coralStowStateGameObject;

        private float _elevatorTargetHeight;
        private float _intakeTargetAngle;
        private float _shooterTargetAngle;
        private bool _handoff;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        
        protected override void Start()
        {
            base.Start();
            
            shooterPivot.SetPid(shooterPivotPid);
            intakePivot.SetPid(intakePivotPid);
            
            _elevatorTargetHeight = 0;
            _intakeTargetAngle = 0;
            _shooterTargetAngle = 0;

            _handoff = true;
            
            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            
            _coralController.gamePieceStates = new[]
            {
                coralStowState,
                intakeStowState
            };
            _coralController.intakes.Add(coralIntake);
            
            intakeAudioSource.clip = intakeClip;
            intakeAudioSource.loop = true;
            intakeAudioSource.playOnAwake = false;
        }

        private IEnumerator PlacePiece()
        {
            if (_coralController.currentStateNum == intakeStowState.stateNum)
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, -3f));
            }
            else if (_coralController.HasPiece())
            {
                switch (GetLevelByState())
                {
                    case 1:
                        _coralController.ReleaseGamePieceWithForce(l1Force);
                        break;
                    case 2:
                        // Rigidbody coral = _coralController.controller.GamePiece.rigidbody;
                        // Rigidbody coral = GetComponent<ReefscapeGamePieceController>().GamePiece.
                        var coral = FindChildWithPrefix(coralStowStateGameObject.gameObject.transform, "Coral").gameObject;
                        _coralController.ReleaseGamePieceWithForce(l2Force);
                        yield return new WaitForSeconds(l2DelayTorque.x);
                        coral.GetComponent<Rigidbody>().AddRelativeTorque(new Vector3(l2DelayTorque.y, 0, 0));
                        break;
                    case 3:
                        _coralController.ReleaseGamePieceWithForce(l3Force);
                        break;
                    case 4:
                        _coralController.ReleaseGamePieceWithForce(l4Force);
                        break;
                    default:
                        break;
                }
                
            }

            yield return null;
        }

        private void SetSetpoint(QuixilverBSetpoint setpoint)
        {
            _elevatorTargetHeight = 3.5f;
            _intakeTargetAngle = setpoint.intakeAngle;
            _shooterTargetAngle = setpoint.shooterAngle;
        }

        private void UpdateSetpoints()
        {
            // if (!_lockedIntakeSlide) elevator.SetTarget(_elevatorTargetHeight);
            elevator.SetTarget(_elevatorTargetHeight);
            intakePivot.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.X).flipDirection().noWrap(-90f);
            shooterPivot.SetTargetAngle(_shooterTargetAngle).withAxis(JointAxis.X).flipDirection();
        }

        private void LateUpdate()
        {
            shooterPivot.UpdatePid(shooterPivotPid);
            intakePivot.UpdatePid(intakePivotPid);
        }
        
        public Transform FindChildWithPrefix(Transform parent, string prefix)
        {
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith(prefix))
                {
                    return child;
                }
            }
        
            Debug.LogWarning($"No child found starting with '{prefix}'");
            return null;
        }
        
        private int GetLevelByState()
        {
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.L1:
                    return 1;
                case ReefscapeSetpoints.L2:
                    return 2;
                case ReefscapeSetpoints.L3:
                    return 3;
                case ReefscapeSetpoints.L4:
                    return 4;
            }
            
            switch (LastSetpoint)
            {
                case ReefscapeSetpoints.L1:
                    return 1;
                case ReefscapeSetpoints.L2:
                    return 2;
                case ReefscapeSetpoints.L3:
                    return 3;
                case ReefscapeSetpoints.L4:
                    return 4;
            }

            return 0;
        }
        
        private bool CoralAtStow(GamePieceState stowState)
        {
            return _coralController.currentStateNum == stowState.stateNum && _coralController.atTarget;
        }
        
        private bool AtSetpoint(QuixilverBSetpoint stp)
        {
            return
                Utils.InAngularRange((-shooterPivot.GetSingleAxisAngle(JointAxis.X))+360, stp.shooterAngle, 2f) &&
                Utils.InAngularRange(-intakePivot.GetSingleAxisAngle(JointAxis.X)+360, stp.intakeAngle, 2f);
        }
    
        private bool AtSetpoint()
        {
            return
                Utils.InAngularRange(shooterPivot.GetSingleAxisAngle(JointAxis.X), _shooterTargetAngle, 2f) &&
                Utils.InAngularRange(intakePivot.GetSingleAxisAngle(JointAxis.X), _intakeTargetAngle, 2f);
        }


        private void FixedUpdate()
        {
            bool hasCoral = _coralController.HasPiece();
            
            
            UpdateIntakeAudio();
            
            // --- IMPROVED WHEEL LOGIC ---
            // We only run this if we are NOT in the middle of a scoring coroutine
            // if (!_isScoring)
            // {
            bool isIntaking = CurrentSetpoint == ReefscapeSetpoints.Intake && IntakeAction.IsPressed();
            
            if (isIntaking)
            {
                foreach (var wheel in intakeWheels)
                    wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.Y);
            }
            else
            {
                // Regular stopping of rollers
                leftIntakeRollerJoint.ChangeAngularVelocity(0);
                rightIntakeRollerJoint.ChangeAngularVelocity(0);
                topIntakeRoller.ChangeAngularVelocity(0);
            
                // Explicitly stop wheel animations
                foreach (var wheel in intakeWheels)
                    wheel.VelocityRoller(0).useAxis(JointAxis.Y);
            }
            // }
            
            if (CoralAtStow(intakeStowState) && AtSetpoint(transfer))
            {
                _handoff = true;
            }
            
            _coralController.SetTargetState(_handoff ? coralStowState : intakeStowState);

            if (!hasCoral)
            {
                _handoff = false;
            }
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (hasCoral && !(CoralAtStow(coralStowState)))
                    {
                        SetSetpoint(transfer);
                    }
                    else
                    {
                        SetSetpoint(stow);
                    }
                    break;
                case ReefscapeSetpoints.Intake:
                    SetSetpoint(intake);
                    
                    _coralController.RequestIntake(coralIntake, !hasCoral);
                    break;
                case ReefscapeSetpoints.Place:
                    // StartCoroutine(PlaceGamePiece());
                    // Debug.Log("In Place Setpoint");
                    StartCoroutine(PlacePiece());
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    break;
                case ReefscapeSetpoints.Climbed:
                    break;
            }
            
            // More 2910 intake logic
            _coralController.MoveIntake(coralIntake, intakeStowState.stateTarget);
            if (!leftIntakeRollerJoint.gameObject.activeSelf)
            {
                leftIntakeRollerJoint.gameObject.SetActive(true);
                rightIntakeRollerJoint.gameObject.SetActive(true);
            }
            
            var rayDirection = intakeStowState.stateTarget.forward;
            var distance = 0.0254f * 5f;
            var coralMask = LayerMask.GetMask("Coral");
            var coralRight = Physics.Raycast(rightIntakeSensor.position, rayDirection, distance, coralMask);
            var coralLeft = Physics.Raycast(leftIntakeSensor.position, rayDirection, distance, coralMask);
            
            if (IntakeAction.IsPressed() && CurrentSetpoint != ReefscapeSetpoints.LowAlgae && CurrentSetpoint != ReefscapeSetpoints.HighAlgae)
            {
                if (coralRight && coralLeft)
                {
                    leftIntakeRollerJoint.ChangeAngularVelocity(8000);
                    rightIntakeRollerJoint.ChangeAngularVelocity(8000);
                }
            }
            
            UpdateSetpoints();
        }
        
            // private IEnumerator PlaceGamePiece(ReefscapeSetpoints lastSetpoint, GamePieceState readState)
        // private IEnumerator PlaceGamePiece()
        // {
        //     // _isScoring = true; // Lock FixedUpdate intake wheels
        //     
        //     // Front (FacingReef) -> Spin Same Way (+)
        //     // Back (Not FacingReef) -> Spin Opposite Way (-)
        //     // float speed = FacingReef ? wheelIntakeSpeed : -wheelIntakeSpeed;
        //
        //     // foreach (var wheel in intakeWheels)
        //     //     wheel.VelocityRoller(speed).useAxis(JointAxis.X);
        //
        //     // if (lastSetpoint is ReefscapeSetpoints.Barge)
        //     // {
        //     //     targetArmAngle = bargePlaceSetpoint.armAngle;
        //     //     targetWristAngle = bargePlaceSetpoint.wristAngle;
        //     //     targetArmDistance = bargePlaceSetpoint.armDistance;
        //     //     yield return new WaitForSeconds(0.075f);
        //     // }
        //     // else if ((lastSetpoint == ReefscapeSetpoints.L1 && CurrentIntakeMode != ReefscapeIntakeMode.L1))
        //     // {
        //     //     leftIntakeRollerJoint.ChangeAngularVelocity(1000);
        //     //     rightIntakeRollerJoint.ChangeAngularVelocity(-1000);
        //     //     topIntakeRoller.flipVelocity();
        //     // }
        //     // else if ((lastSetpoint is not ReefscapeSetpoints.Processor && !FacingReef))
        //     // {
        //     //     leftIntakeRollerJoint.flipVelocity();
        //     //     rightIntakeRollerJoint.flipVelocity();
        //     //     topIntakeRoller.flipVelocity();
        //     // }
        //
        //     // Vector3 force;
        //     // if (CurrentIntakeMode == ReefscapeIntakeMode.L1 || (readState != null && readState.stateNum == coralL1TargetState.stateNum))
        //     //     force = new Vector3(1, 0, 0);
        //     // else
        //     // {
        //     //     force = FacingReef ? new Vector3(0, 0, -5) : new Vector3(0, 0, 5);
        //     //     if (LastSetpoint == ReefscapeSetpoints.L1) force = new Vector3(0, 0, 2f);
        //     // }
        //
        //     // _coralController.ReleaseGamePieceWithForce(force);
        //     // _algaeController.ReleaseGamePieceWithForce(new Vector3(0, algaeEjectForce, 0));
        //     //
        //     // if (lastSetpoint is ReefscapeSetpoints.L4 && !FacingReef)
        //     // {
        //     //     yield return new WaitForSeconds(0.05f);
        //     //     targetArmAngle = l4BackPlaceSetpoint.armAngle;
        //     //     targetWristAngle = l4BackPlaceSetpoint.wristAngle;
        //     //     targetArmDistance = l4BackPlaceSetpoint.armDistance;
        //     // }
        //
        //     // // Wait until game pieces are released (state becomes 0) or timeout after 0.5s
        //     // float timer = 0f;
        //     // while ((_coralController.currentStateNum != 0 || _algaeController.currentStateNum != 0) && timer < 0.5f)
        //     // {
        //     //     timer += Time.deltaTime;
        //     //     yield return null;
        //     // }
        //     
        //     // // Explicitly stop wheels
        //     // foreach (var wheel in intakeWheels) 
        //     //     wheel.VelocityRoller(0).useAxis(JointAxis.X);
        //         
        //     // _isScoring = false; // Release lock
        //
        //     _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, -5f));
        //     
        //     yield return null;
        // }
        
        private void UpdateIntakeAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (intakeAudioSource.isPlaying)
                {
                    intakeAudioSource.Stop();
                }

                return;
            }

            if ((IntakeAction.IsPressed() || OuttakeAction.IsPressed() || CurrentSetpoint is ReefscapeSetpoints.Climb) &&
                !intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Play();
            }
            else if (!IntakeAction.IsPressed() && !OuttakeAction.IsPressed() && CurrentSetpoint is not ReefscapeSetpoints.Climb &&
                     intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Stop();
            }

        }
    }
}