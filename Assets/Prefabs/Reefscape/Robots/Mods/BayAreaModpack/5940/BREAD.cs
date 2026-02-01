using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._5940
{
public class BREAD: ReefscapeRobotBase
{
    [Header("Components")]
    [SerializeField] private GenericElevator elevator;
    [SerializeField] private GenericJoint arm;
    [SerializeField] private GenericJoint climber;
    [SerializeField] private GenericJoint intakeJoint;
    // [SerializeField] private GenericJoint floatingRoller;
    [SerializeField] private GenericRoller[] intakeRollers;
    [SerializeField] private GenericRoller[] endEffectorRollers;

    [Header("PIDs")]
    [SerializeField] private PidConstants armPid;
    [SerializeField] private PidConstants climbPid;
    [SerializeField] private PidConstants intakePid;
    // [SerializeField] private PidConstants floatingRollerPid;
    
    [Header("Intakes")]
    [SerializeField] private ReefscapeGamePieceIntake coralIntake;
    [SerializeField] private ReefscapeGamePieceIntake algaeIntake;
    
    [Header("Game Piece Stow States")]
    [SerializeField] private GamePieceState coralStowState;
    [SerializeField] private GamePieceState algaeStowState;
    
    [Header("Setpoints")]
    [SerializeField] private BREADSetpoint stow;
    [SerializeField] private BREADSetpoint intake;
    [SerializeField] private BREADSetpoint l1;
    [SerializeField] private BREADSetpoint l2;
    [SerializeField] private BREADSetpoint l3;
    [SerializeField] private BREADSetpoint l4;
    [SerializeField] private BREADSetpoint lowAlgae;
    [SerializeField] private BREADSetpoint highAlgae;
    [SerializeField] private BREADSetpoint lollipopAlgae;
    [SerializeField] private BREADSetpoint groundAlgae;
    [SerializeField] private BREADSetpoint bargePrep;
    [SerializeField] private BREADSetpoint bargePlace;
    [SerializeField] private BREADSetpoint processor;
    [SerializeField] private BREADSetpoint climbPrep;
    [SerializeField] private BREADSetpoint climbed;

    private float _elevatorTargetHeight;
    private float _armTargetAngle;
    private float _climberTargetAngle;
    private float _intakeTargetAngle;

    private bool _alreadyPlaced;
    private bool _wasCoral;
    private bool _isScoring;
    
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
    
    protected override void Start()
    {
        base.Start();
        
        arm.SetPid(armPid);
        climber.SetPid(climbPid);
        intakeJoint.SetPid(intakePid);
        // floatingRoller.SetPid(floatingRollerPid);
        
        _elevatorTargetHeight = 0;
        _armTargetAngle = 0;
        _climberTargetAngle = 0;
        _intakeTargetAngle = 0;
        
        _alreadyPlaced = false;
        _wasCoral = false;
        _isScoring = false;
        
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
    }

    private void LateUpdate()
    {
        arm.UpdatePid(armPid);
        climber.UpdatePid(climbPid);
        intakeJoint.UpdatePid(intakePid);
        // floatingRoller.UpdatePid(floatingRollerPid);
    }

    private void SetSetpoint(BREADSetpoint setpoint)
    {
        _elevatorTargetHeight = setpoint.elevatorHeight;
        _armTargetAngle = setpoint.armAngle;
        _climberTargetAngle = setpoint.climbAngle;
        
        if (_elevatorTargetHeight < 15 && Mathf.Abs(arm.transform.position.x - _armTargetAngle) < 0.1f)
        {
            _elevatorTargetHeight = 15f;
        }
    }

    private void UpdateSetpoints()
    {
        elevator.SetTarget(_elevatorTargetHeight);
        arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X);
        climber.SetTargetAngle(_climberTargetAngle).withAxis(JointAxis.X);
        intakeJoint.SetTargetAngle(_intakeTargetAngle);
        // floatingRoller.SetTargetAngle(-20f).withAxis(JointAxis.X);
    }
    
    private IEnumerator PlacePiece(bool hasCoral, bool hasAlgae)
    {
        // if (CurrentRobotMode == ReefscapeRobotMode.Coral)
        // {
        //     _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5f));
        //     if (hasAlgae)
        //     {
        //         SetRobotMode(ReefscapeRobotMode.Algae);
        //     }
        // }
        // else if (CurrentRobotMode == ReefscapeRobotMode.Algae)
        // {
        //     if (LastSetpoint == ReefscapeSetpoints.Barge)
        //     {
        //         yield return new WaitForSeconds(0.165f);
        //     }
        //     _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, -7.5f));
        // }
        if (hasAlgae)
        {
            if (LastSetpoint == ReefscapeSetpoints.Barge)
            {
                yield return new WaitForSeconds(0.165f);
            }
            _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, -7.5f));
            if (hasCoral)
            {
                SetRobotMode(ReefscapeRobotMode.Coral);
            }
        }
        else if (hasCoral)
        {
            _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5f));
        }
    }

    public void UpdateRollers(bool hasCoral, bool hasAlgae)
    {
        if (IntakeAction.IsPressed() && !_coralController.atTarget)
        {
            foreach (var roller in intakeRollers)
            {
                roller.ChangeAngularVelocity(1000f);
            }
        }
    }

    private void FixedUpdate()
    {
        bool hasAlgae = _algaeController.HasPiece();
        bool hasCoral = _coralController.HasPiece();
        
        _algaeController.SetTargetState(algaeStowState);
        _coralController.SetTargetState(coralStowState);
        
        switch (CurrentSetpoint)
        {
            case ReefscapeSetpoints.Stow:
                if (hasCoral && !_coralController.atTarget)
                {
                    SetSetpoint(intake);
                }
                else
                {
                    SetSetpoint(stow);
                }
                break;
            case ReefscapeSetpoints.Intake:
                if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    SetSetpoint(intake);
                }
                else
                {
                    SetSetpoint(groundAlgae);
                }
                
                _algaeController.RequestIntake(algaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae);
                _coralController.RequestIntake(coralIntake, CurrentRobotMode == ReefscapeRobotMode.Coral && !hasCoral);
                break;
            case ReefscapeSetpoints.Place:
                if (LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    SetSetpoint(bargePlace);
                }
                if (OuttakeAction.triggered)
                {
                    StartCoroutine(PlacePiece(hasCoral, hasAlgae)); 
                }
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
                SetSetpoint(bargePrep);
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

        if (hasCoral && hasAlgae)
        {
            SetRobotMode(ReefscapeRobotMode.Algae);
        }
        
        UpdateSetpoints();
        UpdateRollers(hasCoral, hasAlgae);
    }
}
}