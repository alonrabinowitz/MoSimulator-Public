using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

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
        
        [Header("Intake Audio")]
        [SerializeField] private AudioSource intakeAudioSource;
        [SerializeField] private AudioClip intakeClip;

        private float _elevatorTargetHeight;
        private float _intakeTargetAngle;
        private float _shooterTargetAngle;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        
        protected override void Start()
        {
            base.Start();
            
            shooterPivot.SetPid(shooterPivotPid);
            intakePivot.SetPid(intakePivotPid);
            
            _elevatorTargetHeight = 0;
            _intakeTargetAngle = 0;
            _shooterTargetAngle = 0;
            
            RobotGamePieceController.SetPreload(intakeStowState);
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

        private void PlacePiece()
        {
            _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 2f));
        }

        private void SetSetpoint(QuixilverBSetpoint setpoint)
        {
            _elevatorTargetHeight = 3.0f;
            _intakeTargetAngle = setpoint.intakeAngle;
            _shooterTargetAngle = setpoint.shooterAngle;
        }

        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            intakePivot.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.X);
            shooterPivot.SetTargetAngle(_shooterTargetAngle).withAxis(JointAxis.X);
        }

        private void LateUpdate()
        {
            shooterPivot.UpdatePid(shooterPivotPid);
            intakePivot.UpdatePid(intakePivotPid);
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
                    wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.X);
            }
            else
            {
                // Regular stopping of rollers
                leftIntakeRollerJoint.ChangeAngularVelocity(0);
                rightIntakeRollerJoint.ChangeAngularVelocity(0);
                topIntakeRoller.ChangeAngularVelocity(0);

                // Explicitly stop wheel animations
                foreach (var wheel in intakeWheels)
                    wheel.VelocityRoller(0).useAxis(JointAxis.X);
            }
            // }

            _coralController.SetTargetState(intakeStowState);
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.Intake:
                    SetSetpoint(intake);
                    
                    _coralController.RequestIntake(coralIntake, !hasCoral);
                    break;
                case ReefscapeSetpoints.Place:
                    StartCoroutine(PlaceGamePiece());
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

            PlacePiece();
            UpdateSetpoints();
        }
        
            // private IEnumerator PlaceGamePiece(ReefscapeSetpoints lastSetpoint, GamePieceState readState)
        private IEnumerator PlaceGamePiece()
        {
            // _isScoring = true; // Lock FixedUpdate intake wheels
            
            // Front (FacingReef) -> Spin Same Way (+)
            // Back (Not FacingReef) -> Spin Opposite Way (-)
            // float speed = FacingReef ? wheelIntakeSpeed : -wheelIntakeSpeed;

            // foreach (var wheel in intakeWheels)
            //     wheel.VelocityRoller(speed).useAxis(JointAxis.X);

            // if (lastSetpoint is ReefscapeSetpoints.Barge)
            // {
            //     targetArmAngle = bargePlaceSetpoint.armAngle;
            //     targetWristAngle = bargePlaceSetpoint.wristAngle;
            //     targetArmDistance = bargePlaceSetpoint.armDistance;
            //     yield return new WaitForSeconds(0.075f);
            // }
            // else if ((lastSetpoint == ReefscapeSetpoints.L1 && CurrentIntakeMode != ReefscapeIntakeMode.L1))
            // {
            //     leftIntakeRollerJoint.ChangeAngularVelocity(1000);
            //     rightIntakeRollerJoint.ChangeAngularVelocity(-1000);
            //     topIntakeRoller.flipVelocity();
            // }
            // else if ((lastSetpoint is not ReefscapeSetpoints.Processor && !FacingReef))
            // {
            //     leftIntakeRollerJoint.flipVelocity();
            //     rightIntakeRollerJoint.flipVelocity();
            //     topIntakeRoller.flipVelocity();
            // }

            // Vector3 force;
            // if (CurrentIntakeMode == ReefscapeIntakeMode.L1 || (readState != null && readState.stateNum == coralL1TargetState.stateNum))
            //     force = new Vector3(1, 0, 0);
            // else
            // {
            //     force = FacingReef ? new Vector3(0, 0, -5) : new Vector3(0, 0, 5);
            //     if (LastSetpoint == ReefscapeSetpoints.L1) force = new Vector3(0, 0, 2f);
            // }

            // _coralController.ReleaseGamePieceWithForce(force);
            // _algaeController.ReleaseGamePieceWithForce(new Vector3(0, algaeEjectForce, 0));
            //
            // if (lastSetpoint is ReefscapeSetpoints.L4 && !FacingReef)
            // {
            //     yield return new WaitForSeconds(0.05f);
            //     targetArmAngle = l4BackPlaceSetpoint.armAngle;
            //     targetWristAngle = l4BackPlaceSetpoint.wristAngle;
            //     targetArmDistance = l4BackPlaceSetpoint.armDistance;
            // }

            // // Wait until game pieces are released (state becomes 0) or timeout after 0.5s
            // float timer = 0f;
            // while ((_coralController.currentStateNum != 0 || _algaeController.currentStateNum != 0) && timer < 0.5f)
            // {
            //     timer += Time.deltaTime;
            //     yield return null;
            // }
            
            // // Explicitly stop wheels
            // foreach (var wheel in intakeWheels) 
            //     wheel.VelocityRoller(0).useAxis(JointAxis.X);
                
            // _isScoring = false; // Release lock

            _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0));
            
            yield return null;
        }
        
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