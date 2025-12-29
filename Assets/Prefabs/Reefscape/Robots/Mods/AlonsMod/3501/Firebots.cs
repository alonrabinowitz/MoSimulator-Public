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

    [Header("Constants")]
    [SerializeField] private PidConstants dalePid;
    [SerializeField] private float tootsieRollersOuttakeVelocity;
    [SerializeField] private float tootsieRollersIntakeVelocity;

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

    private IEnumerator PlacePiece()
    {
        if (CurrentRobotMode == ReefscapeRobotMode.Coral)
        {
            _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 3.0f), 0.5f, 0.5f);
            yield return new WaitForSeconds(0.5f);
            SetState(ReefscapeSetpoints.Stow);
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
                if (!hasCoral || !_coralController.atTarget)
                {
                    upperTootsieRoller.ChangeAngularVelocity(-tootsieRollersIntakeVelocity);
                    lowerTootsieRoller.ChangeAngularVelocity(tootsieRollersIntakeVelocity);
                }
                break;
            case ReefscapeSetpoints.Place:
                upperTootsieRoller.ChangeAngularVelocity(-tootsieRollersOuttakeVelocity);
                lowerTootsieRoller.ChangeAngularVelocity(tootsieRollersOuttakeVelocity);
                StartCoroutine(PlacePiece());
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