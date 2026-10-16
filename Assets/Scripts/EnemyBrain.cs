using UnityEngine;
[RequireComponent(typeof(CombatActor))]
public sealed class EnemyBrain : MonoBehaviour
{
    public CombatActor Target { get; set; }
    public bool Active { get; set; } = true;
    public float ThinkInterval = .8f, AttackInterval = 1.8f, MoveSpeed = 2;
    public bool IsBoss;
    public int Difficulty = 1;
    public EncounterDirector Director;
    public DojaehakBrain BossDriver;
    private CombatActor actor;
    private Rigidbody body;
    private float nextAttack, nextThink, guardUntil, ultimateReady;
    private void Awake() { actor = GetComponent<CombatActor>(); body = GetComponent<Rigidbody>(); ultimateReady = Time.time + 10; }
    private void FixedUpdate()
    {
        if(BossDriver!=null){BossDriver.Tick();return;}
        if (Target == null || !Target.Health.Alive || !Active || !actor.GameplayEnabled)
        { if (actor.CanAct) StopMotion(); return; }
        if (actor.State == CombatActor.ActionState.GUARD)
        { if (Time.time >= guardUntil) actor.ReleaseGuard(); else return; }
        if (!actor.CanAct) return;
        Vector3 delta = Target.transform.position - transform.position; delta.y = 0;
        actor.SetFacing(delta);
        Vector3 direction = delta.magnitude > 1.45f ? delta.normalized * MoveSpeed : Vector3.zero;
        if (Difficulty == 11 && IsBoss && delta.magnitude < 1.2f && Time.time < nextAttack)
            direction = -delta.normalized * MoveSpeed;
        Vector3 v = body.linearVelocity; v.x = direction.x; v.z = direction.z; body.linearVelocity = v;
        if (Time.time < nextThink) return;
        nextThink = Time.time + ThinkInterval;
        bool versatile = Difficulty >= 5 && Difficulty != 10;
        if (versatile && delta.magnitude < 2.3f && Target.State == CombatActor.ActionState.ATTACK)
        {
            float choice = Random.value;
            if (choice < (IsBoss ? .3f : .12f)) { actor.TryGuard(IsBoss && Difficulty == 11); guardUntil = Time.time + .45f; return; }
            if (choice < (IsBoss ? .48f : .2f)) { actor.TryDodge(-delta.normalized); return; }
            if (choice < .55f && actor.TryJump()) return;
            if (choice < .62f && actor.TryCrouch()) return;
        }
        if (Time.time < nextAttack || Director != null && !Director.CanAttack(actor)) return;
        if (IsBoss && Time.time >= ultimateReady && delta.magnitude < 3)
        {
            if (actor.TryBossUltimate(Difficulty == 5 ? "주먹작렬" : "K.O 펀치"))
            { ultimateReady = Time.time + 14; nextAttack = Time.time + 2; return; }
        }
        if (delta.magnitude > 1.65f) return;
        bool attacked = versatile && Target.State == CombatActor.ActionState.GUARD &&
            Random.value < (IsBoss ? .4f : .15f) && actor.TryGrab();
        if (!attacked) attacked = actor.TryAttack(Difficulty >= 5 && Random.value < .3f);
        if (attacked) nextAttack = Time.time + AttackInterval;
    }
    private void StopMotion() { Vector3 v = body.linearVelocity; v.x = v.z = 0; body.linearVelocity = v; }
}
