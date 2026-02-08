using System;
using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Games.Reefscape.Robots
{
    public class Orbit : ReefscapeRobotBase
    {
        [Header("Robot Components")] [SerializeField]
        private GenericJoint armJoint;

        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint intakeJoint;
        [SerializeField] private GenericJoint climberJoint;
        [SerializeField] private GenericJoint frontFlap;
        [SerializeField] private GenericRoller[] physRollers;
        [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private Transform algaeSlider;
        [SerializeField] private Transform algaeTarget;
        [SerializeField] private GameObject algaeScoop;
        [SerializeField] private BoxCollider intakeVision;

        [Header("PID Constants")] [SerializeField]
        private PidConstants armPidConstants;

        [SerializeField] private PidConstants intakePidConstants;
        [SerializeField] private PidConstants climberPidConstants;
        [SerializeField] private PidConstants frontFlapPidConstants;

        [Header("Robot Setpoints")] [SerializeField]
        private OrbitSetpoint coralIntakeSetpoint;

        [SerializeField] private OrbitSetpoint algaeGroundIntakeSetpoint;
        [SerializeField] private OrbitSetpoint stowSetpoint;
        [SerializeField] private OrbitSetpoint coralStowSetpoint;
        [SerializeField] private OrbitSetpoint algaeStowSetpoint;
        [SerializeField] private OrbitSetpoint l1Setpoint;
        [SerializeField] private OrbitSetpoint l2Setpoint;
        [SerializeField] private OrbitSetpoint l2PlaceSetpoint;
        [SerializeField] private OrbitSetpoint l2BackSetpoint;
        [SerializeField] private OrbitSetpoint l2BackPlaceSetpoint;
        [SerializeField] private OrbitSetpoint lowAlgaeSetpoint;
        [SerializeField] private OrbitSetpoint lowAlgaeBackSetpoint;
        [SerializeField] private OrbitSetpoint l3Setpoint;
        [SerializeField] private OrbitSetpoint l3PlaceSetpoint;
        [SerializeField] private OrbitSetpoint l3BackSetpoint;
        [SerializeField] private OrbitSetpoint l3BackPlaceSetpoint;
        [SerializeField] private OrbitSetpoint highAlgaeSetpoint;
        [SerializeField] private OrbitSetpoint highAlgaeBackSetpoint;
        [SerializeField] private OrbitSetpoint l4Setpoint;
        [SerializeField] private OrbitSetpoint l4PlaceSetpoint;
        [SerializeField] private OrbitSetpoint l4BackSetpoint;
        [SerializeField] private OrbitSetpoint l4BackPlaceSetpoint;
        [SerializeField] private OrbitSetpoint bargeSetpoint;
        [SerializeField] private OrbitSetpoint bargeSetpointRear;
        [SerializeField] private OrbitSetpoint processorSetpoint;
        [SerializeField] private OrbitSetpoint stackAlgaeIntakeSetpoint;
        [SerializeField] private OrbitSetpoint climbSetpoint;
        [SerializeField] private OrbitSetpoint climbedSetpoint;

        [Header("Game Piece Intakes")] [SerializeField]
        private ReefscapeGamePieceIntake coralIntake;

        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [Header("Game Piece States")] [SerializeField]
        private string currentState;

        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralSecondSetpointState;
        [SerializeField] private GamePieceState coralThirdSetpointState;
        [SerializeField] private GamePieceState coralFourthSetpointState;
        [SerializeField] private GamePieceState coralChassisStowState;
        [SerializeField] private GamePieceState coralArmStowState;
        [SerializeField] private GamePieceState algaeStowState;

        [Header("Target Setpoints")] [SerializeField]
        private float targetArmAngle;

        [SerializeField] private float targetElevatorHeight;
        [SerializeField] private float targetIntakeAngle;
        [SerializeField] private float targetClimberAngle;
        [SerializeField] private float noWrapAngle;

        
        private float emptyMaxSpeed;
        [Header("Arm Settings")]
        [SerializeField] private float coralMaxSpeed;
        [SerializeField] private float algaeMaxSpeed;
        
        [Header("Intake Settings")] [SerializeField]
        private float intakeWheelSpeed = 300f;

        [Header("Auto Align Offsets")] [SerializeField]
        private Vector3 l4AutoAlignOffset;

        [SerializeField] private Vector3 l2AutoAlignOffset;

        [Header("Coral Grab Geometry")] [Tooltip("Tolerance (inches) that counts as 'at coral grab height' for the handoff check.")]
        [SerializeField] private float coralGrabHeightBuffer = 0.5f;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode algaeController;

        private bool _intakeSequenceRunning;
        private bool _disruptable;
        private bool _wasCoral;
        private ReefscapeSetpoints? _bufferedSetpoint;
        private bool _bufferAlgaeState;
        private bool _facingBarge;
        private float _suppressCoralMoveUntil;
        private ReefscapeRobotMode? lastMode;
        private Rigidbody _rigidbody;
        private ReefscapeAutoAlign autoAlign;
        private Collider[] colliders;
        private OverlapBoxBounds VisionDetect;
        private LayerMask mask;
        

        protected override void Start()
        {
            base.Start();

            colliders = new Collider[6];
            VisionDetect = new OverlapBoxBounds(intakeVision);
            mask = LayerMask.GetMask("Coral");
            armJoint.SetPid(armPidConstants);
            intakeJoint.SetPid(intakePidConstants);
            climberJoint.SetPid(climberPidConstants);
            frontFlap.SetPid(frontFlapPidConstants);

            targetArmAngle = coralStowSetpoint.armAngle;
            targetElevatorHeight = stowSetpoint.elevatorHeight;
            targetIntakeAngle = stowSetpoint.intakeAngle;
            targetClimberAngle = stowSetpoint.climberAngle;
            
            RobotGamePieceController.SetPreload(coralArmStowState);
            coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            coralController.gamePieceStates = new[]
            {
                coralIntakeState, coralSecondSetpointState, coralThirdSetpointState, coralFourthSetpointState,
                coralChassisStowState, coralArmStowState
            };
            coralController.intakes.Add(coralIntake);

            algaeController.gamePieceStates = new[] { algaeStowState };
            algaeController.intakes.Add(algaeIntake);

            _disruptable = true;
            _intakeSequenceRunning = false;
            _wasCoral = false;
            _bufferedSetpoint = null;
            lastMode = null;
            _rigidbody = GetComponent<Rigidbody>();
            autoAlign = GetComponent<ReefscapeAutoAlign>();

            emptyMaxSpeed = armPidConstants.Max;
        }

        private void LateUpdate()
        {
            armJoint.UpdatePid(armPidConstants);
            intakeJoint.UpdatePid(intakePidConstants);
            climberJoint.UpdatePid(climberPidConstants);
            frontFlap.UpdatePid(frontFlapPidConstants);
        }

        private void FixedUpdate()
        {
            
            if (algaeController.currentStateNum == algaeStowState.stateNum)
            {
                armPidConstants.Max = algaeMaxSpeed;
            } 
            else if (coralController.currentStateNum == coralArmStowState.stateNum)
            {
                armPidConstants.Max = coralMaxSpeed;
            } 
            else
            {
                armPidConstants.Max = emptyMaxSpeed;
            }
            
            if (CurrentSetpoint == ReefscapeSetpoints.Intake && CurrentRobotMode == ReefscapeRobotMode.Algae &&
                algaeController.currentStateNum == 0)
            {
                algaeScoop.SetActive(true);
            }
            else
            {
                algaeScoop.SetActive(false);
            }
            if (algaeIntake.GamePiece != null)
            {
                var localSliderSpaceX = algaeTarget.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).x;
                var localSliderSpaceY = algaeTarget.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).y;
                algaeSlider.localPosition = new Vector3(localSliderSpaceX, localSliderSpaceY, 0);
            }
            
            if (coralController.HasPiece())
            {
                foreach (var roller in physRollers)
                {
                    roller.flipVelocity();
                }
            }

            if (algaeController.HasPiece())
            {
                lastMode ??= CurrentRobotMode;
                SetRobotMode(ReefscapeRobotMode.Algae);
            }
            else if (!_disruptable && CurrentRobotMode == ReefscapeRobotMode.Algae)
            {
                SetRobotMode(ReefscapeRobotMode.Coral);
                lastMode = ReefscapeRobotMode.Algae;
            }
            else
            {
                if (lastMode != null)
                {
                    SetRobotMode(lastMode.Value);
                    lastMode = null;
                }
            }

            if (CurrentSetpoint is ReefscapeSetpoints.L2 or ReefscapeSetpoints.L3)
            {
                autoAlign.offset = l2AutoAlignOffset;
            }
            else
            {
                autoAlign.offset = l4AutoAlignOffset;
            }

            if (coralController.currentStateNum >= coralArmStowState.stateNum && CurrentRobotMode == ReefscapeRobotMode.Algae && coralController.atTarget)
            {
                _intakeSequenceRunning = false;
            } else if (algaeController.currentStateNum == 0 && coralController.HasPiece() && CurrentSetpoint != ReefscapeSetpoints.Place && coralController.currentStateNum != coralArmStowState.stateNum)
            {
                _intakeSequenceRunning = true;
            }
        
            if (_intakeSequenceRunning && algaeController.currentStateNum == 0 && CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                if (CurrentSetpoint != ReefscapeSetpoints.Stow)
                {
                    if (CurrentSetpoint != ReefscapeSetpoints.Intake)
                    {
                        _bufferedSetpoint = CurrentSetpoint;
                    }

                    if (IntakeAction.IsPressed() && CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetState(ReefscapeSetpoints.Stow);
                    }
                }
                
                if (coralController.currentStateNum == coralArmStowState.stateNum && coralController.atTarget)
                {
                    _intakeSequenceRunning = false;
                }
            }

            if (!_intakeSequenceRunning && _bufferedSetpoint != null)
            {
                SetState(_bufferedSetpoint.Value);
                _bufferedSetpoint = null;
            }
            
            if (L4Action.triggered && algaeController.HasPiece())
            {
                CheckFacingBarge();
            }
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (!_intakeSequenceRunning || coralController.HasPiece())
                    {
                        SetOrbitSetpoint(algaeController.HasPiece()
                            ? algaeStowSetpoint
                            : coralStowSetpoint);
                    }

                    targetClimberAngle = stowSetpoint.climberAngle;
                    break;
                case ReefscapeSetpoints.Intake:
                    if (CurrentRobotMode == ReefscapeRobotMode.Algae && algaeController.currentStateNum == 0)
                    {
                        if (!Utils.InAngularRange(armJoint.GetSingleAxisAngle(JointAxis.X), -algaeGroundIntakeSetpoint.armAngle, 10))
                        {
                            targetArmAngle = algaeGroundIntakeSetpoint.armAngle;
                            targetElevatorHeight = stowSetpoint.elevatorHeight;
                        }
                        else
                        {
                            SetOrbitSetpoint(algaeGroundIntakeSetpoint);
                        }
                        algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed());
                    }
                    else if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetOrbitSetpoint(coralIntakeSetpoint);
                        coralController.RequestIntake(coralIntake, IntakeAction.IsPressed());
                    }

                    break;
                case ReefscapeSetpoints.Place:
                    if (coralController.HasPiece() && algaeController.HasPiece())
                    {
                        lastMode = ReefscapeRobotMode.Coral;
                    }
                    StartCoroutine(PlaceGamePiece(LastSetpoint));
                    break;
                case ReefscapeSetpoints.L1:
                    SetOrbitSetpoint(l1Setpoint);
                    break;
                case ReefscapeSetpoints.L2:
                    if (!Utils.InAngularRange(armJoint.GetSingleAxisAngle(JointAxis.X), -l2Setpoint.armAngle, 10) &&
                        FacingReef)
                    {
                        targetArmAngle = l2Setpoint.armAngle;
                        targetElevatorHeight = stowSetpoint.elevatorHeight;
                    }
                    else if (!Utils.InAngularRange(armJoint.GetSingleAxisAngle(JointAxis.X),
                                 -l2BackPlaceSetpoint.armAngle, 10) && !FacingReef)
                    {
                        targetArmAngle = l2BackPlaceSetpoint.armAngle;
                        targetElevatorHeight = stowSetpoint.elevatorHeight;
                    }
                    else
                    {
                        SetOrbitSetpoint(FacingReef ? l2Setpoint : l2BackSetpoint);
                    }
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetOrbitSetpoint(FacingReef ? lowAlgaeSetpoint : lowAlgaeBackSetpoint);
                    algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed());
                    break;
                case ReefscapeSetpoints.L3:
                    SetOrbitSetpoint(FacingReef ? l3Setpoint : l3BackSetpoint);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetOrbitSetpoint(FacingReef ? highAlgaeSetpoint : highAlgaeBackSetpoint);
                    algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed());
                    break;
                case ReefscapeSetpoints.L4:
                    SetOrbitSetpoint(FacingReef ? l4Setpoint : l4BackSetpoint);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetOrbitSetpoint(_facingBarge ? bargeSetpoint : bargeSetpointRear);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    break;
                case ReefscapeSetpoints.Stack:
                    if (!Utils.InAngularRange(armJoint.GetSingleAxisAngle(JointAxis.X), -stackAlgaeIntakeSetpoint.armAngle, 10))
                    {
                        targetArmAngle = stackAlgaeIntakeSetpoint.armAngle;
                        targetElevatorHeight = stowSetpoint.elevatorHeight;
                    }
                    else
                    {
                        SetOrbitSetpoint(stackAlgaeIntakeSetpoint);
                    }
                    algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed());
                    break;
                case ReefscapeSetpoints.Processor:
                    SetOrbitSetpoint(processorSetpoint);
                    break;
                case ReefscapeSetpoints.Climb:
                    if (!Utils.InAngularRange(armJoint.GetSingleAxisAngle(JointAxis.X), -climbSetpoint.armAngle, 10))
                    {
                        targetArmAngle = climbSetpoint.armAngle;
                        targetElevatorHeight = stowSetpoint.elevatorHeight;
                        targetClimberAngle = climbSetpoint.climberAngle;
                    }
                    else
                    {
                        SetOrbitSetpoint(climbSetpoint);
                    }
                    break;
                case ReefscapeSetpoints.Climbed:
                    if (!Utils.InAngularRange(armJoint.GetSingleAxisAngle(JointAxis.X), -climbedSetpoint.armAngle, 10))
                    {
                        targetArmAngle = climbedSetpoint.armAngle;
                        targetElevatorHeight = stowSetpoint.elevatorHeight;
                        targetClimberAngle = climbedSetpoint.climberAngle;
                    }
                    else
                    {
                        SetOrbitSetpoint(climbedSetpoint);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (coralIntake.GamePiece != null)
            {
                targetIntakeAngle = 0;
            }

            OrbitIntakeSequence();
            SetSubsystemSetpoints();
            runIntakeVision();
        }

        private IEnumerator PlaceGamePiece(ReefscapeSetpoints lastSetpoint)
        {
            if (algaeController.HasPiece())
            {
                var speed = _rigidbody.velocity;
                var translatedSpeed = algaeController.controller.gameObject.transform.InverseTransformDirection(speed);
                algaeController.ReleaseGamePieceWithForce(translatedSpeed * 0.5f);

                if (_wasCoral)
                {
                    _wasCoral = false;
                }
            }
            else if (CurrentRobotMode != ReefscapeRobotMode.Algae &&
                     coralController.currentStateNum == coralArmStowState.stateNum)
            {
                switch (lastSetpoint)
                {
                    case ReefscapeSetpoints.L4:
                        SetOrbitSetpoint(FacingReef ? l4PlaceSetpoint : l4BackPlaceSetpoint);
                        break;
                    case ReefscapeSetpoints.L3:
                        SetOrbitSetpoint(FacingReef ? l3PlaceSetpoint : l3BackPlaceSetpoint);
                        break;
                    case ReefscapeSetpoints.L2:
                        SetOrbitSetpoint(FacingReef ? l2PlaceSetpoint : l2BackPlaceSetpoint);
                        break;
                }

                yield return new WaitForSeconds(0.08f);

                if (lastSetpoint != ReefscapeSetpoints.L1)
                {
                    coralController.ReleaseGamePieceWithForce(FacingReef
                        ? new Vector3(0, 0, -2.5f)
                        : new Vector3(0, 0, 2.5f));
                }
                else
                {
                    coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0.0f));
                }
            }
        }

        private void OrbitIntakeSequence()
        {
            if (!IntakeAction.IsPressed())
            {
                _intakeSequenceRunning = false;
                if (coralController.currentStateNum == 0)
                {
                    _disruptable = true;
                }
            }

            if (CurrentRobotMode == ReefscapeRobotMode.Coral ||
                (algaeController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Algae))
            {
                if (CurrentSetpoint != ReefscapeSetpoints.HighAlgae && CurrentSetpoint != ReefscapeSetpoints.LowAlgae &&
                    CurrentSetpoint != ReefscapeSetpoints.Barge && CurrentSetpoint != ReefscapeSetpoints.Place)
                {
                    bool hasAlgae = algaeController.HasPiece();
                    coralController.RequestIntake(coralIntake, IntakeAction.IsPressed());

                    if (IntakeAction.IsPressed() ||
                        (coralController.HasPiece() &&
                         coralController.currentStateNum != coralArmStowState.stateNum))
                    {
                        _disruptable = false;
                        _intakeSequenceRunning = true;

                        targetArmAngle = hasAlgae ? targetArmAngle : coralIntakeSetpoint.armAngle;
                        targetElevatorHeight = hasAlgae ? targetElevatorHeight : stowSetpoint.elevatorHeight;
                        targetIntakeAngle = coralIntakeSetpoint.intakeAngle;

                        coralController.SetTargetState(coralController.currentStateNum switch
                        {
                            0 => coralIntakeState,
                            1 => coralSecondSetpointState,
                            2 => coralThirdSetpointState,
                            3 => coralFourthSetpointState,
                            4 => coralChassisStowState,
                            _ => coralController.GetCurrentState() ?? coralIntakeState
                        });

                        if (BaseGameManager.Instance.RobotState == RobotState.Enabled &&
                            Mathf.Approximately(targetIntakeAngle, coralIntakeSetpoint.intakeAngle))
                        {
                            foreach (var wheel in intakeWheels)
                            {
                                wheel.VelocityRoller(intakeWheelSpeed).useAxis(JointAxis.X);
                            }
                        }

                        bool atChassisStow = coralController.atTarget &&
                                             coralController.currentStateNum == coralChassisStowState.stateNum;
                        if (atChassisStow)
                        {
                            targetArmAngle = hasAlgae ? targetArmAngle : coralIntakeSetpoint.armAngle;
                            targetElevatorHeight = hasAlgae ? targetElevatorHeight : coralIntakeSetpoint.elevatorHeight;
                            targetIntakeAngle = stowSetpoint.intakeAngle;
                            
                            if (!Utils.InAngularRange(armJoint.GetSingleAxisAngle(JointAxis.X),
                                    -coralIntakeSetpoint.armAngle, 7))
                            {
                                targetElevatorHeight = hasAlgae ? targetElevatorHeight: coralStowSetpoint.elevatorHeight;
                            }
                            
                            _disruptable = true;

                            float elev = elevator.GetElevatorHeight();
                            bool elevatorAtCoralGrab =
                                Mathf.Abs(elev - coralIntakeSetpoint.elevatorHeight) <= coralGrabHeightBuffer;

                            if (elevatorAtCoralGrab && coralController.atTarget &&
                                Mathf.Approximately(targetElevatorHeight, coralIntakeSetpoint.elevatorHeight))
                            {
                                coralController.SetTargetState(coralArmStowState);
                            }
                        }

                        bool atArmStow = coralController.atTarget &&
                                         coralController.currentStateNum == coralArmStowState.stateNum;
                        if (atArmStow)
                        {
                            SetState(ReefscapeSetpoints.Stow);
                            _intakeSequenceRunning = false;
                        }
                    }
                    else if ((coralController.atTarget &&
                              coralController.currentStateNum == coralArmStowState.stateNum) &&
                             _intakeSequenceRunning)
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        _intakeSequenceRunning = false;
                    }
                }
            }
        }

        private void runIntakeVision()
        {
            if (!IntakeAction.IsPressed() || coralController.HasPiece()) return;
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i] = null;
            }
            var size = VisionDetect.OverlapBoxNonAlloc(ref colliders, mask);
            
            if (colliders != null)
            {
                if (!colliders[0]) return;
                GameObject close = colliders[0].gameObject;
                for (int i = 1; i < size; i++) {
                    if (Vector3.Distance(colliders[i].transform.position, transform.position) <
                        Vector3.Distance(close.transform.position, transform.position))
                    {
                        close = colliders[i].gameObject;
                    }
                }

                var angle = Quaternion.LookRotation(transform.position - close.transform.position, transform.up).eulerAngles.y;
                DriveController.SoftSteer(Mathf.Clamp(-angle + transform.eulerAngles.y, 0.08f, -0.08f));
            }
        }
        
        private void CheckFacingBarge()
        {
            var toZAxisXY = new Vector3(-transform.position.x, -transform.position.y, 0f).normalized;
            var forwardXY = new Vector3(transform.forward.x, transform.forward.y, 0f).normalized;
            var dot = Vector3.Dot(forwardXY, toZAxisXY);
            _facingBarge = dot > 0.0f;
        }

        private void SetOrbitSetpoint(OrbitSetpoint setpoint)
        {
            targetArmAngle = setpoint.armAngle;
            targetElevatorHeight = setpoint.elevatorHeight;
            targetIntakeAngle = setpoint.intakeAngle;
            targetClimberAngle = setpoint.climberAngle;
        }

        private void SetSubsystemSetpoints()
        {
            armJoint.SetTargetAngle(targetArmAngle).withAxis(JointAxis.X).useAutomaticStartingOffset()
                .flipDirection().noWrap(noWrapAngle);

            elevator.SetTarget(targetElevatorHeight);

            intakeJoint.SetTargetAngle(targetIntakeAngle).withAxis(JointAxis.X).useAutomaticStartingOffset()
                .flipDirection();

            climberJoint.SetTargetAngle(targetClimberAngle).withAxis(JointAxis.Z).flipDirection().noWrap(300f);

            frontFlap.SetTargetAngle(GetFlapAngle()).withAxis(JointAxis.X);
        }

        private float GetFlapAngle()
        {
            float currentAngle = intakeJoint.GetSingleAxisAngle(JointAxis.X);

            if (currentAngle > 180)
            {
                currentAngle -= 360;
            }

            float newAngle = ((currentAngle) * (15)) / (60);

            return -Mathf.Clamp(newAngle, -15, 0);
        }
    }

    [Serializable]
    public struct OrbitSetpoint
    {
        [Tooltip("Deg")] public float armAngle;
        [Tooltip("Inch")] public float elevatorHeight;
        [Tooltip("Deg")] public float intakeAngle;
        [Tooltip("Deg")] public float climberAngle;
    }
}

