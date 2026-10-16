using System;
using System.Collections;
using System.Collections.Generic;
using SchoolRuleZero;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class CombatActor : MonoBehaviour, IHitReceiver
{
    public enum ActionState { IDLE, ATTACK, HIT, GUARD, DODGE, GRAB, ULTIMATE, DEAD, CROUCH }
    public bool TelegraphEnabled { get; set; }
    public bool BossUltimateEnabled { get; set; }
    public float GrabCounterChance { get; set; }
    public string WarningText { get; private set; } = "";
    public float WarningUntil { get; private set; }
    public BossMove CurrentBossMove { get; private set; }
    public float BossMoveElapsed => CurrentBossMove==null?0:Time.time-bossMoveStarted;
    public int BossMoveStrikeIndex { get; private set; }=-1;
    private float bossMoveStarted;
    public bool BossMoveEarlyArmor { get; private set; }
    public bool HeavyStrike => activeHeavy;
    public bool HeavyUsesKick { get; set; }
    public bool PunchStrike { get; private set; }
    public bool PunchLeft { get; private set; }
    public int PunchCount => punchSequence.Count;
    private readonly PunchSequence punchSequence=new PunchSequence();
    public event Action Punched;
    public float VisualWindup => CurrentBossMove!=null?CurrentBossMove.Windup:attackWindup * (activeHeavy ? 1.8f : 1);
    public float VisualRecovery => CurrentBossMove!=null?CurrentBossMove.Duration-CurrentBossMove.StrikeTimes[CurrentBossMove.StrikeTimes.Length-1]:attackRecovery * (activeHeavy ? 1.6f : 1);
    public int BasicComboHit { get; private set; }
    public int UltimateStrikeIndex { get; private set; } = -1;
    public float UltimateStrikeInterval => activeUltimate == 3 ? .14f : .18f;
    public static int UltimateHitCount(int tier) => tier == 3 ? 6 : 3;
    public bool IsUltimateFinisher => activeUltimate > 0 && UltimateStrikeIndex == UltimateHitCount(activeUltimate)-1;
    private bool crouched;
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
    public event Action AttackPrepared;
    public event Action BossMoveInterrupted;
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
    public void ConfigureStats(float hp, float damage, float windup, float recovery)
    {
        maximumHP = Mathf.Max(1, hp); Health = new CombatHealth(maximumHP);
        basicDamage = damage; heavyDamage = damage * 1.7f;
        attackWindup = windup; attackRecovery = recovery;
    }
    public void ReleaseGuard() { if (State == ActionState.GUARD) State = ActionState.IDLE; }
    public void Configure(int team) { Team = team; Facing = team == 0 ? Vector3.right : Vector3.left; }
    private void Update()
    {
        if (Time.timeScale == 0 || !GameplayEnabled || Health == null || !Health.Alive) return;
        Progress.Tick(Time.time);
        if (State == ActionState.HIT && Time.time >= lockedUntil) State = ActionState.IDLE;
        if (input == null || !input.GameplayEnabled) return;
        if (State == ActionState.ATTACK && (PunchStrike?input.PunchPressed:input.AttackPressed)) queuedBasic = true;
        if (State == ActionState.GUARD && !input.GuardHeld) State = ActionState.IDLE;
        if (input.GuardPressed && (CanAct || State == ActionState.GUARD))
        { guardStarted = Time.time; State = ActionState.GUARD; StopPlanarMotion(); }
        else if (CanAct && input.GuardHeld) { State = ActionState.GUARD; StopPlanarMotion(); }
        if ((CanAct || State == ActionState.GUARD) && input.DodgePressed)
        { State = ActionState.IDLE; TryDodge(input.DodgeDirection); return; }
        if (CanAct && motor != null) Facing = motor.FacingDirection;
        if (CanAct && input.UltimatePressed) { TryUltimate(); return; }
        if (CanAct && input.PunchPressed) { TryPunch(); return; }
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
        StartCoroutine(AttackRoutine(heavy,Team!=0&&(!heavy||!HeavyUsesKick))); return true;
    }
    public bool TryBossMove(BossMove move,bool earlyArmor=false)
    {
        if(!CanAct||Team==0||!BossUltimateEnabled||move==null)return false;
        StartCoroutine(BossMoveRoutine(move,earlyArmor));return true;
    }
    public void CancelBossMove()
    {if(CurrentBossMove==null)return;CancelActions();if(Health.Alive){State=ActionState.IDLE;StopPlanarMotion();}}
    public void ShowBossCue(string text,float duration)
    {if(Team!=0&&Health.Alive)Warn(text,duration);}
    private IEnumerator BossMoveRoutine(BossMove move,bool earlyArmor)
    {
        CurrentBossMove=move;BossMoveEarlyArmor=earlyArmor&&move.Armor;bossMoveStarted=Time.time;BossMoveStrikeIndex=-1;
        State=ActionState.ATTACK;activeHeavy=!move.Punch;PunchStrike=move.Punch;PunchLeft=move.Clip=="PunchLeft";
        StopPlanarMotion();AttackPrepared?.Invoke();Warn(BossMoveEarlyArmor?"버티고 "+move.Warning:move.Warning,move.Windup);
        for(int i=0;i<move.StrikeTimes.Length;i++)
        {
            while(BossMoveElapsed<move.StrikeTimes[i])yield return null;
            BossMoveStrikeIndex=i;
            SpawnStrike(basicDamage*move.DamageRatio,move.Knockback,false,move.Reach);
            // Parry callbacks can synchronously stagger/cancel the source during SpawnStrike.
            if(CurrentBossMove!=move||State!=ActionState.ATTACK)yield break;
        }
        while(BossMoveElapsed<move.Duration)yield return null;
        CurrentBossMove=null;BossMoveEarlyArmor=false;BossMoveStrikeIndex=-1;State=ActionState.IDLE;
    }
    public bool TryPunch()
    {if(!CanAct)return false;StartCoroutine(AttackRoutine(false,true));return true;}
    private IEnumerator AttackRoutine(bool heavy,bool punch)
    {
        State = ActionState.ATTACK; activeHeavy = heavy; PunchStrike=punch; StopPlanarMotion();
        int chain = 0;
        do
        {
            queuedBasic = false;
            BasicComboHit = heavy ? 0 : chain + 1;
            if(punch)PunchLeft=punchSequence.Next(Time.time);
            AttackPrepared?.Invoke();
            if (TelegraphEnabled) Warn(heavy ? "강공격!" : "공격!", attackWindup * (heavy ? 1.8f : 1));
            yield return new WaitForSeconds(attackWindup * (heavy ? 1.8f : 1));
            SpawnStrike(heavy ? heavyDamage : basicDamage + Mathf.Min(chain,2) * 2,
                heavy ? 6 : !punch && chain == 2 ? 4 : 1.2f, false);
            yield return new WaitForSeconds(attackRecovery * (heavy ? 1.6f : 1));
            chain++;
        } while (!heavy && queuedBasic && (punch || chain < 3));
        State = ActionState.IDLE;
    }
    private void Warn(string text, float duration) { WarningText = text; WarningUntil = Time.time + duration; }
    private void SpawnStrike(float damage, float knockback, bool unblockable, float reach = 1.3f)
    {
        WarningText = "";
        boxes.RemoveAll(box => box == null);
        StrikeStarted?.Invoke();
        GameObject box = new GameObject("Attack Hitbox");
        box.transform.position = transform.position + Facing * (.95f + (reach - 1.3f) * .5f);
        box.transform.rotation = Quaternion.LookRotation(Facing);
        AttackHitbox hitbox = box.AddComponent<AttackHitbox>(); boxes.Add(hitbox);
        hitbox.Configure(gameObject, Team, new HitRequest(gameObject, damage, Facing,
            unblockable, knockback), new Vector3(1.2f, 1.5f, reach), .10f, OnConfirmedHit);
    }
    private void OnConfirmedHit(IHitReceiver receiver, Vector3 position)
    {
        CombatActor target = receiver as CombatActor;
        if (target != null)
        {
            Progress.ConfirmHit(Time.time, gain: State == ActionState.ULTIMATE ? 0 : 12);
            HitConfirmed?.Invoke(this, target, position);
            if (State == ActionState.ULTIMATE) UltimateLanded?.Invoke(activeUltimate);
            if (State == ActionState.ATTACK)
            {AttackLanded?.Invoke(activeHeavy);if(PunchStrike)Punched?.Invoke();}
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
        if(Health.Alive&&CurrentBossMove!=null&&(CurrentBossMove.ArmoredAt(BossMoveElapsed)||BossMoveEarlyArmor&&BossMoveElapsed<=CurrentBossMove.StrikeTimes[CurrentBossMove.StrikeTimes.Length-1]+.1f)&&!hit.Unblockable&&hit.Knockback<4&&
            (hit.Source==null||hit.Source.GetComponent<CombatActor>()?.State!=ActionState.ULTIMATE))
        {Damaged?.Invoke(result);return true;}
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
        if (!CanAct || Team != 0 || Progress.UltimateTier == 0) return false;
        int tier = Progress.ConsumeUltimate(); StartCoroutine(UltimateRoutine(tier)); return true;
    }
    private IEnumerator UltimateRoutine(int tier)
    {
        State = ActionState.ULTIMATE; activeUltimate = tier; UltimateStrikeIndex = -1; StopPlanarMotion();
        UltimateStarted?.Invoke(tier);
        if (tier == 2)
        {
            counterTriggered = false; counterWindowUntil = Time.time + .65f;
            yield return new WaitForSeconds(.65f);
        }
        else yield return new WaitForSeconds(tier == 3 ? .4f : .12f);
        if (tier != 2 || counterTriggered)
        {
            int strikes = UltimateHitCount(tier);
            for (int i = 0; i < strikes; i++)
            {
                UltimateStrikeIndex = i;
                bool finisher = i == strikes - 1;
                SpawnStrike(tier == 3 ? 10 : tier == 2 ? 16 : 10,
                    finisher ? (tier == 3 ? 9 : 2) : .25f, tier == 3);
                yield return new WaitForSeconds(UltimateStrikeInterval);
            }
        }
        yield return new WaitForSeconds(.2f); activeUltimate = 0; State = ActionState.IDLE;
    }
    private void CancelActions()
    {
        bool interruptedBossMove=CurrentBossMove!=null;
        StopAllCoroutines();CurrentBossMove=null;BossMoveEarlyArmor=false;BossMoveStrikeIndex=-1;
        if(interruptedBossMove)BossMoveInterrupted?.Invoke();
        WarningText = ""; SetEnemyCrouch(false);
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
        punchSequence.Reset();
        GetComponent<DefeatedBody>()?.Restore();
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
        if (nearest.State == ActionState.GUARD && UnityEngine.Random.value < nearest.GrabCounterChance)
        { Stagger(.45f); return false; }
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
    public bool TryJump()
    {
        if (!CanAct || !Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, 1.15f,
            ~0, QueryTriggerInteraction.Ignore) || hit.collider.GetComponentInParent<CombatActor>() != null) return false;
        Vector3 velocity = GetVelocity(); velocity.y = 4.4f; SetVelocity(velocity);
        return true;
    }
    public bool TryCrouch()
    {
        if (!CanAct || Mathf.Abs(GetVelocity().y) > .1f) return false;
        StartCoroutine(CrouchRoutine()); return true;
    }
    private IEnumerator CrouchRoutine()
    {
        State = ActionState.CROUCH; StopPlanarMotion(); SetEnemyCrouch(true);
        yield return new WaitForSeconds(.4f);
        SetEnemyCrouch(false); State = ActionState.IDLE;
    }
    private void SetEnemyCrouch(bool value)
    {
        if (Team == 0 || value == crouched) return;
        crouched = value;
        CapsuleCollider shape = GetComponent<CapsuleCollider>();
        shape.height = value ? 1.2f : 2; shape.center = value ? Vector3.down * .4f : Vector3.zero;
        PrototypeCharacterView view = GetComponentInChildren<PrototypeCharacterView>();
        if (view != null && view.GetComponent<ImportedPlayerView>() == null)
        { view.transform.localScale = new Vector3(1, value ? .6f : 1, 1);
          view.transform.localPosition = value ? Vector3.down * .4f : Vector3.zero; }
    }
    public bool TryBossUltimate(string title)
    {
        if (!CanAct || Team == 0 || !BossUltimateEnabled) return false;
        StartCoroutine(BossUltimateRoutine(title)); return true;
    }
    private IEnumerator BossUltimateRoutine(string title)
    {
        State = ActionState.ULTIMATE; StopPlanarMotion();
        if (TelegraphEnabled) Warn(title, .8f);
        UltimateStarted?.Invoke(3);
        yield return new WaitForSeconds(.8f);
        SpawnStrike(basicDamage * 3, 8, false, 2.6f);
        Vector3 v = GetVelocity(); v.x = Facing.x * 6; v.z = Facing.z * 6; SetVelocity(v);
        yield return new WaitForSeconds(.2f); StopPlanarMotion();
        yield return new WaitForSeconds(.6f); State = ActionState.IDLE;
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
