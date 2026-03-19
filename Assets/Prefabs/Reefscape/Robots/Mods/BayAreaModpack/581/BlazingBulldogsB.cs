using System;
using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.FieldScripts;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using Games.Reefscape.Scoring.Scorers;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.Drivetrain;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._581
{
public class BlazingBulldogsB: ReefscapeRobotBase
{
    [Header("Components")]
    [SerializeField] private GenericElevator elevator;
    [SerializeField] private GenericJoint arm;
    [SerializeField] private GenericJoint intakeJoint;
    [SerializeField] private GenericJoint climber;
    [SerializeField] private GenericRoller[] intakeRollers;
    [SerializeField] private GenericRoller[] endEffectorRollers;
    [SerializeField] private GenericRoller[] climbRollers;
    [SerializeField] private Transform algaeTarget;
    [SerializeField] private Transform algaeSlider;
    [SerializeField] private Transform intakeCoralTarget;
    [SerializeField] private Transform coralSlider;
    [SerializeField] private BoxCollider climbScorerCollider;
    [SerializeField] private BoxCollider climbCollider;
    [SerializeField] private ClimbScorer scorer;
    [SerializeField] private BoxCollider lollipopIntakeVision;
    private OverlapBoxBounds _cageDetector;

    [Header("PIDs")]
    [SerializeField] private PidConstants armPid;
    [SerializeField] private PidConstants armPidWithPiece;
    [SerializeField] private PidConstants intakePid;
    [SerializeField] private PidConstants climbPid;
    // [SerializeField] private PidConstants climbLatchPid;
    
    [Header("Intakes")]
    [SerializeField] private ReefscapeGamePieceIntake coralIntake;
    [SerializeField] private ReefscapeGamePieceIntake lollipopCoralIntake;
    [SerializeField] private ReefscapeGamePieceIntake algaeIntake;
    
    [Header("Game Piece Stow States")]
    [SerializeField] private GamePieceState coralStowState;
    [SerializeField] private GamePieceState intakeStowState;
    [SerializeField] private GamePieceState algaeStowState;

    [Header("Setpoints")]
    [SerializeField] private BlazingBulldogsBSetpoint stow;
    [SerializeField] private BlazingBulldogsBSetpoint transfer;
    [SerializeField] private BlazingBulldogsBSetpoint intake;
    [SerializeField] private BlazingBulldogsBSetpoint l1Front;
    [SerializeField] private BlazingBulldogsBSetpoint l2Front;
    [SerializeField] private BlazingBulldogsBSetpoint l3Front;
    [SerializeField] private BlazingBulldogsBSetpoint l4Front;
    [SerializeField] private BlazingBulldogsBSetpoint l2FrontPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l3FrontPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l4FrontPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l2FrontRelease;
    [SerializeField] private BlazingBulldogsBSetpoint l3FrontRelease;
    [SerializeField] private BlazingBulldogsBSetpoint l4FrontRelease;
    [SerializeField] private BlazingBulldogsBSetpoint lowAlgaeFront;
    [SerializeField] private BlazingBulldogsBSetpoint highAlgaeFront;
    [SerializeField] private BlazingBulldogsBSetpoint l1Back;
    [SerializeField] private BlazingBulldogsBSetpoint l2Back;
    [SerializeField] private BlazingBulldogsBSetpoint l3Back;
    [SerializeField] private BlazingBulldogsBSetpoint l4Back;
    [SerializeField] private BlazingBulldogsBSetpoint l2BackPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l3BackPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l4BackPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l2BackRelease;
    [SerializeField] private BlazingBulldogsBSetpoint l3BackRelease;
    [SerializeField] private BlazingBulldogsBSetpoint l4BackRelease;
    [SerializeField] private BlazingBulldogsBSetpoint lowAlgaeBack;
    [SerializeField] private BlazingBulldogsBSetpoint highAlgaeBack;
    [SerializeField] private BlazingBulldogsBSetpoint lollipopAlgae;
    [SerializeField] private BlazingBulldogsBSetpoint groundAlgae;
    [SerializeField] private BlazingBulldogsBSetpoint bargeFront;
    [SerializeField] private BlazingBulldogsBSetpoint bargeBack;
    [SerializeField] private BlazingBulldogsBSetpoint processor;
    [SerializeField] private BlazingBulldogsBSetpoint climbPrep;
    [SerializeField] private BlazingBulldogsBSetpoint climbed;
    [SerializeField] private BlazingBulldogsBSetpoint lollipopCoral;
    
    [Header("Score Settings")]
    [SerializeField] private BlazingBulldogsBScoreSettings l4ScoreSettings;
    [SerializeField] private BlazingBulldogsBScoreSettings l3ScoreSettings;
    [SerializeField] private BlazingBulldogsBScoreSettings l2ScoreSettings;
    [SerializeField] private BlazingBulldogsBScoreSettings l1ScoreSettings;
    
    [Header("End Effector Roller Audio")]
    [SerializeField] private AudioSource endEffectorRollerSource;
    [SerializeField] private AudioClip endEffectorRollerClip;
    
    [Header("Intake Roller Audio")]
    [SerializeField] private AudioSource intakeRollerSource;
    [SerializeField] private AudioClip intakeRollerClip;
    
    [Header("Climb Roller Audio")]
    [SerializeField] private AudioSource climbRollerSource;
    [SerializeField] private AudioClip climbRollerClip;
    
    [Header("Climb Click Audio")]
    [SerializeField] private AudioSource climbClickSource;
    [SerializeField] private AudioClip climbClickClip;
    
    [Header("Auto Align")]
    [SerializeField] private float zOffset;
    [SerializeField] private float xOffset;
    private ReefscapeAutoAlign _align;

    [Header("Miscellaneous")]
    [SerializeField] private float reefAvoidanceDistance;
    private float _elevatorTargetHeight;
    private float _armTargetAngle;
    private float _intakeTargetAngle;
    private float _climberTargetAngle;
    private bool _handoff;
    private bool _justPlaced;
    private Vector3 _blueReef;
    private Vector3 _redReef;
    private Collider[] _colliders;
    private OverlapBoxBounds _visionDetect;
    private LayerMask _mask;
    private BlazingBulldogsBSetpoint _currentSetpoint;
    private bool _isPlacingCoral;
    // private PlayerInput _playerInput;
    
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
    
    protected override void Start()
    {
        base.Start();
        
        arm.SetPid(armPid);
        climber.SetPid(climbPid);
        intakeJoint.SetPid(intakePid);
        
        _elevatorTargetHeight = 0;
        _armTargetAngle = 0;
        _climberTargetAngle = 0;
        _intakeTargetAngle = 0;
        _handoff = true;
        _justPlaced = false;
        _cageDetector = new OverlapBoxBounds(climbScorerCollider);
        _blueReef = GameObject.Find("BlueReef").transform.position;
        _redReef = GameObject.Find("RedReef").transform.position;
        _colliders = new Collider[6];
        _visionDetect = new OverlapBoxBounds(lollipopIntakeVision);
        _mask = LayerMask.GetMask("Coral");
        _currentSetpoint = stow;
        _align = GetComponent<ReefscapeAutoAlign>();
        _isPlacingCoral = false;
        // _playerInput = GetComponent<PlayerInput>();
        
        RobotGamePieceController.SetPreload(coralStowState);
        _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
        _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());
        
        // endEffectorRollerSource.clip = endEffectorRollerClip;
        // endEffectorRollerSource.loop = true;
        // endEffectorRollerSource.Stop();
        //
        // intakeRollerSource.clip = endEffectorRollerClip;
        // intakeRollerSource.loop = true;
        // intakeRollerSource.Stop();
        //
        // climbRollerSource.clip = climbRollerClip;
        // climbRollerSource.loop = true;
        // climbRollerSource.Stop();
        //
        // climbClickSource.clip = climbClickClip;
        // climbClickSource.loop = false;
        // climbClickSource.Stop();

        _coralController.gamePieceStates = new[]
        {
            coralStowState,
            intakeStowState
        };
        _coralController.intakes.Add(coralIntake);
        _coralController.intakes.Add(lollipopCoralIntake);

        _algaeController.gamePieceStates = new[] { algaeStowState };
        _algaeController.intakes.Add(algaeIntake);

        //
        // scoreSource.clip = scoreClip;
        // scoreSource.loop = false;
        // scoreSource.Stop();

        // _coralController.SetTargetState(coralStowState);
    }

    private void LateUpdate()
    {
        arm.UpdatePid((_algaeController.HasPiece() || CoralAtStow(coralStowState)) ? armPidWithPiece : armPid);
        climber.UpdatePid(climbPid);
        intakeJoint.UpdatePid(intakePid);
    }

    private void SetSetpoint(BlazingBulldogsBSetpoint setpoint)
    {
        _currentSetpoint = setpoint;
        
        _elevatorTargetHeight = setpoint.elevatorHeight;
        _armTargetAngle = setpoint.armAngle;
        _intakeTargetAngle = setpoint.intakeAngle;
        _climberTargetAngle = setpoint.climbAngle;
    }

    private void UpdateSetpoints()
    {
        float elevatorMinHeight = 30 * Mathf.Cos((arm.GetSingleAxisAngle(JointAxis.X) - 180) * Mathf.Deg2Rad);
        if (arm.GetSingleAxisAngle(JointAxis.X) < 100 || arm.GetSingleAxisAngle(JointAxis.X) > 260)
        {
            elevatorMinHeight = 0;
        }
        elevator.SetTarget(Mathf.Max(_elevatorTargetHeight, elevatorMinHeight));
        intakeJoint.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.Z);
        climber.SetTargetAngle(_climberTargetAngle).withAxis(JointAxis.X);

        float armNoWarpAngle;
        if (DistanceToReef(GetClosestReef()) < reefAvoidanceDistance)
        {
            armNoWarpAngle = IsFacingReef(GetClosestReef()) ? 135 : 225;
        }
        else
        {
            armNoWarpAngle = -1;
        }

        // if (elevator.GetElevatorHeight() < 40)
        // {
        //     if (DistanceToReef(GetClosestReef()) < reefAvoidanceDistance && IsFacingReef(GetClosestReef()) && arm.GetSingleAxisAngle(JointAxis.X) > 145 && arm.GetSingleAxisAngle(JointAxis.X) < 225)
        //     {
        //         armTarget = 180;
        //     }
        //     else
        //     {
        //         armNoWarpAngle = 225;
        //     }
        // }

        if (armNoWarpAngle >= 0)
        {
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(armNoWarpAngle);
        }
        else
        {
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X);
        }
        // arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(armNoWarpAngle);
    }
    
    private IEnumerator PlacePiece(bool hasCoral, bool hasAlgae)
    {
        // _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0));
        if (_coralController.currentStateNum == coralStowState.stateNum)
        {
            _isPlacingCoral = true;
            switch (GetLevelByState())
            {
                case 4:
                    // yield return new WaitForSeconds(l4ScoreSettings.scoreDelay);
                    yield return new WaitUntil(() => AtSetpoint(IsFacingReef(GetClosestReef()) ? l4FrontRelease :  l4BackRelease));
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, l4ScoreSettings.yForce, IsFacingReef(GetClosestReef()) ? l4ScoreSettings.zForce : -l4ScoreSettings.zForce));
                    break;
                case 3:
                    // yield return new WaitForSeconds(l3ScoreSettings.scoreDelay);
                    yield return new WaitUntil(() => AtSetpoint(IsFacingReef(GetClosestReef()) ? l3FrontRelease :  l3BackRelease));
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, l3ScoreSettings.yForce, IsFacingReef(GetClosestReef()) ? l3ScoreSettings.zForce : -l3ScoreSettings.zForce));
                    break;
                case 2:
                    // yield return new WaitForSeconds(l2ScoreSettings.scoreDelay);
                    yield return new WaitUntil(() => AtSetpoint(IsFacingReef(GetClosestReef()) ? l2FrontRelease :  l2BackRelease));
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, l2ScoreSettings.yForce, IsFacingReef(GetClosestReef()) ? l2ScoreSettings.zForce : -l2ScoreSettings.zForce));
                    break;
                case 1:
                    yield return new WaitForSeconds(l1ScoreSettings.scoreDelay);
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, l1ScoreSettings.yForce, IsFacingReef(GetClosestReef()) ? l1ScoreSettings.zForce : -l1ScoreSettings.zForce));
                    break;
                default:
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 1f, 0));
                    break;
            }
            _isPlacingCoral = false;
        }
        else
        {
            _coralController.ReleaseGamePieceWithForce(new Vector3(0, 1f, 0));
        }
        
        
        _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 1.5f, 0));
        _handoff = false;
        _justPlaced = true;
    }

    private void UpdateRollers(bool hasCoral, bool hasAlgae)
    {
        if (IntakeAction.IsPressed() && !hasCoral && CurrentRobotMode == ReefscapeRobotMode.Coral)
        {
            foreach (var roller in intakeRollers)
            {
                roller.ChangeAngularVelocity(1000f);
            }
        }
        
        if (CurrentSetpoint == ReefscapeSetpoints.Climb)
        {
            foreach (var roller in climbRollers)
            {
                roller.ChangeAngularVelocity(1500f);
            }
        }
    }

    private void UpdateAudio()
    {
        // // Score Sound
        // if (CurrentSetpoint == ReefscapeSetpoints.Place && LastSetpoint != ReefscapeSetpoints.L1 && !scoreSource.isPlaying && CurrentRobotMode == ReefscapeRobotMode.Coral && !_playedScoreSound)
        // {
        //     scoreSource.Play();
        //     _playedScoreSound = true;
        // }
        
        // EE Rollers
        float endEffectorRollerSpeed = Mathf.Max(new float[]
        {
            Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
            Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
            Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
        });
        if (endEffectorRollerSpeed > 5 && !endEffectorRollerSource.isPlaying)
        {
            endEffectorRollerSource.Play();
        }
        else if (endEffectorRollerSpeed <= 5 && endEffectorRollerSource.isPlaying)
        {
            endEffectorRollerSource.Stop();
        }
        
        // Intake Rollers
        float intakeRollerSpeed = Mathf.Max(new float[]
        {
            Mathf.Abs(intakeRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
            Mathf.Abs(intakeRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
            Mathf.Abs(intakeRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
        });
        if (intakeRollerSpeed > 5 && !intakeRollerSource.isPlaying)
        {
            intakeRollerSource.Play();
        }
        else if (intakeRollerSpeed <= 5 && intakeRollerSource.isPlaying)
        {
            intakeRollerSource.Stop();
        }
        
        // Climb Rollers
        float climbRollerSpeed = Mathf.Max(new float[]
        {
            Mathf.Abs(climbRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
            Mathf.Abs(climbRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
            Mathf.Abs(climbRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
        });
        if (climbRollerSpeed > 5 && !climbRollerSource.isPlaying)
        {
            climbRollerSource.Play();
        }
        else if (climbRollerSpeed <= 5 && climbRollerSource.isPlaying)
        {
            climbRollerSource.Stop();
        }
    }
    
    private bool AtSetpoint(BlazingBulldogsBSetpoint stp)
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), stp.elevatorHeight, 2f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp.armAngle, 2f) &&
            Utils.InAngularRange(intakeJoint.GetSingleAxisAngle(JointAxis.Z), stp.intakeAngle, 2f);
    }
    
    private bool AtSetpoint()
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), _elevatorTargetHeight, 7f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _armTargetAngle, 20f) &&
            Utils.InAngularRange(intakeJoint.GetSingleAxisAngle(JointAxis.Z), _intakeTargetAngle, 20f);
    }

    private bool CoralAtStow(GamePieceState stowState)
    {
        return _coralController.atTarget && _coralController.currentStateNum == stowState.stateNum;
    }

    private bool FacingBarge()
    {
        return (transform.position.x > 0 && transform.rotation.eulerAngles.y > 180) || (transform.position.x <= 0 && transform.rotation.eulerAngles.y <= 180);
    }
    
    private float DistanceToReef(Vector3 reefPos)
    {
        return Mathf.Sqrt(Mathf.Pow(transform.position.x - reefPos.x, 2) + Mathf.Pow(transform.position.z - reefPos.z, 2));
    }
    
    private Vector3 GetClosestReef()
    {
        return DistanceToReef(_blueReef) < DistanceToReef(_redReef) ? _blueReef : _redReef;
    }

    private bool IsFacingReef(Vector3 reefPos)
    {
        var toReefVector = (reefPos - transform.position).normalized;
        var robotForwardVector = transform.forward.normalized;
        var angle = Vector3.Dot(robotForwardVector, toReefVector);
        return angle > 0.0f;
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
    
    private void AlgaeSlider()
    {
        if (algaeIntake.GamePiece != null)
        {
            var localSliderSpaceZ = algaeTarget.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).z;
            algaeSlider.localPosition = new Vector3(0, 0, localSliderSpaceZ);
        }
    }
    
    private void CoralSlider()
    {
        if (coralIntake.GamePiece != null)
        {
            var localSliderSpaceZ = intakeCoralTarget.transform.InverseTransformPoint(coralIntake.GamePiece.transform.position).z;
            coralSlider.localPosition = new Vector3(0, 0, Mathf.Clamp(localSliderSpaceZ, -0.0875f, 0.057f));
        }
    }
    
    private void UpdateAutoAlign()
    {
        _align.offset = new Vector3(IsFacingReef(GetClosestReef()) ? xOffset : -xOffset, 0, zOffset);
    }
    
    private void RunIntakeVision()
        {
            if ((CurrentSetpoint != ReefscapeSetpoints.RobotSpecial && CurrentSetpoint != ReefscapeSetpoints.Stack) || CurrentRobotMode == ReefscapeRobotMode.Algae || _coralController.HasPiece())
            {
                return;
            }
            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i] = null;
            }
            var size = _visionDetect.OverlapBoxNonAlloc(ref _colliders, _mask);
            
            if (_colliders != null)
            {
                if (!_colliders[0]) return;
                GameObject close = _colliders[0].gameObject;
                for (int i = 1; i < size; i++) {
                    if (Vector3.Distance(_colliders[i].transform.position, transform.position) <
                        Vector3.Distance(close.transform.position, transform.position))
                    {
                        close = _colliders[i].gameObject;
                    }
                }
                
                Transform offsetTransform = new GameObject().transform;
                offsetTransform.position = lollipopCoralIntake.transform.position;
                offsetTransform.rotation = Quaternion.Euler(lollipopCoralIntake.transform.rotation.eulerAngles.x, lollipopCoralIntake.transform.rotation.eulerAngles.y, lollipopCoralIntake.transform.rotation.eulerAngles.z);
                var angle = Quaternion.LookRotation(offsetTransform.position - close.transform.position, offsetTransform.up).eulerAngles.y;
                // DriveController.overideInput(new Vector2(0.6f*TranslateAction.ReadValue<Vector2>().y, 0f), Mathf.Clamp(-angle + offsetTransform.eulerAngles.y, -0.18f, 0.18f), DriveController.DriveMode.RobotRelative);
                // DriveController.overideInput(new Vector2(0.6f*TranslateAction.ReadValue<Vector2>().y, 0f), 0, DriveController.DriveMode.RobotRelative);
                float turnValue = Mathf.Clamp(-angle + offsetTransform.eulerAngles.y, 0.18f, -0.18f);
                Vector2 translateInput = TranslateAction.ReadValue<Vector2>();
                float translateAngle = Mathf.Atan2(translateInput.y, translateInput.x) * Mathf.Rad2Deg;
                float heading = transform.rotation.eulerAngles.y - 90f;
                DriveController.overideInput(new Vector2(0.6f*translateInput.magnitude*Mathf.Sin(Mathf.Deg2Rad * (translateAngle+heading)), 0f), 0, DriveController.DriveMode.RobotRelative);
                // if (Utils.InRange(turnValue, 0f, .01f)) turnValue = 0;
                DriveController.SoftSteer(Mathf.Clamp((-angle + offsetTransform.eulerAngles.y)/100, 0.18f, -0.18f));
                Debug.Log(Mathf.Clamp((0.1f*(-angle + offsetTransform.eulerAngles.y)), 0.12f, -0.12f));
            }
        }

    private void FixedUpdate()
    {
        bool hasAlgae = _algaeController.HasPiece();
        bool hasCoral = _coralController.HasPiece();
        
        AlgaeSlider();
        CoralSlider();
        
        Debug.Log(CurrentSetpoint + ", " + LastSetpoint);
        
        // climbCollider.enabled = _cageDetector.OverlapBox().Length > 7;
        climbCollider.enabled = scorer.AutoClimbTriggered;
        
        if (_isPlacingCoral)
        {
            DriveController.overideInput(new Vector2(0, 0), 0, DriveController.DriveMode.FieldOriented);
        }
        
        _algaeController.SetTargetState(algaeStowState);
        // if (_coralController.currentStateNum == coralStowState.stateNum)
        // {
        //     _coralController.SetTargetState(coralStowState);
        // }
        // else
        // {
        //     _coralController.SetTargetState(intakeStowState);
        // }

        if (_handoff || (CoralAtStow(intakeStowState) && AtSetpoint(transfer)) || (AtSetpoint(lollipopCoral) && !CoralAtStow(intakeStowState)))
        {
            _coralController.SetTargetState(coralStowState);
            _handoff = true;
        }
        else
        {
            _coralController.SetTargetState(intakeStowState);
        }

        if (CurrentRobotMode == ReefscapeRobotMode.Coral && IsIntaking)
        {
            _justPlaced = false;
        }

        if (CurrentSetpoint == ReefscapeSetpoints.Place && hasCoral)
        {
            _justPlaced = true;
        }

        if (_justPlaced)
        {
            BlazingBulldogsBSetpoint placeSetpoint = _currentSetpoint;
            if (LastSetpoint == ReefscapeSetpoints.L4)
            {
                placeSetpoint = IsFacingReef(GetClosestReef()) ? l4FrontPlace : l4BackPlace;
            }
            else if (LastSetpoint == ReefscapeSetpoints.L3)
            {
                placeSetpoint = IsFacingReef(GetClosestReef()) ? l3FrontPlace : l3BackPlace;
            }
            else if (LastSetpoint == ReefscapeSetpoints.L2)
            {
                placeSetpoint = IsFacingReef(GetClosestReef()) ? l2FrontPlace : l2BackPlace;
            }
            SetSetpoint(placeSetpoint);
        }
        
        // _coralController.SetTargetState(_coralController.currentStateNum switch
        // {
        //     1 => coralStowState,
        //     2 => intakeStowState,
        //     _ => coralStowState
        // });
        
        // if (!IntakeAction.IsPressed())
        // {
        //     _algaeController.RequestIntake(algaeIntake, false);
        //     _coralController.RequestIntake(coralIntake, false);
        // }

        switch (CurrentSetpoint)
        {
            case ReefscapeSetpoints.Stow:
                if (L1Action.IsPressed() && LastSetpoint == ReefscapeSetpoints.Stow)
                {
                    SetState(ReefscapeSetpoints.Stack);
                }
                if (hasAlgae || CoralAtStow(coralStowState))
                {
                    SetSetpoint(stow);
                }
                else
                {
                    SetSetpoint(transfer);
                }
                break;
            case ReefscapeSetpoints.Intake:
                if (CurrentRobotMode == ReefscapeRobotMode.Coral || hasAlgae)
                {
                    SetSetpoint(intake);
                }
                else
                {
                    SetSetpoint(groundAlgae);
                }
                
                _algaeController.RequestIntake(algaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae && IntakeAction.IsPressed());
                _coralController.RequestIntake(coralIntake, CurrentRobotMode == ReefscapeRobotMode.Coral && !hasCoral && IntakeAction.IsPressed());
                break;
            case ReefscapeSetpoints.Place:
                StartCoroutine(PlacePiece(hasCoral, hasAlgae));
                
                // Switch statement can't be used because the setpoints are technically not constants
                // BlazingBulldogsBSetpoint placeSetpoint = _currentSetpoint;
                // if (_currentSetpoint == l2Front)
                // {
                //     placeSetpoint = l2FrontPlace;
                // }
                // else if (_currentSetpoint == l2Back)
                // {
                //     placeSetpoint = l2BackPlace;
                // }
                // else if (_currentSetpoint == l3Front)
                // {
                //     placeSetpoint = l3FrontPlace;
                // }
                // else if (_currentSetpoint == l3Back)
                // {
                //     placeSetpoint = l3BackPlace;
                // }
                // else if (_currentSetpoint == l4Front)
                // {
                //     placeSetpoint = l4FrontPlace;
                // }
                // else if (_currentSetpoint == l4Back)
                // {
                //     placeSetpoint = l4BackPlace;
                // }
                // SetSetpoint(placeSetpoint);

                break;
            case ReefscapeSetpoints.L1:
                SetSetpoint(IsFacingReef(GetClosestReef()) ? l1Front : l1Back);
                break;
            case ReefscapeSetpoints.Stack:
                if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    SetSetpoint(lollipopCoral);
                }
                else
                {
                    SetSetpoint(lollipopAlgae);
                }
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(lollipopCoralIntake, IntakeAction.IsPressed() && !hasCoral);
                break;
            case ReefscapeSetpoints.L2:
                SetSetpoint(IsFacingReef(GetClosestReef()) ? l2Front : l2Back);
                break;
            case ReefscapeSetpoints.LowAlgae:
                SetSetpoint(IsFacingReef(GetClosestReef()) ? lowAlgaeFront : lowAlgaeBack);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L3:
                SetSetpoint(IsFacingReef(GetClosestReef()) ? l3Front : l3Back);
                break;
            case ReefscapeSetpoints.HighAlgae:
                SetSetpoint(IsFacingReef(GetClosestReef()) ? highAlgaeFront : highAlgaeBack);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L4:
                // if (_justPlaced)
                // {
                //     SetSetpoint(IsFacingReef(GetClosestReef()) ? l4FrontPlace : l4BackPlace);
                // }
                // else
                // {
                //     SetSetpoint(IsFacingReef(GetClosestReef()) ? l4Front : l4Back);
                // }
                SetSetpoint(IsFacingReef(GetClosestReef()) ? l4Front : l4Back);
                break;
            case ReefscapeSetpoints.Processor:
                SetSetpoint(processor);
                break;
            case ReefscapeSetpoints.Barge:
                SetSetpoint(FacingBarge() ? bargeFront : bargeBack);
                break;
            case ReefscapeSetpoints.RobotSpecial:
                SetSetpoint(lollipopCoral);
                
                _algaeController.RequestIntake(algaeIntake, !hasCoral && !hasAlgae && AtSetpoint(lollipopCoral));
                _coralController.RequestIntake(lollipopCoralIntake, !hasCoral && !hasAlgae && AtSetpoint(lollipopCoral));
                break;
            case ReefscapeSetpoints.Climb:
                SetSetpoint(climbPrep);
                if (scorer.AutoClimbTriggered)
                {
                    SetState(ReefscapeSetpoints.Climbed);
                    climbClickSource.Play();
                }
                break;
            case ReefscapeSetpoints.Climbed:
                SetSetpoint(climbed);
                break;
            default:
                break;
        }

        if (_coralController.currentStateNum == coralStowState.stateNum && !_coralController.atTarget)
        {
            SetSetpoint(transfer);
        }
        
        UpdateSetpoints();
        UpdateAudio();
        UpdateRollers(hasCoral, hasAlgae);
        UpdateAutoAlign();
        RunIntakeVision();
    }
}
[Serializable]
public struct BlazingBulldogsBScoreSettings
{
    public float yForce;
    public float zForce;
    public float scoreDelay;
}
}