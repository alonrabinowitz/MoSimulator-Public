using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
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

            PlacePiece();
            UpdateSetpoints();
        }
    }
}