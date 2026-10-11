using System;
using UnityEngine;
using SchoolRuleZero;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class PlayerController : MonoBehaviour, IMovementStateContext
{
    // Preserve the step-1 API while sharing stable IDs with the standalone FSM.
    public enum PlayerState
    {
        IDLE = (int)MovementStateId.IDLE,
        MOVE = (int)MovementStateId.MOVE,
        JUMP = (int)MovementStateId.JUMP,
        FALL = (int)MovementStateId.FALL,
        CROUCH = (int)MovementStateId.CROUCH
    }

    [Header("Movement: XZ floor, Y jump")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float jumpSpeed = 7f;
    [SerializeField, Range(0f, 1f)] private float jumpCancelMultiplier = 0.45f;
    [SerializeField, Range(0f, 1f)] private float crouchSpeedMultiplier = 0.4f;

    [Header("Crouch")]
    [SerializeField, Range(0.1f, 1f)] private float crouchHeightRatio = 0.6f;
    [SerializeField, Min(0.001f)] private float clearanceSkin = 0.02f;
    [SerializeField] private LayerMask obstructionLayers = ~0;
    [Tooltip("Optional direct child mesh for visible prototype crouching.")]
    [SerializeField] private Transform visualRoot;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.08f;
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.65f;
    [Header("Diagnostics")]
    [SerializeField] private bool logStateTransitions;

    public PlayerState CurrentState => movementStateMachine == null
        ? PlayerState.IDLE : (PlayerState)movementStateMachine.CurrentState;
    public bool IsGrounded { get; private set; }
    public bool IsCrouching { get; private set; }
    public Vector3 FacingDirection { get; private set; } = Vector3.right;
    public event Action<PlayerState, PlayerState> StateChanged;
    public Func<bool> MovementSuppressed { get; set; }
    public bool GameplayEnabled { get; set; } = true;

    private readonly RaycastHit[] groundHits = new RaycastHit[32];
    private readonly Collider[] clearanceHits = new Collider[32];
    private Rigidbody body;
    private PlayerMovementStateMachine movementStateMachine;
    private CapsuleCollider capsule;
    private Vector2 moveInput;
    private bool crouchHeld;
    private bool jumpRequested;
    private bool jumpReleased;
    private bool groundContactFromLastStep;
    private bool hasFocus = true;
    private float standingHeight;
    private Vector3 standingCenter;
    private float crouchingHeight;
    private Vector3 standingVisualScale;
    private Vector3 standingVisualPosition;

    private Vector3 Velocity
    {
        get
        {
            return body.linearVelocity;
        }
        set
        {
            body.linearVelocity = value;
        }
    }

    private void Reset()
    {
        CapsuleCollider shape = GetComponent<CapsuleCollider>();
        shape.direction = 1;
        shape.height = 2f;
        shape.radius = 0.4f;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        // Physics probes below use an upright, unit-scale capsule.
        if (capsule.direction != 1 || capsule.isTrigger || body.isKinematic ||
            !body.useGravity ||
            (transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f ||
            Vector3.Dot(transform.up, Vector3.up) < 0.9999f)
        {
            Debug.LogError("PlayerController requires a vertical solid capsule, " +
                "unit world scale, and a dynamic Rigidbody with gravity.", this);
            enabled = false;
            return;
        }
        body.constraints |= RigidbodyConstraints.FreezeRotation;
        standingHeight = Mathf.Max(capsule.height, capsule.radius * 2f);
        standingCenter = capsule.center;
        crouchingHeight = Mathf.Clamp(standingHeight * crouchHeightRatio,
            capsule.radius * 2f, standingHeight);
        if (visualRoot != null)
        {
            if (visualRoot.parent != transform)
            {
                Debug.LogWarning("Visual Root must be a direct child of Player.", this);
                visualRoot = null;
            }
            else
            {
                standingVisualScale = visualRoot.localScale;
                standingVisualPosition = visualRoot.localPosition;
            }
        }
        movementStateMachine = new PlayerMovementStateMachine(this);
        movementStateMachine.StateChanged += OnMovementStateChanged;
    }

    private void Update()
    {
        // Read keys once per render frame; consume jump once per physics step.
        if (!hasFocus || !GameplayEnabled || Time.timeScale == 0)
        {
            ClearInput();
            return;
        }
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            ClearInput();
            return;
        }
        moveInput = new Vector2(
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
        crouchHeld = keyboard.cKey.isPressed;
        jumpRequested |= keyboard.spaceKey.wasPressedThisFrame;
        jumpReleased |= keyboard.spaceKey.wasReleasedThisFrame;
#else
        moveInput = new Vector2(
            (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
            (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
        crouchHeld = Input.GetKey(KeyCode.C);
        jumpRequested |= Input.GetKeyDown(KeyCode.Space);
        jumpReleased |= Input.GetKeyUp(KeyCode.Space);
#endif
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        Vector3 velocity = Velocity;
        IsGrounded = velocity.y <= 0.05f && (groundContactFromLastStep || ProbeGround());
        groundContactFromLastStep = false;
        UpdateCrouch();

        if (jumpRequested && IsGrounded && !IsCrouching && jumpSpeed > 0f &&
            (MovementSuppressed == null || !MovementSuppressed()))
        {
            velocity.y = jumpSpeed;
            IsGrounded = false;
        }
        // A request made in the air is discarded: no automatic landing jump.
        jumpRequested = false;
        velocity.y = JumpRules.CancelAscent(velocity.y, jumpReleased, jumpCancelMultiplier);
        jumpReleased = false;
        Velocity = velocity;
        movementStateMachine.FixedTick(new MovementFrame(
            IsGrounded, IsCrouching, moveInput.sqrMagnitude > 0.001f, velocity.y));
    }

    void IMovementStateContext.ApplyPlanarMovement(bool useCrouchSpeed)
    {
        if (MovementSuppressed != null && MovementSuppressed()) return;
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
        if (direction.sqrMagnitude > 0.001f)
            FacingDirection = direction.normalized;
        // A crouched character can fall off a ledge; retain its slower motion.
        float speed = moveSpeed * ((useCrouchSpeed || IsCrouching)
            ? crouchSpeedMultiplier : 1f);
        Vector3 velocity = Velocity;
        velocity.x = direction.x * speed;
        velocity.z = direction.z * speed;
        Velocity = velocity;
    }

    private bool ProbeGround()
    {
        GetCapsule(standing: false, skin: clearanceSkin,
            out Vector3 top, out Vector3 bottom, out float radius);
        int count = Physics.CapsuleCastNonAlloc(top, bottom, radius,
            Vector3.down, groundHits, groundProbeDistance + clearanceSkin,
            groundLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (!IsOwnCollider(hit.collider) && hit.normal.y >= minimumGroundNormalY)
                return true;
        }
        return false;
    }

    private void OnCollisionEnter(Collision collision) => CheckGroundContacts(collision);
    private void OnCollisionStay(Collision collision) => CheckGroundContacts(collision);

    private void CheckGroundContacts(Collision collision)
    {
        if ((groundLayers.value & (1 << collision.gameObject.layer)) == 0)
            return;
        for (int i = 0; i < collision.contactCount; i++)
            if (collision.GetContact(i).normal.y >= minimumGroundNormalY)
                groundContactFromLastStep = true;
    }

    private void UpdateCrouch()
    {
        if (crouchHeld && IsGrounded)
        {
            SetCrouching(true);
            return;
        }
        if (IsCrouching && !crouchHeld && CanStand())
            SetCrouching(false);
    }

    private bool CanStand()
    {
        GetCapsule(standing: true, skin: clearanceSkin,
            out Vector3 top, out Vector3 bottom, out float radius);
        int count = Physics.OverlapCapsuleNonAlloc(top, bottom, radius,
            clearanceHits, obstructionLayers, QueryTriggerInteraction.Ignore);
        // Never stand if a saturated buffer prevents proving clearance.
        if (count == clearanceHits.Length)
            return false;
        for (int i = 0; i < count; i++)
            if (!IsOwnCollider(clearanceHits[i]))
                return false;
        return true;
    }

    private void GetCapsule(bool standing, float skin,
        out Vector3 top, out Vector3 bottom, out float radius)
    {
        float height = standing ? standingHeight : capsule.height;
        Vector3 localCenter = standing ? standingCenter : capsule.center;
        Vector3 center = transform.TransformPoint(localCenter);
        float halfSegment = Mathf.Max(0f, height * 0.5f - capsule.radius);
        radius = Mathf.Max(0.001f, capsule.radius - skin);
        top = center + Vector3.up * halfSegment;
        bottom = center - Vector3.up * halfSegment;
    }

    private bool IsOwnCollider(Collider other)
    {
        return other == null || other.attachedRigidbody == body ||
            other.transform == transform || other.transform.IsChildOf(transform);
    }

    private void SetCrouching(bool crouching)
    {
        if (IsCrouching == crouching)
            return;
        IsCrouching = crouching;
        capsule.height = crouching ? crouchingHeight : standingHeight;
        // Lower the center by half the lost height, keeping the feet fixed.
        capsule.center = standingCenter + Vector3.down *
            ((standingHeight - capsule.height) * 0.5f);
        if (visualRoot != null)
        {
            Vector3 scale = standingVisualScale;
            scale.y *= capsule.height / standingHeight;
            visualRoot.localScale = scale;
            visualRoot.localPosition = standingVisualPosition + Vector3.down *
                ((standingHeight - capsule.height) * 0.5f);
        }
    }

    private void OnMovementStateChanged(MovementStateId previous, MovementStateId next)
    {
        if (logStateTransitions)
            Debug.Log($"[Movement] {previous} -> {next}; grounded={IsGrounded}", this);
        StateChanged?.Invoke((PlayerState)previous, (PlayerState)next);
    }

    public void SetVisualRoot(Transform child)
    {
        if (child == null || child.parent != transform)
            throw new ArgumentException("Visual must be a direct child of the player.");
        visualRoot = child;
        standingVisualScale = child.localScale;
        standingVisualPosition = child.localPosition;
    }

    private void OnDrawGizmosSelected()
    {
        CapsuleCollider shape = GetComponent<CapsuleCollider>();
        if (shape == null) return;
        Gizmos.color = IsGrounded ? Color.green : Color.yellow;
        Vector3 feet = transform.TransformPoint(shape.center) -
            Vector3.up * (shape.height * .5f);
        Gizmos.DrawLine(feet, feet + Vector3.down * groundProbeDistance);
        Gizmos.DrawWireSphere(feet + Vector3.down * groundProbeDistance, .06f);
    }

    private void ClearInput()
    {
        moveInput = Vector2.zero;
        crouchHeld = false;
        jumpRequested = false;
        jumpReleased = false;
    }

    private void OnDisable() => ClearInput();

    private void OnApplicationFocus(bool hasFocus)
    {
        this.hasFocus = hasFocus;
        if (!hasFocus)
            ClearInput();
    }
}
