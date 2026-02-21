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
    // [SerializeField] private BoxCollider intakeVision;
    private OverlapBoxBounds _cageDetector;

    [Header("PIDs")]
    [SerializeField] private PidConstants armPid;
    [SerializeField] private PidConstants intakePid;
    [SerializeField] private PidConstants climbPid;
    // [SerializeField] private PidConstants climbLatchPid;
    
    [Header("Intakes")]
    [SerializeField] private ReefscapeGamePieceIntake coralIntake;
    [SerializeField] private ReefscapeGamePieceIntake algaeIntake;
    
    [Header("Game Piece Stow States")]
    [SerializeField] private GamePieceState coralStowState;
    [SerializeField] private GamePieceState intakeStowState;
    [SerializeField] private GamePieceState algaeStowState;

    [Header("Setpoints")]
    [SerializeField] private BlazingBulldogsBSetpoint stow;
    [SerializeField] private BlazingBulldogsBSetpoint intake;
    [SerializeField] private BlazingBulldogsBSetpoint l1Front;
    [SerializeField] private BlazingBulldogsBSetpoint l2Front;
    [SerializeField] private BlazingBulldogsBSetpoint l3Front;
    [SerializeField] private BlazingBulldogsBSetpoint l4Front;
    [SerializeField] private BlazingBulldogsBSetpoint l2FrontPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l3FrontPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l4FrontPlace;
    [SerializeField] private BlazingBulldogsBSetpoint lowAlgaeFront;
    [SerializeField] private BlazingBulldogsBSetpoint highAlgaeFront;
    [SerializeField] private BlazingBulldogsBSetpoint l1Back;
    [SerializeField] private BlazingBulldogsBSetpoint l2Back;
    [SerializeField] private BlazingBulldogsBSetpoint l3Back;
    [SerializeField] private BlazingBulldogsBSetpoint l4Back;
    [SerializeField] private BlazingBulldogsBSetpoint l2BackPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l3BackPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l4BackPlace;
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
    [SerializeField] private float l4ScoreDelay;
    [SerializeField] private float l3ScoreDelay;
    [SerializeField] private float l2ScoreDelay;
    private float _elevatorTargetHeight;
    private float _armTargetAngle;
    private float _intakeTargetAngle;
    private float _climberTargetAngle;
    // private Collider[] _colliders;
    // private OverlapBoxBounds _visionDetect;
    // private LayerMask _mask;
    // private BlazingBulldogsBSetpoint _currentSetpoint;
    
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
        // _cageDetector = new OverlapBoxBounds(climbScorerCollider);
        // _colliders = new Collider[6];
        // _visionDetect = new OverlapBoxBounds(intakeVision);
        // _mask = LayerMask.GetMask("Coral");
        // _currentSetpoint = stow;
        // _align = GetComponent<ReefscapeAutoAlign>();
        
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
        arm.UpdatePid(armPid);
        climber.UpdatePid(climbPid);
        intakeJoint.UpdatePid(intakePid);
    }

    private void SetSetpoint(BlazingBulldogsBSetpoint setpoint)
    {
        // _currentSetpoint = setpoint;
        
        _elevatorTargetHeight = setpoint.elevatorHeight;
        _armTargetAngle = setpoint.armAngle;
        _intakeTargetAngle = setpoint.intakeAngle;
        _climberTargetAngle = setpoint.climbAngle;
    }

    private void UpdateSetpoints()
    {
        elevator.SetTarget(Mathf.Max(_elevatorTargetHeight, 0));
        arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X);
        intakeJoint.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.Z);
        climber.SetTargetAngle(_climberTargetAngle).withAxis(JointAxis.X);
    }
    
    private IEnumerator PlacePiece(bool hasCoral, bool hasAlgae)
    {
        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0));
        _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0));
        yield return null;
    }

    // private void UpdateRollers(bool hasCoral, bool hasAlgae)
    // {
    //     // if (IntakeAction.IsPressed() && !hasCoral && !hasAlgae)
    //     // {
    //     //     foreach (var roller in endEffectorRollers)
    //     //     {
    //     //         roller.ChangeAngularVelocity(1000f);
    //     //     }
    //     // }
    //     //
    //     // if (CurrentSetpoint == ReefscapeSetpoints.Climb)
    //     // {
    //     //     foreach (var roller in climbRollers)
    //     //     {
    //     //         roller.ChangeAngularVelocity(1000f);
    //     //     }
    //     // }
    // }

    // private void UpdateAudio()
    // {
    //     // // Score Sound
    //     // if (CurrentSetpoint == ReefscapeSetpoints.Place && LastSetpoint != ReefscapeSetpoints.L1 && !scoreSource.isPlaying && CurrentRobotMode == ReefscapeRobotMode.Coral && !_playedScoreSound)
    //     // {
    //     //     scoreSource.Play();
    //     //     _playedScoreSound = true;
    //     // }
    //     
    //     // EE Rollers
    //     float endEffectorRollerSpeed = Mathf.Max(new float[]
    //     {
    //         Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
    //         Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
    //         Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
    //     });
    //     if (endEffectorRollerSpeed > 5 && !endEffectorRollerSource.isPlaying)
    //     {
    //         endEffectorRollerSource.Play();
    //     }
    //     else if (endEffectorRollerSpeed <= 5 && endEffectorRollerSource.isPlaying)
    //     {
    //         endEffectorRollerSource.Stop();
    //     }
    //     
    //     // Intake Rollers
    //     float intakeRollerSpeed = Mathf.Max(new float[]
    //     {
    //         Mathf.Abs(intakeRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
    //         Mathf.Abs(intakeRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
    //         Mathf.Abs(intakeRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
    //     });
    //     if (intakeRollerSpeed > 5 && !intakeRollerSource.isPlaying)
    //     {
    //         intakeRollerSource.Play();
    //     }
    //     else if (intakeRollerSpeed <= 5 && intakeRollerSource.isPlaying)
    //     {
    //         intakeRollerSource.Stop();
    //     }
    //     
    //     // Climb Rollers
    //     float climbRollerSpeed = Mathf.Max(new float[]
    //     {
    //         Mathf.Abs(climbRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
    //         Mathf.Abs(climbRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
    //         Mathf.Abs(climbRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
    //     });
    //     if (climbRollerSpeed > 5 && !climbRollerSource.isPlaying)
    //     {
    //         climbRollerSource.Play();
    //     }
    //     else if (climbRollerSpeed <= 5 && climbRollerSource.isPlaying)
    //     {
    //         climbRollerSource.Stop();
    //     }
    // }
    
    // private bool AtSetpoint(BlazingBulldogsBSetpoint stp)
    // {
    //     return
    //         Utils.InRange(elevator.GetElevatorHeight(), stp.elevatorHeight, 2f) &&
    //         Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp.armAngle, 2f) &&
    //         Utils.InAngularRange(intakeJoint.GetSingleAxisAngle(JointAxis.Z), stp.intakeAngle, 2f);
    // }
    //
    // private bool AtSetpoint()
    // {
    //     return
    //         Utils.InRange(elevator.GetElevatorHeight(), _elevatorTargetHeight, 7f) &&
    //         Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _armTargetAngle, 20f) &&
    //         Utils.InAngularRange(intakeJoint.GetSingleAxisAngle(JointAxis.Z), _intakeTargetAngle, 20f);
    // }
    
    // private void AlgaeSlider()
    // {
    //     if (algaeIntake.GamePiece != null)
    //     {
    //         var localSliderSpaceZ = algaeTarget.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).z;
    //         algaeSlider.localPosition = new Vector3(0, 0, localSliderSpaceZ);
    //     }
    // }
    //
    // private void CoralSlider()
    // {
    //     if (coralIntake.GamePiece != null)
    //     {
    //         var localSliderSpaceZ = intakeCoralTarget.transform.InverseTransformPoint(coralIntake.GamePiece.transform.position).z;
    //         coralSlider.localPosition = new Vector3(0, 0, localSliderSpaceZ);
    //     }
    // }
    //
    // private void UpdateAutoAlign()
    // {
    //     _align.offset = new Vector3(FacingReef ? xOffset : -xOffset, 0, zOffset);
    // }
    
    // private void RunIntakeVision()
    //     {
    //         if (!IntakeAction.IsPressed() || _coralController.HasPiece()) return;
    //         for (int i = 0; i < _colliders.Length; i++)
    //         {
    //             _colliders[i] = null;
    //         }
    //         var size = _visionDetect.OverlapBoxNonAlloc(ref _colliders, _mask);
    //         
    //         if (_colliders != null)
    //         {
    //             if (!_colliders[0]) return;
    //             GameObject close = _colliders[0].gameObject;
    //             for (int i = 1; i < size; i++) {
    //                 if (Vector3.Distance(_colliders[i].transform.position, transform.position) <
    //                     Vector3.Distance(close.transform.position, transform.position))
    //                 {
    //                     close = _colliders[i].gameObject;
    //                 }
    //             }
    //             
    //             Transform offsetTransform = new GameObject().transform;
    //             offsetTransform.position = transform.position;
    //             offsetTransform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y+180, transform.rotation.eulerAngles.z);
    //             var angle = Quaternion.LookRotation(offsetTransform.position - close.transform.position, offsetTransform.up).eulerAngles.y;
    //             // DriveController.overideInput(new Vector2(0.5f, 0f), Mathf.Clamp(-angle + offsetTransform.eulerAngles.y, 0.18f, -0.18f), DriveController.DriveMode.RobotRelative);
    //             // DriveController.SoftSteer(Mathf.Clamp(-angle + offsetTransform.eulerAngles.y, 0.4f, -0.4f));
    //             DriveController.SoftSteer(Mathf.Clamp(-angle + offsetTransform.eulerAngles.y, 0.18f, -0.18f));
    //         }
    //     }

    private void FixedUpdate()
    {
        bool hasAlgae = _algaeController.HasPiece();
        bool hasCoral = _coralController.HasPiece();
        
        Debug.Log(hasCoral);
        
        // AlgaeSlider();
        // CoralSlider();
        //
        _algaeController.SetTargetState(algaeStowState);
        _coralController.SetTargetState(coralStowState);
        // _coralController.SetTargetState(_coralController.currentStateNum switch
        // {
        //     0 => coralStowState,
        //     1 => intakeStowState,
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
                SetSetpoint(stow);
                break;
            case ReefscapeSetpoints.Intake:
                // if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                // {
                //     SetSetpoint(intake);
                // }
                // else
                // {
                //     SetSetpoint(groundAlgae);
                // }
                SetSetpoint(intake);
                
                _algaeController.RequestIntake(algaeIntake, !hasCoral && !hasAlgae && IntakeAction.IsPressed());
                _coralController.RequestIntake(coralIntake, !hasAlgae && !hasCoral && IntakeAction.IsPressed());
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
                SetSetpoint(FacingReef ? l1Front : l1Back);
                break;
            case ReefscapeSetpoints.Stack:
                SetSetpoint(lollipopAlgae);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L2:
                SetSetpoint(FacingReef ? l2Front : l2Back);
                break;
            case ReefscapeSetpoints.LowAlgae:
                SetSetpoint(FacingReef ? lowAlgaeFront : lowAlgaeBack);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L3:
                SetSetpoint(FacingReef ? l3Front : l3Back);
                break;
            case ReefscapeSetpoints.HighAlgae:
                SetSetpoint(FacingReef ? highAlgaeFront : highAlgaeBack);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L4:
                SetSetpoint(FacingReef ? l4Front : l4Back);
                break;
            case ReefscapeSetpoints.Processor:
                SetSetpoint(processor);
                break;
            case ReefscapeSetpoints.Barge:
                SetSetpoint(FacingReef ? bargeFront : bargeBack);
                break;
            case ReefscapeSetpoints.RobotSpecial:
                SetState(ReefscapeSetpoints.Stow);
                break;
            case ReefscapeSetpoints.Climb:
                SetSetpoint(climbPrep);
                // if (scorer.AutoClimbTriggered)
                // {
                //     SetState(ReefscapeSetpoints.Climbed);
                //     climbClickSource.Play();
                // }
                break;
            case ReefscapeSetpoints.Climbed:
                SetSetpoint(climbed);
                break;
        }
        
        UpdateSetpoints();
        // UpdateAudio();
        // UpdateRollers(hasCoral, hasAlgae);
        // UpdateAutoAlign();
        // RunIntakeVision();
    }
}
}