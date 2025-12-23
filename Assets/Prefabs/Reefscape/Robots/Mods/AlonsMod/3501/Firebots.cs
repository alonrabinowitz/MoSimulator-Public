using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.AlonsMod._3501
{
public class Firebots: ReefscapeRobotBase
{
    
    [Header("Components")]
    [SerializeField] private GenericElevator elevator;
    [SerializeField] private GenericJoint dale;
    [SerializeField] private GenericRoller daleRoller;

    [Header("PIDs")]
    [SerializeField] private PidConstants dalePid;

    [Header("Setpoints")]
    [SerializeField] private FirebotsSetpoint stow;
    [SerializeField] private FirebotsSetpoint intake;
    [SerializeField] private FirebotsSetpoint l1;
    [SerializeField] private FirebotsSetpoint l2;
    [SerializeField] private FirebotsSetpoint l3;
    [SerializeField] private FirebotsSetpoint l4;
    [SerializeField] private FirebotsSetpoint lowAlgae;
    [SerializeField] private FirebotsSetpoint highAlgae;

    [Header("Intake Components")] 
    [SerializeField] private ReefscapeGamePieceIntake coralIntake;

    [Header("Game Piece States")]
    [SerializeField] private GamePieceState coralStowState;
    
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
    
    private float _elevatorTargetHeight;
    private float _daleTargetAngle;
    private float _daleRollerTargetVelocity;
    
    protected override void Start()
    {
        base.Start();
        
        dale.SetPid(dalePid);

        _elevatorTargetHeight = 0;
        _daleTargetAngle = 0;
        _daleRollerTargetVelocity = 0;
        
        RobotGamePieceController.SetPreload(coralStowState);
        _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());

        _coralController.gamePieceStates = new[]
        {
            coralStowState
        };
        _coralController.intakes.Add(coralIntake);
    }

    private void SetSetpoint(FirebotsSetpoint setpoint)
    {
        _elevatorTargetHeight = setpoint.elevatorHeight;
        _daleTargetAngle = setpoint.daleAngle;
        _daleRollerTargetVelocity = setpoint.daleRollerVelocity;
    }

    private void PlacePiece()
    {
        if (CurrentRobotMode == ReefscapeRobotMode.Coral)
        {
            _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 3.0f), 0.5f, 0.5f);
        }
    }

    private void UpdateSetpoints()
    {
        elevator.SetTarget(_elevatorTargetHeight);
        dale.SetTargetAngle(_daleTargetAngle).withAxis(JointAxis.Y);
        // daleRoller.SetAngularVelocity(_daleRollerTargetVelocity);
        if (_daleRollerTargetVelocity > 0)
        {
            daleRoller.SetAngularVelocity(_daleRollerTargetVelocity);
        }
        else
        {
            daleRoller.stopAngularVelocity();
        }
    }

    private void LateUpdate()
    {
        dale.UpdatePid(dalePid);
    }

    private void FixedUpdate()
    {
        bool hasCoral = _coralController.HasPiece();
        _coralController.SetTargetState(coralStowState);
        
        switch (CurrentSetpoint)
        {
            case ReefscapeSetpoints.Stow:
                SetSetpoint(stow);
                break;
            case ReefscapeSetpoints.Intake:
                SetSetpoint(intake);
                _coralController.RequestIntake(coralIntake, CurrentRobotMode == ReefscapeRobotMode.Coral && !hasCoral);
                break;
            case ReefscapeSetpoints.Place:
                PlacePiece();
                break;
            case ReefscapeSetpoints.L1:
                SetSetpoint(l1);
                break;
            case ReefscapeSetpoints.Stack:
                break;
            case ReefscapeSetpoints.L2:
                SetSetpoint(l2);
                break;
            case ReefscapeSetpoints.LowAlgae:
                SetSetpoint(lowAlgae);
                break;
            case ReefscapeSetpoints.L3:
                SetSetpoint(l3);
                break;
            case ReefscapeSetpoints.HighAlgae:
                SetSetpoint(highAlgae);
                break;
            case ReefscapeSetpoints.L4:
                SetSetpoint(l4);
                break;
            case ReefscapeSetpoints.Processor:
                break;
            case ReefscapeSetpoints.Barge:
                break;
            case ReefscapeSetpoints.RobotSpecial:
                SetState(ReefscapeSetpoints.Stow);
                break;
            case ReefscapeSetpoints.Climb:
                break;
            case ReefscapeSetpoints.Climbed:
                break;
        }
        
        UpdateSetpoints();
    }


}
}