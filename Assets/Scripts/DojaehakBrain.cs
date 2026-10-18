using System.Collections;
using UnityEngine;

public enum DojaehakPattern { OneTwoKick, SideHook, ChaseSideKick, HookOneTwo }

// Only the first mid-boss uses this controller. Later bosses keep their existing controller.
public sealed class DojaehakBrain : MonoBehaviour
{
    public const float MinimumReactionDelay=.24f;
    private EnemyBrain owner;
    private CombatActor actor;
    private Rigidbody body;
    private Coroutine sequence;
    private float readyAt,nextThink,guardUntil,nextDefense,reactionAt,lastObservedAttack=-1;
    private bool phaseAnnounced,cancelling,reactionChecked;
    private int lastPattern=-1,interruptedChains;
    public bool PressureReady => interruptedChains>=2;
    private readonly float[] usedAt={-100,-100,-100,-100};
    public bool Running { get; private set; }
    public bool Rushing { get; private set; }
    public bool PhaseTwo => actor!=null&&actor.Health.Current<=actor.Health.Maximum*.5f;
    public DojaehakPattern CurrentPattern { get; private set; }
    public int PatternsStarted { get; private set; }
    public float ReadyAt => readyAt;
    public static DojaehakBrain Install(CombatActor enemy)
    {
        var current=enemy.GetComponent<DojaehakBrain>();if(current!=null)return current;
        enemy.GetComponentInChildren<PrototypeCharacterView>().UseMinion1Model("Dojaehak");
        var brain=enemy.gameObject.AddComponent<DojaehakBrain>();brain.actor=enemy;brain.owner=enemy.GetComponent<EnemyBrain>();
        brain.body=enemy.GetComponent<Rigidbody>();brain.owner.BossDriver=brain;
        enemy.BossUltimateEnabled=true;enemy.BossMoveInterrupted+=brain.OnMoveInterrupted;
        brain.readyAt=Time.time+1;brain.nextDefense=Time.time+1;return brain;
    }
    public void Tick()
    {
        var target=owner.Target;
        if(!owner.Active||!actor.GameplayEnabled||!actor.Health.Alive||target==null||!target.Health.Alive)
        {CancelPattern();return;}
        ObserveAttack(target);
        if(actor.State==CombatActor.ActionState.GUARD)
        {if(Time.time<guardUntil)return;actor.ReleaseGuard();}
        if(actor.State==CombatActor.ActionState.HIT)
        {if(Running){interruptedChains=Mathf.Min(2,interruptedChains+1);CancelPattern();readyAt=Time.time+.85f;}return;}
        if(Running||!actor.CanAct)return;
        if(PhaseTwo&&!phaseAnnounced)
        {phaseAnnounced=true;actor.ShowBossCue("압박 강화!",.85f);readyAt=Time.time+.85f;StopMotion();Debug.Log("DOJAEHAK_PHASE_TWO");return;}
        Vector3 delta=Delta();float distance=delta.magnitude;actor.SetFacing(delta);
        if(Time.time<readyAt)
        {Reposition(delta,distance);return;}
        if(Time.time<nextThink)return;
        nextThink=Time.time+(PhaseTwo?.14f:.18f);
        if(TryDefense(distance))return;
        if(owner.Director!=null&&!owner.Director.CanAttack(actor)){Reposition(delta,distance);return;}
        var selected=ChoosePattern(distance,target.State==CombatActor.ActionState.GUARD,Random.value);
        if(TryPattern(selected))return;
        if(distance>2.05f)Move(delta.normalized*(distance>6.2f?2.92f:2.27f));
        else Reposition(delta,distance);
    }
    private void ObserveAttack(CombatActor target)
    {
        if(target.State!=CombatActor.ActionState.ATTACK){lastObservedAttack=-1;reactionChecked=false;return;}
        float elapsed=target.AttackElapsed;
        if(lastObservedAttack<0||elapsed+.04f<lastObservedAttack)
        {reactionAt=Time.time+MinimumReactionDelay+Random.Range(0,.10f);reactionChecked=false;}
        lastObservedAttack=elapsed;
    }
    private bool TryDefense(float distance)
    {
        if(reactionChecked||Time.time<reactionAt||Time.time<nextDefense||lastObservedAttack<0||distance>2.45f||PressureReady)return false;
        reactionChecked=true;
        // Decide once, after observing the attack. No fresh-press parry or input/frame reading.
        float roll=Random.value;
        if(roll<.19f)
        {
            if(!actor.TryGuard(false))return false;
            guardUntil=Time.time+.38f;nextDefense=Time.time+2.8f;readyAt=Mathf.Max(readyAt,guardUntil+.25f);return true;
        }
        if(roll<.36f&&owner.Target.HeavyStrike)
        {
            Vector3 away=-Delta().normalized;
            Vector3 side=Vector3.Cross(Vector3.up,away)*((GetInstanceID()&1)==0?1:-1);
            if(!actor.TryDodge((away*.35f+side).normalized))return false;
            nextDefense=Time.time+3.2f;readyAt=Mathf.Max(readyAt,Time.time+.6f);return true;
        }
        return false;
    }
    private void Reposition(Vector3 delta,float distance)
    {
        Vector3 side=Vector3.Cross(Vector3.up,delta.normalized)*((GetInstanceID()&1)==0?1:-1);
        Vector3 velocity=distance<1.25f?-delta.normalized*1.1f:distance>3?delta.normalized*1.57f:side*.52f;
        Move(velocity);
    }
    public DojaehakPattern ChoosePattern(float distance,bool guarding,float roll)
    {
        if(PressureReady&&distance<=2.4f)return DojaehakPattern.SideHook;
        DojaehakPattern preferred;
        if(distance>3.05f)preferred=DojaehakPattern.ChaseSideKick;
        else if(guarding&&distance<1.8f&&roll<.6f)preferred=DojaehakPattern.HookOneTwo;
        else if(distance>1.8f)preferred=DojaehakPattern.SideHook;
        else if(PhaseTwo&&roll<.35f)preferred=DojaehakPattern.HookOneTwo;
        else if(roll<.72f)preferred=DojaehakPattern.OneTwoKick;
        else preferred=DojaehakPattern.SideHook;
        if(Available(preferred,distance)&&lastPattern!=(int)preferred)return preferred;
        for(int i=1;i<=4;i++)
        {var candidate=(DojaehakPattern)(((int)preferred+i)%4);if((int)candidate!=lastPattern&&Available(candidate,distance))return candidate;}
        return preferred;
    }
    private bool Available(DojaehakPattern pattern,float distance)
    {
        if(PressureReady&&pattern==DojaehakPattern.SideHook&&distance<=2.4f)return true;
        float cooldown=pattern==DojaehakPattern.ChaseSideKick?6:pattern==DojaehakPattern.HookOneTwo?5:3.4f;
        if(Time.time-usedAt[(int)pattern]<cooldown)return false;
        if(pattern==DojaehakPattern.ChaseSideKick)return distance>=2.5f&&distance<=6.2f;
        if(pattern==DojaehakPattern.OneTwoKick||pattern==DojaehakPattern.HookOneTwo)return distance<=1.8f;
        return distance<=2.4f;
    }
    public bool TryPattern(DojaehakPattern pattern)
    {
        if((int)pattern<0||(int)pattern>=usedAt.Length||Running||!owner.Active||!actor.CanAct||owner.Target==null||!owner.Target.Health.Alive||Time.time<readyAt||!Available(pattern,Delta().magnitude))return false;
        if(owner.Director!=null&&!owner.Director.CanAttack(actor))return false;
        Running=true;CurrentPattern=pattern;lastPattern=(int)pattern;usedAt[(int)pattern]=Time.time;PatternsStarted++;
        sequence=StartCoroutine(Perform(pattern));return true;
    }
    private IEnumerator Perform(DojaehakPattern pattern)
    {
        if(pattern==DojaehakPattern.ChaseSideKick)
        {
            StopMotion();actor.SetFacing(Delta());actor.ShowBossCue("돌진 후 옆차기!",.45f);
            yield return new WaitForSeconds(.45f);
            if(!Valid()){Running=false;sequence=null;yield break;}
            Vector3 rushDirection=Delta().normalized;
            Rushing=true;float end=Time.time+1.05f;
            while(Valid()&&actor.CanAct&&Delta().magnitude>1.82f&&Time.time<end)
            {actor.SetFacing(rushDirection);Move(rushDirection*4.1f);yield return new WaitForFixedUpdate();}
            Rushing=false;StopMotion();
        }
        string[] chain=pattern==DojaehakPattern.OneTwoKick?new[]{"PunchLeft","PunchRight","SideKick"}:
            pattern==DojaehakPattern.SideHook?new[]{"SideKick","HookPunch"}:
            pattern==DojaehakPattern.ChaseSideKick?new[]{"SideKick"}:new[]{"HookPunch","PunchLeft","PunchRight"};
        foreach(string clip in chain)
        {
            if(!Valid()||!actor.CanAct)break;
            var move=DojaehakMoves.Get(clip);
            // Committed attacks do not turn; backing out of range removes later hits.
            if(Delta().magnitude>move.Reach+.4f)break;
            actor.SetFacing(Delta());StopMotion();
            bool resolve=PressureReady&&move.Armor;
            if(!actor.TryBossMove(move,resolve))break;
            if(resolve)interruptedChains=0;
            while(Valid()&&actor.State==CombatActor.ActionState.ATTACK)yield return null;
            if(!Valid()||!actor.CanAct)break;
        }
        Rushing=false;Running=false;sequence=null;
        if(actor.CanAct)StopMotion();readyAt=Time.time+(PhaseTwo?.90f:1.20f);
    }
    private bool Valid()=>owner.Active&&actor.GameplayEnabled&&actor.Health.Alive&&owner.Target!=null&&owner.Target.Health.Alive&&actor.State!=CombatActor.ActionState.HIT;
    private Vector3 Delta(){Vector3 delta=owner.Target.transform.position-transform.position;delta.y=0;return delta;}
    private void Move(Vector3 planar){Vector3 v=body.linearVelocity;v.x=planar.x;v.z=planar.z;body.linearVelocity=v;}
    private void StopMotion()=>Move(Vector3.zero);
    private void OnMoveInterrupted()
    {
        if(cancelling||!Running)return;
        if(actor.Health.Alive)interruptedChains=Mathf.Min(2,interruptedChains+1);
        CancelPattern();readyAt=Time.time+.85f;
    }
    public void CancelPattern()
    {
        if(sequence!=null)StopCoroutine(sequence);sequence=null;Running=false;Rushing=false;
        if(actor==null)return;cancelling=true;actor.CancelBossMove();cancelling=false;if(actor.CanAct)StopMotion();
    }
    public void ResetForEncounter()
    {
        CancelPattern();phaseAnnounced=false;lastPattern=-1;interruptedChains=0;readyAt=Time.time+1;
        nextThink=0;nextDefense=Time.time+1;reactionAt=0;lastObservedAttack=-1;reactionChecked=false;
        for(int i=0;i<usedAt.Length;i++)usedAt[i]=-100;
    }
    private void OnDisable(){if(owner!=null)CancelPattern();}
    private void OnDestroy(){if(actor!=null)actor.BossMoveInterrupted-=OnMoveInterrupted;}
}
