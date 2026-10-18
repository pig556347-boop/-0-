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
    private float nextDefense,reactionAt,lastObservedAttack=-1;
    private bool reactionChecked;
    public const float MinimumReactionDelay=.22f;
    private void Awake() { actor = GetComponent<CombatActor>(); body = GetComponent<Rigidbody>(); ultimateReady = Time.time + 10; }
    private void FixedUpdate()
    {
        if(BossDriver!=null){BossDriver.Tick();return;}
        if (Target == null || !Target.Health.Alive || !Active || !actor.GameplayEnabled)
        { if (actor.CanAct) StopMotion(); return; }
        if(Director!=null&&Difficulty<=5&&!IsBoss){TickSchoolStudent();return;}
        if (actor.State == CombatActor.ActionState.GUARD)
        { if (Time.time >= guardUntil) actor.ReleaseGuard(); else return; }
        if (!actor.CanAct) return;
        Vector3 delta = Target.transform.position - transform.position; delta.y = 0;
        actor.SetFacing(delta);
        Vector3 direction = delta.magnitude > 1.32f ? delta.normalized * MoveSpeed : Vector3.zero;
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
        if (delta.magnitude > 1.42f) return;
        bool attacked = versatile && Target.State == CombatActor.ActionState.GUARD &&
            Random.value < (IsBoss ? .4f : .15f) && actor.TryGrab();
        if (!attacked) attacked = actor.TryAttack(Difficulty >= 5 && Random.value < .3f);
        if (attacked) nextAttack = Time.time + AttackInterval;
    }
    // First chapter students share attack roles instead of converging into a single pile.
    private void TickSchoolStudent()
    {
        ObserveTargetAttack();
        if(actor.State==CombatActor.ActionState.GUARD)
        {if(Time.time<guardUntil)return;actor.ReleaseGuard();}
        if(!actor.CanAct)return;
        Vector3 delta=Target.transform.position-transform.position;delta.y=0;
        float distance=delta.magnitude;Vector3 toward=distance>.01f?delta/distance:actor.Facing;
        actor.SetFacing(toward);
        bool hasSlot=Director.CanAttack(actor);
        Vector3 tangent=Vector3.Cross(Vector3.up,toward)*((GetInstanceID()&1)==0?1:-1);
        float desired=hasSlot?1.22f:2.65f;
        float radial=distance>desired+(hasSlot?.10f:.18f)?MoveSpeed:distance<desired-.35f?-MoveSpeed*.55f:0;
        Vector3 movement=toward*radial+tangent*(hasSlot?.24f:.65f)+Director.Separation(actor)*1.3f;
        if(distance<1.38f&&hasSlot&&Time.time>=nextAttack)movement=Director.Separation(actor)*.5f;
        Move(Vector3.ClampMagnitude(movement,MoveSpeed));
        if(Time.time<nextThink)return;
        nextThink=Time.time+ThinkInterval;
        if(TryStudentDefense(distance))return;
        if(!hasSlot||Time.time<nextAttack||distance>1.42f)return;
        // Recovery is visible before it is punished, and it never bypasses this student's own attack cooldown.
        bool punish=Difficulty>=3&&Target.IsRecovering&&Target.ActionRecoveryRemaining>.25f;
        bool heavy=Difficulty>=3&&Random.value<(punish?.40f:.12f);
        if(actor.TryAttack(heavy))nextAttack=Time.time+AttackInterval+Random.Range(-.12f,.28f);
    }
    private void ObserveTargetAttack()
    {
        if(Target.State!=CombatActor.ActionState.ATTACK){lastObservedAttack=-1;reactionChecked=false;return;}
        float elapsed=Target.AttackElapsed;
        if(lastObservedAttack<0||elapsed+.04f<lastObservedAttack)
        {reactionAt=Time.time+MinimumReactionDelay+Random.Range(.03f,.16f);reactionChecked=false;}
        lastObservedAttack=elapsed;
    }
    private bool TryStudentDefense(float distance)
    {
        if(Difficulty<2||reactionChecked||lastObservedAttack<0||Time.time<reactionAt||Time.time<nextDefense||distance>2.25f)return false;
        reactionChecked=true;
        float roll=Random.value;
        float guardChance=Difficulty>=4?.20f:Difficulty==3?.16f:.12f;
        if(roll<guardChance)
        {
            if(!actor.TryGuard(false))return false;
            guardUntil=Time.time+Random.Range(.35f,.52f);nextDefense=Time.time+2.8f;return true;
        }
        if(Difficulty>=3&&roll<guardChance+.10f&&Target.HeavyStrike)
        {
            Vector3 away=(transform.position-Target.transform.position);away.y=0;
            Vector3 side=Vector3.Cross(Vector3.up,away.normalized)*((GetInstanceID()&1)==0?1:-1);
            if(!actor.TryDodge((side+away.normalized*.35f).normalized))return false;
            nextDefense=Time.time+3.6f;return true;
        }
        return false;
    }
    public void DelayFirstAction(float seconds)
    {nextThink=nextAttack=Time.time+Mathf.Max(0,seconds);nextDefense=nextAttack;}
    private void Move(Vector3 planar)
    {Vector3 velocity=body.linearVelocity;velocity.x=planar.x;velocity.z=planar.z;body.linearVelocity=velocity;}
    private void StopMotion() { Vector3 v = body.linearVelocity; v.x = v.z = 0; body.linearVelocity = v; }
}
