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
using UnityEngine.InputSystem;

namespace Prefabs.Reefscape.Robots.Mods.AlonsMod._3501
{
public class Firebots: ReefscapeRobotBase
{
    
    [Header("Components")]
    [SerializeField] private GenericElevator elevator;
    [SerializeField] private GenericJoint dale;
    [SerializeField] private GenericRoller daleRoller;
    [SerializeField] private GenericRoller upperTootsieRoller;
    [SerializeField] private GenericRoller lowerTootsieRoller;
    [SerializeField] private GenericRoller backRightFunnelRoller;
    [SerializeField] private GenericRoller backLeftFunnelRoller;
    [SerializeField] private GenericRoller frontRightFunnelRoller;
    [SerializeField] private GenericRoller frontLeftFunnelRoller;

    [Header("Constants")]
    [SerializeField] private PidConstants dalePid;
    [SerializeField] private float tootsieRollersOuttakeSpeed;
    [SerializeField] private float tootsieRollersIntakeSpeed;
    [SerializeField] private float funnelRollersIntakeSpeed;
    [SerializeField] private float funnelRollersL1Speed;

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
    
    private ReefscapeAutoAlign _align;
    
    private float _daleXOffset;
    
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
        
        _align = gameObject.GetComponent<ReefscapeAutoAlign>();
        _daleXOffset = -2.75f;
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
            if (LastSetpoint == ReefscapeSetpoints.L1)
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, -3.6f), 0.75f, 0.6f);
            }
            else
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 3.0f), 0.25f, 0.5f);
            }
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

        if (CurrentSetpoint == ReefscapeSetpoints.Intake && !hasCoral)
        {
            upperTootsieRoller.ChangeAngularVelocity(-tootsieRollersIntakeSpeed);
            lowerTootsieRoller.ChangeAngularVelocity(tootsieRollersIntakeSpeed);
            
            backLeftFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeSpeed);
            backRightFunnelRoller.ChangeAngularVelocity(0.8f * funnelRollersIntakeSpeed);
            frontLeftFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeSpeed);
            frontRightFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeSpeed);
        }
        
        if (CurrentSetpoint == ReefscapeSetpoints.Place)
        {
            if (LastSetpoint == ReefscapeSetpoints.L1)
            {
                upperTootsieRoller.ChangeAngularVelocity(tootsieRollersOuttakeSpeed);
                lowerTootsieRoller.ChangeAngularVelocity(-tootsieRollersOuttakeSpeed);
                
                backLeftFunnelRoller.ChangeAngularVelocity(funnelRollersL1Speed);
                backRightFunnelRoller.ChangeAngularVelocity(0.8f * -funnelRollersL1Speed);
                frontLeftFunnelRoller.ChangeAngularVelocity(funnelRollersL1Speed);
                frontRightFunnelRoller.ChangeAngularVelocity(-funnelRollersL1Speed);
            }
            else
            {
                upperTootsieRoller.ChangeAngularVelocity(-tootsieRollersOuttakeSpeed);
                lowerTootsieRoller.ChangeAngularVelocity(tootsieRollersOuttakeSpeed);
            }
        }
        
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
                // StartCoroutine(PlacePiece());
                PlacePiece();
                break;
            case ReefscapeSetpoints.L1:
                SetSetpoint(l1);
                _align.offset = new Vector3(0f, 0f, 8f);
                break;
            case ReefscapeSetpoints.Stack:
                break;
            case ReefscapeSetpoints.L2:
                SetSetpoint(l2);
                _align.offset = new Vector3(0f, 0f, 8f);
                // align.offset = new Vector3(0, 0, zOffset);
                break;
            case ReefscapeSetpoints.LowAlgae:
                SetSetpoint(lowAlgae);
                _align.offset = new Vector3(_daleXOffset, 0f, 8f);
                break;
            case ReefscapeSetpoints.L3:
                SetSetpoint(l3);
                _align.offset = new Vector3(0f, 0f, 8f);
                break;
            case ReefscapeSetpoints.HighAlgae:
                SetSetpoint(highAlgae);
                _align.offset = new Vector3(_daleXOffset, 0f, 8f);
                break;
            case ReefscapeSetpoints.L4:
                SetSetpoint(l4);
                _align.offset = new Vector3(0f, 0f, 8f);
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