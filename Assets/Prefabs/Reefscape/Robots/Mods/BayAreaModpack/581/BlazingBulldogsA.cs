using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using Games.Reefscape.Scoring.Scorers;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._581
{
public class BlazingBulldogsA: ReefscapeRobotBase
{
    [Header("Components")]
    [SerializeField] private GenericElevator elevator;
    [SerializeField] private GenericJoint arm;
    [SerializeField] private GenericJoint wrist;
    [SerializeField] private GenericJoint climber;
    [SerializeField] private GenericJoint leftLatch;
    [SerializeField] private GenericJoint rightLatch;
    [SerializeField] private GenericRoller[] endEffectorRollers;
    [SerializeField] private GenericRoller[] climbRollers;
    [SerializeField] private Transform algaeTarget;
    [SerializeField] private Transform algaeSlider;
    [SerializeField] private Transform coralTarget;
    [SerializeField] private Transform coralSlider;
    [SerializeField] private BoxCollider climbScorerCollider;
    [SerializeField] private BoxCollider climbCollider;
    private OverlapBoxBounds _cageDetector;

    [Header("PIDs")]
    [SerializeField] private PidConstants armPid;
    [SerializeField] private PidConstants wristPid;
    [SerializeField] private PidConstants climbPid;
    [SerializeField] private PidConstants climbLatchPid;
    
    [Header("Intakes")]
    [SerializeField] private ReefscapeGamePieceIntake coralIntake;
    [SerializeField] private ReefscapeGamePieceIntake algaeIntake;
    
    [Header("Game Piece Stow States")]
    [SerializeField] private GamePieceState coralStowState;
    [SerializeField] private GamePieceState algaeStowState;
    
    [Header("Setpoints")]
    [SerializeField] private BlazingBulldogsASetpoint stow;
    [SerializeField] private BlazingBulldogsASetpoint groundIntake;
    [SerializeField] private BlazingBulldogsASetpoint stationIntake;
    [SerializeField] private BlazingBulldogsASetpoint l1;
    [SerializeField] private BlazingBulldogsASetpoint l2;
    [SerializeField] private BlazingBulldogsASetpoint l3;
    [SerializeField] private BlazingBulldogsASetpoint l4;
    [SerializeField] private BlazingBulldogsASetpoint lowAlgae;
    [SerializeField] private BlazingBulldogsASetpoint highAlgae;
    [SerializeField] private BlazingBulldogsASetpoint lollipopAlgae;
    [SerializeField] private BlazingBulldogsASetpoint groundAlgae;
    [SerializeField] private BlazingBulldogsASetpoint barge;
    [SerializeField] private BlazingBulldogsASetpoint processor;
    [SerializeField] private BlazingBulldogsASetpoint climbPrep;
    [SerializeField] private BlazingBulldogsASetpoint climbed;
    
    // [Header("End Effector Roller Audio")]
    // [SerializeField] private AudioSource endEffectorRollerSource;
    // [SerializeField] private AudioClip endEffectorRollerClip;
    //
    // [Header("Score Audio")]
    // [SerializeField] private AudioSource scoreSource;
    // [SerializeField] private AudioClip scoreClip;

    [Header("Debug")]
    private float _elevatorTargetHeight;
    private float _armTargetAngle;
    private float _wristTargetAngle;
    private float _climberTargetAngle;
    private bool _playedScoreSound;
    private bool _placedVerticalCoral;
    
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
    
    protected override void Start()
    {
        base.Start();
        
        arm.SetPid(armPid);
        climber.SetPid(climbPid);
        wrist.SetPid(wristPid);
        rightLatch.SetPid(climbLatchPid);
        leftLatch.SetPid(climbLatchPid);
        
        _elevatorTargetHeight = 0;
        _armTargetAngle = 0;
        _climberTargetAngle = 0;
        _wristTargetAngle = 0;
        _playedScoreSound = false;
        _placedVerticalCoral = false;
        _cageDetector = new OverlapBoxBounds(climbScorerCollider);
        
        RobotGamePieceController.SetPreload(coralStowState);
        _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
        _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());
        

        _coralController.gamePieceStates = new[]
        {
            coralStowState
        };
        _coralController.intakes.Add(coralIntake);

        _algaeController.gamePieceStates = new[] { algaeStowState };
        _algaeController.intakes.Add(algaeIntake);
        
        // endEffectorRollerSource.clip = endEffectorRollerClip;
        // endEffectorRollerSource.loop = true;
        // endEffectorRollerSource.Stop();
        //
        // scoreSource.clip = scoreClip;
        // scoreSource.loop = false;
        // scoreSource.Stop();
    }

    private void LateUpdate()
    {
        arm.UpdatePid(armPid);
        climber.UpdatePid(climbPid);
        wrist.UpdatePid(wristPid);
        rightLatch.UpdatePid(climbLatchPid);
        leftLatch.UpdatePid(climbLatchPid);
    }

    private void SetSetpoint(BlazingBulldogsASetpoint setpoint)
    {
        _elevatorTargetHeight = setpoint.elevatorHeight;
        _armTargetAngle = setpoint.armAngle;
        _wristTargetAngle = setpoint.wristAngle;
        _climberTargetAngle = setpoint.climbAngle;

        if (_wristTargetAngle == 0 && coralSlider.localPosition.z > 0)
        {
            _wristTargetAngle = 180;
        }

        if (CurrentSetpoint == ReefscapeSetpoints.L2 || CurrentSetpoint == ReefscapeSetpoints.L3 ||  CurrentSetpoint == ReefscapeSetpoints.L4)
        {
            _elevatorTargetHeight -= Mathf.Abs(coralSlider.localPosition.z*25.4f);
        }
    }

    private void UpdateSetpoints()
    {
        elevator.SetTarget(_elevatorTargetHeight);
        arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X);
        wrist.SetTargetAngle(_wristTargetAngle).withAxis(JointAxis.Z);
        climber.SetTargetAngle(_climberTargetAngle).withAxis(JointAxis.X);
        leftLatch.SetTargetAngle(0).withAxis(JointAxis.X);
        rightLatch.SetTargetAngle(0).withAxis(JointAxis.X);
    }
    
    private IEnumerator PlacePiece(bool hasCoral, bool hasAlgae)
    {
        if (LastSetpoint == ReefscapeSetpoints.L2 || LastSetpoint == ReefscapeSetpoints.L3 ||  LastSetpoint == ReefscapeSetpoints.L4)
        {
            if (!_placedVerticalCoral)
            {
                _elevatorTargetHeight -= 8;
                _placedVerticalCoral = true;
            }
            yield return new WaitForSeconds(LastSetpoint switch {
                ReefscapeSetpoints.L2 => 0.02f,
                ReefscapeSetpoints.L3 => 0.05f,
                ReefscapeSetpoints.L4 => 0.1f,
                _ => 0f
                });
            _coralController.ReleaseGamePieceWithForce(new Vector3(0, LastSetpoint switch {
                ReefscapeSetpoints.L2 => 0.5f,
                ReefscapeSetpoints.L3 => 0.1f,
                ReefscapeSetpoints.L4 => 0f,
                _ => 0f
            }, 0));
            // yield return new WaitForSeconds(0.5f);
            // _placedVerticalCoral = false;
        }
        else
        {
            _coralController.ReleaseGamePieceWithForce(new Vector3(0, 2f, 0));
        }
        _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, 3f));
    }

    private void UpdateRollers(bool hasCoral, bool hasAlgae)
    {
        // if ((IntakeAction.IsPressed() && !_coralController.atTarget) || coralIntake.requestIntake)
        if (IntakeAction.IsPressed() && !hasCoral && !hasAlgae)
        {
            foreach (var roller in endEffectorRollers)
            {
                roller.ChangeAngularVelocity(1000f);
            }
        }

        if (CurrentSetpoint == ReefscapeSetpoints.Climb)
        {
            foreach (var roller in climbRollers)
            {
                roller.ChangeAngularVelocity(1000f);
            }
        }
        //
        // if (IntakeAction.IsPressed() && !_algaeController.atTarget && (CurrentRobotMode == ReefscapeRobotMode.Algae || CurrentSetpoint == ReefscapeSetpoints.HighAlgae || CurrentSetpoint == ReefscapeSetpoints.LowAlgae))
        // {
        //     foreach (var roller in endEffectorRollers)
        //     {
        //         roller.ChangeAngularVelocity(500f);
        //     }
        // }
        //
        // if (_coralController.currentStateNum == coralStowState.stateNum && !_coralController.atTarget)
        // {
        //     foreach (var roller in endEffectorRollers)
        //     {
        //         roller.ChangeAngularVelocity(-500f);
        //     }
        // }
        //
        // if (hasCoral && !_coralController.atTarget)
        // {
        //     foreach (var roller in indexerRollers)
        //     {
        //         roller.ChangeAngularVelocity(500f);
        //     }
        // }
    }

    private void UpdateAudio()
    {
        // // Score Sound
        // if (CurrentSetpoint == ReefscapeSetpoints.Place && LastSetpoint != ReefscapeSetpoints.L1 && !scoreSource.isPlaying && CurrentRobotMode == ReefscapeRobotMode.Coral && !_playedScoreSound)
        // {
        //     scoreSource.Play();
        //     _playedScoreSound = true;
        // }
        //
        // // EE Rollers
        // float endEffectorRollerSpeed = Mathf.Max(new float[]
        // {
        //     Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
        //     Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
        //     Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
        // });
        // if (endEffectorRollerSpeed > 5 && !endEffectorRollerSource.isPlaying)
        // {
        //     endEffectorRollerSource.Play();
        // }
        // else if (endEffectorRollerSpeed <= 5 && endEffectorRollerSource.isPlaying)
        // {
        //     endEffectorRollerSource.Stop();
        // }
    }
    
    private bool AtSetpoint(BlazingBulldogsASetpoint stp)
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), stp.elevatorHeight, 2f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp.armAngle, 2f) &&
            Utils.InAngularRange(wrist.GetSingleAxisAngle(JointAxis.Z), stp.wristAngle, 2f);
    }
    
    private bool AtSetpoint()
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), _elevatorTargetHeight, 7f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _armTargetAngle, 20f) &&
            Utils.InAngularRange(wrist.GetSingleAxisAngle(JointAxis.Z), _wristTargetAngle, 20f);
    }
    
    private void AlgaeSlider()
    {
        if (algaeIntake.GamePiece != null)
        {
            var localSliderSpaceY = algaeTarget.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).y;
            algaeSlider.localPosition = new Vector3(0, localSliderSpaceY, 0);
        }
    }
    
    private void CoralSlider()
    {
        if (coralIntake.GamePiece != null)
        {
            var localSliderSpaceZ = coralTarget.transform.InverseTransformPoint(coralIntake.GamePiece.transform.position).z;
            coralSlider.localPosition = new Vector3(0, 0, localSliderSpaceZ);
        }
    }

    private void FixedUpdate()
    {
        bool hasAlgae = _algaeController.HasPiece();
        bool hasCoral = _coralController.HasPiece();
        
        AlgaeSlider();
        CoralSlider();

        climbCollider.enabled = _cageDetector.OverlapBox().Length > 3;
        
        Debug.Log(_cageDetector.OverlapBox().Length);

        if (IsIntaking)
        {
            _placedVerticalCoral = false;
        }
        
        _algaeController.SetTargetState(algaeStowState);
        _coralController.SetTargetState(coralStowState);

        if (hasCoral && CurrentSetpoint != ReefscapeSetpoints.Place)
        {
            _playedScoreSound = false;
        }
        
        if (!IntakeAction.IsPressed())
        {
            _algaeController.RequestIntake(algaeIntake, false);
            _coralController.RequestIntake(coralIntake, false);
        }

        switch (CurrentSetpoint)
        {
            case ReefscapeSetpoints.Stow:
                SetSetpoint(stow);
                break;
            case ReefscapeSetpoints.Intake:
                if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    SetSetpoint(groundIntake);
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
                break;
            case ReefscapeSetpoints.L1:
                SetSetpoint(l1);
                break;
            case ReefscapeSetpoints.Stack:
                SetSetpoint(lollipopAlgae);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L2:
                SetSetpoint(l2);
                break;
            case ReefscapeSetpoints.LowAlgae:
                SetSetpoint(lowAlgae);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L3:
                SetSetpoint(l3);
                break;
            case ReefscapeSetpoints.HighAlgae:
                SetSetpoint(highAlgae);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L4:
                SetSetpoint(l4);
                break;
            case ReefscapeSetpoints.Processor:
                SetSetpoint(processor);
                break;
            case ReefscapeSetpoints.Barge:
                SetSetpoint(barge);
                break;
            case ReefscapeSetpoints.RobotSpecial:
                SetState(ReefscapeSetpoints.Stow);
                break;
            case ReefscapeSetpoints.Climb:
                SetSetpoint(climbPrep);
                break;
            case ReefscapeSetpoints.Climbed:
                SetSetpoint(climbed);
                break;
        }
        
        UpdateSetpoints();
        UpdateAudio();
        UpdateRollers(hasCoral, hasAlgae);
    }
}
}