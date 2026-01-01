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
    [SerializeField] private GenericRoller backLeftFunnelRoller;
    [SerializeField] private GenericRoller backRightFunnelRoller;
    [SerializeField] private GenericRoller frontLeftFunnelRoller;
    [SerializeField] private GenericRoller frontRightFunnelRoller;

    [Header("Constants")]
    [SerializeField] private PidConstants dalePid;
    [SerializeField] private float tootsieRollersOuttakeVelocity;
    [SerializeField] private float tootsieRollersIntakeVelocity;
    [SerializeField] private float funnelRollersL1OuttakeVelocity;
    [SerializeField] private float funnelRollersIntakeVelocity;

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
    
    [Header("Colliders")]
    [SerializeField] private GameObject leftFunnelWall;
    [SerializeField] private GameObject lowerFunnelFloor;
    [SerializeField] private GameObject l1LowerFunnelWall;

    [Header("Game Piece States")]
    [SerializeField] private GamePieceState coralStowState;
    
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
    
    private float _elevatorTargetHeight;
    private float _daleTargetAngle;
    private float _daleRollerTargetVelocity;
    
    // private BoxCollider _upperFunnelFloorCollider;
    // private BoxCollider _lowerFunnelFloorCollider;
    
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
        
        // _upperFunnelFloorCollider = upperFunnelFloor.GetComponent<BoxCollider>();
        // _lowerFunnelFloorCollider = lowerFunnelFloor.GetComponent<BoxCollider>();
        
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
            if (LastSetpoint == ReefscapeSetpoints.L1)
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, -3.5f), 0.65f, 0.5f);
                yield return new WaitForSeconds(0.75f);
            }
            else if (LastSetpoint == ReefscapeSetpoints.L4)
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0f, 3.5f), 0.6f, 0.5f);
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0f, 3.25f), 0.5f, 0.6f);
                yield return new WaitForSeconds(0.2f);
            }

            if (!IntakeAction.inProgress && CurrentSetpoint != ReefscapeSetpoints.HighAlgae && CurrentSetpoint != ReefscapeSetpoints.LowAlgae)
            {
                SetState(ReefscapeSetpoints.Stow);
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

        if (CurrentSetpoint == ReefscapeSetpoints.L1)
        {
            lowerFunnelFloor.SetActive(false);
            l1LowerFunnelWall.SetActive(true);
        }
        else
        {
            lowerFunnelFloor.SetActive(true);
            l1LowerFunnelWall.SetActive(false);
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

        if (CurrentSetpoint == ReefscapeSetpoints.Stow && !_coralController.atTarget)
        {
            SetState(ReefscapeSetpoints.Intake);
        }
        
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
                    
                    backLeftFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeVelocity);
                    backRightFunnelRoller.ChangeAngularVelocity(0.8f * funnelRollersIntakeVelocity);
                    frontLeftFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeVelocity);
                    frontRightFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeVelocity);
                }
                
                if ((hasCoral && !_coralController.atTarget) || coralIntake.hasGamePiece)
                {
                    // _upperFunnelFloorCollider.enabled = false;
                    // _lowerFunnelFloorCollider.enabled = false;
                    leftFunnelWall.SetActive(false);
                    lowerFunnelFloor.SetActive(false);
                }
                else
                {
                    // _upperFunnelFloorCollider.enabled = true;
                    // _lowerFunnelFloorCollider.enabled = true;
                    leftFunnelWall.SetActive(true);
                    lowerFunnelFloor.SetActive(true);
                }
                break;
            case ReefscapeSetpoints.Place:
                if (LastSetpoint == ReefscapeSetpoints.L1)
                {
                    upperTootsieRoller.ChangeAngularVelocity(tootsieRollersOuttakeVelocity);
                    lowerTootsieRoller.ChangeAngularVelocity(-tootsieRollersOuttakeVelocity);
                    
                    backLeftFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeVelocity);
                    backRightFunnelRoller.ChangeAngularVelocity(0.8f * -funnelRollersIntakeVelocity);
                    frontLeftFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeVelocity);
                    frontRightFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeVelocity);
                }
                else
                {
                    upperTootsieRoller.ChangeAngularVelocity(-tootsieRollersOuttakeVelocity);
                    lowerTootsieRoller.ChangeAngularVelocity(tootsieRollersOuttakeVelocity);
                }
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