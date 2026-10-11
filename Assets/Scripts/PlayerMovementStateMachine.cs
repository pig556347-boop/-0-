using System;

namespace SchoolRuleZero
{
    // Keep these values aligned with PlayerController.PlayerState for compatibility.
    public enum MovementStateId { IDLE, MOVE, JUMP, FALL, CROUCH }

    /// <summary>A single physics-step snapshot. No Unity dependencies.</summary>
    public readonly struct MovementFrame
    {
        public readonly bool Grounded;
        public readonly bool Crouching;
        public readonly bool Moving;
        public readonly float VerticalSpeed;

        public MovementFrame(bool grounded, bool crouching, bool moving, float verticalSpeed)
        {
            Grounded = grounded;
            Crouching = crouching;
            Moving = moving;
            VerticalSpeed = verticalSpeed;
        }
    }

    public interface IMovementStateContext
    {
        void ApplyPlanarMovement(bool useCrouchSpeed);
    }

    public abstract class PlayerMovementState
    {
        public abstract MovementStateId Id { get; }
        public virtual void Enter(IMovementStateContext context) { }
        public virtual void Exit(IMovementStateContext context) { }
        public abstract void FixedTick(IMovementStateContext context);

        // Shared priority prevents different states from disagreeing on landing.
        public virtual MovementStateId ResolveNext(MovementFrame frame)
        {
            if (!frame.Grounded)
                return frame.VerticalSpeed > 0f ? MovementStateId.JUMP : MovementStateId.FALL;
            if (frame.Crouching)
                return MovementStateId.CROUCH;
            return frame.Moving ? MovementStateId.MOVE : MovementStateId.IDLE;
        }
    }

    public sealed class IdleMovementState : PlayerMovementState
    {
        public override MovementStateId Id => MovementStateId.IDLE;
        public override void FixedTick(IMovementStateContext context) =>
            context.ApplyPlanarMovement(false);
    }

    public sealed class MoveMovementState : PlayerMovementState
    {
        public override MovementStateId Id => MovementStateId.MOVE;
        public override void FixedTick(IMovementStateContext context) =>
            context.ApplyPlanarMovement(false);
    }

    public sealed class JumpMovementState : PlayerMovementState
    {
        public override MovementStateId Id => MovementStateId.JUMP;
        public override void FixedTick(IMovementStateContext context) =>
            context.ApplyPlanarMovement(false);
    }

    public sealed class FallMovementState : PlayerMovementState
    {
        public override MovementStateId Id => MovementStateId.FALL;
        public override void FixedTick(IMovementStateContext context) =>
            context.ApplyPlanarMovement(false);
    }

    public sealed class CrouchMovementState : PlayerMovementState
    {
        public override MovementStateId Id => MovementStateId.CROUCH;
        public override void FixedTick(IMovementStateContext context) =>
            context.ApplyPlanarMovement(true);
    }

    /// <summary>
    /// Reuses one object per state. Runs at most one transition and one state tick
    /// per physics step; Enter/Exit never own input or ground queries.
    /// </summary>
    public sealed class PlayerMovementStateMachine
    {
        private readonly PlayerMovementState[] states =
        {
            new IdleMovementState(), new MoveMovementState(),
            new JumpMovementState(), new FallMovementState(), new CrouchMovementState()
        };
        private readonly IMovementStateContext context;
        private PlayerMovementState current;
        private bool isTicking;

        public MovementStateId CurrentState => current.Id;
        public event Action<MovementStateId, MovementStateId> StateChanged;

        public PlayerMovementStateMachine(IMovementStateContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            current = states[(int)MovementStateId.IDLE];
            current.Enter(context);
        }

        public void FixedTick(MovementFrame frame)
        {
            // A listener must not recursively advance the same physics step.
            if (isTicking)
                throw new InvalidOperationException("Movement FSM cannot tick recursively.");
            isTicking = true;
            try
            {
                MovementStateId next = current.ResolveNext(frame);
                MovementStateId previous = current.Id;
                if (next != previous)
                {
                    current.Exit(context);
                    current = states[(int)next];
                    current.Enter(context);
                }
                // Apply the new state's motion before notifying observers.
                current.FixedTick(context);
                if (next != previous)
                    StateChanged?.Invoke(previous, next);
            }
            finally
            {
                isTicking = false;
            }
        }
    }
}
