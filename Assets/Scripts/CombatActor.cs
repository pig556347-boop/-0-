using System;
using System.Collections;
using System.Collections.Generic;
using SchoolRuleZero;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class CombatActor : MonoBehaviour, IHitReceiver
{
    public enum ActionState { IDLE, ATTACK, HIT, GUARD, DODGE, GRAB, ULTIMATE, DEAD }
    [SerializeField, Min(1)] private float maximumHP = 120;
    [SerializeField, Min(0)] private float basicDamage = 12;
    [SerializeField, Min(0)] private float heavyDamage = 24;
    [SerializeField, Min(.01f)] private float attackWindup = .12f;
    [SerializeField, Min(.01f)] private float attackRecovery = .28f;
    [SerializeField, Min(.01f)] private float hitStun = .25f;
    [SerializeField, Range(0, 1)] private float guardDamageMultiplier = .25f;
    [SerializeField, Range(.02f, .3f)] private float parryWindow = .12f;
    [SerializeField, Min(.01f)] private float dodgeDuration = .3f;
    [SerializeField, Min(.01f)] private float dodgeInvulnerability = .2f;
    [SerializeField, Min(0)] private float dodgeSpeed = 9;
    public int Team { get; private set; } = 1;
    public bool GameplayEnabled { get; set; } = true;
    public ActionState State { get; private set; }
    public CombatHealth Health { get; private set; }
    public CombatProgress Progress { get; } = new CombatProgress();
    public Vector3 Facing { get; private set; } = Vector3.left;
    public bool CanAct => GameplayEnabled && Health != null && Health.Alive && State == ActionState.IDLE;
    public event Action<CombatActor, CombatActor, Vector3> HitConfirmed;
    public event Action<DamageResult> Damaged;
    public event Action Dodged;
    public event Action Parried;
    public event Action Grabbed;
    public event Action<int> UltimateStarted;
    public event Action<int> UltimateLanded;
    public event Action<bool> AttackLanded;
    public event Action StrikeStarted;
    private readonly List<AttackHitbox> boxes = new List<AttackHitbox>();
    private Rigidbody body;
    private CombatInput input;
    private PlayerController motor;
    private float lockedUntil;
    private float guardStarted = float.NegativeInfinity;
    private float invulnerableUntil;
    private CombatActor heldTarget;
    private CombatActor heldBy;
    private bool queuedBasic;
    private int activeUltimate;
    private float counterWindowUntil;
    private bool counterTriggered;
    private bool activeHeavy;

    private void Awake()
    {
        Health = new CombatHealth(maximumHP);
        body = GetComponent<Rigidbody>(); input = GetComponent<CombatInput>();
        motor = GetComponent<PlayerController>();
        if (motor != null) motor.MovementSuppressed = () => State != ActionState.IDLE;
    }
    public void Configure(int team) { Team = team; Facing = team == 0 ? Vector3.right : Vector3.left; }
    private void Update()
    {
        if (Time.timeScale == 0 || !GameplayEnabled || Health == null || !Health.Alive) return;
        Progress.Tick(Time.time);
        if (State == ActionState.HIT && Time.time >= lockedUntil) State = ActionState.IDLE;
        if (input == null || !input.GameplayEnabled) return;
        if (State == ActionState.ATTACK && input.AttackPressed) queuedBasic = true;
        if (State == ActionState.GUARD && !input.GuardHeld) State = ActionState.IDLE;
        if (input.GuardPressed && (CanAct || State == ActionState.GUARD))
        { guardStarted = Time.time; State = ActionState.GUARD; StopPlanarMotion(); }
        else if (CanAct && input.GuardHeld) { State = ActionState.GUARD; StopPlanarMotion(); }
        if ((CanAct || State == ActionState.GUARD) && input.DodgePressed)
        { State = ActionState.IDLE; TryDodge(input.DodgeDirection); return; }
        if (CanAct && motor != null) Facing = motor.FacingDirection;
        if (CanAct && input.UltimatePressed) { TryUltimate(); return; }
        if (CanAct && input.GrabPressed) { TryGrab(); return; }
        if (CanAct && input.AttackPressed) TryAttack(false);
        else if (CanAct && input.HeavyPressed) TryAttack(true);
    }
    public void SetFacing(Vector3 direction)
    {
        direction.y = 0;
        if (direction.sqrMagnitude > .01f) Facing = direction.normalized;
    }
    public bool TryAttack(bool heavy)
    {
        if (!CanAct) return false;
        StartCoroutine(AttackRoutine(heavy)); return true;
    }
    private IEnumerator AttackRoutine(bool heavy)
    {
        State = ActionState.ATTACK; activeHeavy = heavy; StopPlanarMotion();
        int chain = 0;
        do
        {
            queuedBasic = false;
            yield return new WaitForSeconds(attackWindup * (heavy ? 1.8f : 1));
            SpawnStrike(heavy ? heavyDamage : basicDamage + chain * 2,
                heavy ? 6 : chain == 2 ? 4 : 1.2f, false);
            yield return new WaitForSeconds(attackRecovery * (heavy ? 1.6f : 1));
            chain++;
        } while (!heavy && queuedBasic && chain < 3);
        State = ActionState.IDLE;
    }
    private void SpawnStrike(float damage, float knockback, bool unblockable)
    {
        boxes.RemoveAll(box => box == null);
        StrikeStarted?.Invoke();
        GameObject box = new GameObject("Attack Hitbox");
        box.transform.position = transform.position + Facing * .95f;
        box.transform.rotation = Quaternion.LookRotation(Facing);
        AttackHitbox hitbox = box.AddComponent<AttackHitbox>(); boxes.Add(hitbox);
        hitbox.Configure(gameObject, Team, new HitRequest(gameObject, damage, Facing,
            unblockable, knockback), new Vector3(1.2f, 1.5f, 1.3f), .10f, OnConfirmedHit);
    }
    private void OnConfirmedHit(IHitReceiver receiver, Vector3 position)
    {
        CombatActor target = receiver as CombatActor;
        if (target != null)
        {
            Progress.ConfirmHit(Time.time, gain: State == ActionState.ULTIMATE ? 0 : 12);
            HitConfirmed?.Invoke(this, target, position);
            if (State == ActionState.ULTIMATE) UltimateLanded?.Invoke(activeUltimate);
            if (State == ActionState.ATTACK) AttackLanded?.Invoke(activeHeavy);
        }
    }
    public bool ReceiveHit(HitRequest hit)
    {
        if (Health.Alive && State == ActionState.ULTIMATE && activeUltimate == 2 &&
            Time.time < counterWindowUntil && !hit.Unblockable && hit.Damage > 0)
        {
            counterTriggered = true; SetFacing(-hit.Direction); return false;
        }
        bool guarding = State == ActionState.GUARD;
        bool frontal = Vector3.Dot(Facing, -hit.Direction.normalized) > .1f;
        DamageResult result = Health.Apply(hit.Damage, Time.time < invulnerableUntil,
            guarding, Time.time - guardStarted <= parryWindow, frontal,
            hit.Unblockable, guardDamageMultiplier);
        if (result.Kind == DamageKind.Ignored) return false;
        if (result.Kind == DamageKind.Parried)
        {
            CombatActor source = hit.Source == null ? null : hit.Source.GetComponent<CombatActor>();
            if (source != null) source.Stagger(.4f);
            Damaged?.Invoke(result); Parried?.Invoke(); return false;
        }
        if (result.Kind == DamageKind.Guarded && Health.Alive)
        { Damaged?.Invoke(result); return false; }
        CancelActions(); State = Health.Alive ? ActionState.HIT : ActionState.DEAD;
        Progress.BreakCombo();
        lockedUntil = Time.time + hitStun;
        StopPlanarMotion();
        ApplyKnockback(hit.Direction, hit.Knockback);
        Damaged?.Invoke(result);
        return true;
    }
    public bool TryDodge(Vector3 direction)
    {
        if (!CanAct || direction.sqrMagnitude < .01f) return false;
        StartCoroutine(DodgeRoutine(direction.normalized)); return true;
    }
    public bool TryGuard(bool freshPress)
    {
        if (!CanAct && State != ActionState.GUARD) return false;
        if (freshPress) guardStarted = Time.time;
        State = ActionState.GUARD; StopPlanarMotion(); return true;
    }
    private IEnumerator DodgeRoutine(Vector3 direction)
    {
        State = ActionState.DODGE; invulnerableUntil = Time.time +
            Mathf.Min(dodgeInvulnerability, dodgeDuration);
        Vector3 velocity = GetVelocity(); velocity.x = direction.x * dodgeSpeed;
        velocity.z = direction.z * dodgeSpeed; SetVelocity(velocity); Dodged?.Invoke();
        yield return new WaitForSeconds(dodgeDuration);
        StopPlanarMotion(); State = ActionState.IDLE;
    }
    public void Stagger(float duration)
    {
        if (!Health.Alive) return;
        CancelActions(); State = ActionState.HIT; lockedUntil = Time.time + duration;
        StopPlanarMotion();
    }
    public void ApplyKnockback(Vector3 direction, float strength)
    {
        direction.y = 0;
        float multiplier = HitImpactManager.Instance == null ? 1 :
            HitImpactManager.Instance.KnockbackMultiplier;
        Vector3 velocity = GetVelocity();
        velocity.x = direction.normalized.x * strength * multiplier;
        velocity.z = direction.normalized.z * strength * multiplier;
        SetVelocity(velocity);
    }
    public bool TryUltimate()
    {
        if (!CanAct || Progress.UltimateTier == 0) return false;
        int tier = Progress.ConsumeUltimate(); StartCoroutine(UltimateRoutine(tier)); return true;
    }
    private IEnumerator UltimateRoutine(int tier)
    {
        State = ActionState.ULTIMATE; activeUltimate = tier; StopPlanarMotion();
        UltimateStarted?.Invoke(tier);
        if (tier == 2)
        {
            counterTriggered = false; counterWindowUntil = Time.time + .65f;
            yield return new WaitForSeconds(.65f);
        }
        else yield return new WaitForSeconds(tier == 3 ? .4f : .12f);
        if (tier != 2 || counterTriggered)
        {
            int strikes = tier == 3 ? 1 : 3;
            for (int i = 0; i < strikes; i++)
            {
                SpawnStrike(tier == 3 ? 60 : tier == 2 ? 16 : 10,
                    tier == 3 ? 9 : 2, tier == 3);
                yield return new WaitForSeconds(.18f);
            }
        }
        yield return new WaitForSeconds(.2f); activeUltimate = 0; State = ActionState.IDLE;
    }
    private void CancelActions()
    {
        StopAllCoroutines();
        queuedBasic = false;
        activeUltimate = 0; counterWindowUntil = 0;
        if (heldTarget != null)
        {
            CombatActor target = heldTarget; heldTarget = null; target.heldBy = null;
            if (target.Health.Alive) target.Stagger(.15f);
        }
        if (heldBy != null)
        {
            CombatActor holder = heldBy; heldBy = null; holder.heldTarget = null;
            if (holder.State == ActionState.GRAB) holder.State = ActionState.IDLE;
        }
        foreach (AttackHitbox box in boxes) if (box != null) Destroy(box.gameObject);
        boxes.Clear();
    }
    public void RestoreForTutorial()
    {
        CancelActions(); Health.Reset(); State = ActionState.IDLE;
        invulnerableUntil = 0; guardStarted = float.NegativeInfinity; StopPlanarMotion();
    }
    public bool TryGrab()
    {
        if (!CanAct) return false;
        CombatActor nearest = null; float distance = 1.6f;
        foreach (Collider collider in Physics.OverlapSphere(transform.position, distance,
            ~0, QueryTriggerInteraction.Ignore))
        {
            CombatActor target = collider.GetComponentInParent<CombatActor>();
            if (target == null || target == this || target.Team == Team || !target.Health.Alive ||
                target.heldBy != null || Time.time < target.invulnerableUntil) continue;
            Vector3 delta = target.transform.position - transform.position;
            float range = delta.magnitude;
            if (range >= distance || Vector3.Dot(Facing, delta.normalized) < .3f) continue;
            nearest = target; distance = range;
        }
        if (nearest == null) return false;
        StartCoroutine(GrabRoutine(nearest)); return true;
    }
    private IEnumerator GrabRoutine(CombatActor target)
    {
        target.CancelActions(); State = ActionState.GRAB; target.State = ActionState.GRAB;
        heldTarget = target; target.heldBy = this; StopPlanarMotion(); target.StopPlanarMotion();
        float end = Time.time + .2f;
        while (Time.time < end && heldTarget == target)
        {
            // Hold both fighters in place without teleporting through walls.
            target.StopPlanarMotion(); yield return new WaitForFixedUpdate();
        }
        if (heldTarget != target) yield break;
        heldTarget = null; target.heldBy = null;
        if (target.ReceiveHit(new HitRequest(gameObject, 8, Facing, true, 7)))
        {
            OnConfirmedHit(target, target.transform.position); Grabbed?.Invoke();
        }
        yield return new WaitForSeconds(.15f); State = ActionState.IDLE;
    }
    private void StopPlanarMotion()
    {
        Vector3 velocity = GetVelocity(); velocity.x = 0; velocity.z = 0; SetVelocity(velocity);
    }
    private Vector3 GetVelocity()
    {
        return body.linearVelocity;
    }
    private void SetVelocity(Vector3 velocity)
    {
        body.linearVelocity = velocity;
    }
    private void OnDisable() { CancelActions(); }
}
