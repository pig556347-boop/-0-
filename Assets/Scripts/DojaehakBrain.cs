using System.Collections;
using UnityEngine;

public enum DojaehakPattern { OneTwoKick, SideHook, ChaseRoundhouse, HookFlurry, MixedKicks }

// Only the first mid-boss uses this controller. EnemyBrain retains the shared enable/target contract.
public sealed class DojaehakBrain : MonoBehaviour
{
    private EnemyBrain owner;
    private CombatActor actor;
    private Rigidbody body;
    private Coroutine sequence;
    private float readyAt;
    private bool phaseAnnounced,cancelling;
    private int lastPattern=-1,interruptedChains;
    public bool PressureReady => interruptedChains>=2;
    private readonly float[] usedAt={-100,-100,-100,-100,-100};
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
        enemy.BossUltimateEnabled=true;enemy.BossMoveInterrupted+=brain.OnMoveInterrupted;brain.readyAt=Time.time+.8f;return brain;
    }
    public void Tick()
    {
        var target=owner.Target;
        if(!owner.Active||!actor.GameplayEnabled||!actor.Health.Alive||target==null||!target.Health.Alive)
        {CancelPattern();return;}
        if(actor.State==CombatActor.ActionState.HIT)
        {if(Running){interruptedChains=Mathf.Min(2,interruptedChains+1);CancelPattern();readyAt=Time.time+.65f;}return;}
        if(Running||!actor.CanAct)return;
        if(PhaseTwo&&!phaseAnnounced)
        {phaseAnnounced=true;actor.ShowBossCue("압박 강화!",.75f);readyAt=Time.time+.75f;StopMotion();Debug.Log("DOJAEHAK_PHASE_TWO");return;}
        Vector3 delta=Delta();float distance=delta.magnitude;actor.SetFacing(delta);
        if(Time.time<readyAt)
        {Move(distance<1.25f?-delta.normalized*1.25f:Vector3.zero);return;}
        if(owner.Director!=null&&!owner.Director.CanAttack(actor)){StopMotion();return;}
        if(distance>8)
        {Move(delta.normalized*3.2f);return;}
        var selected=ChoosePattern(distance,target.State==CombatActor.ActionState.GUARD,Random.value);
        if(TryPattern(selected))return;
        Move(distance>1.7f?delta.normalized*2.3f:Vector3.zero);
    }
    public DojaehakPattern ChoosePattern(float distance,bool guarding,float roll)
    {
        if(PressureReady&&distance<=2.6f)return DojaehakPattern.SideHook;
        DojaehakPattern preferred;
        if(distance>3.1f)preferred=DojaehakPattern.ChaseRoundhouse;
        else if(guarding&&distance<1.9f&&roll<.65f)preferred=DojaehakPattern.HookFlurry;
        else if(distance>1.9f)preferred=PhaseTwo&&roll<.45f?DojaehakPattern.MixedKicks:DojaehakPattern.SideHook;
        else if(PhaseTwo&&roll<.25f)preferred=DojaehakPattern.HookFlurry;
        else if(roll<.65f)preferred=DojaehakPattern.OneTwoKick;
        else preferred=DojaehakPattern.SideHook;
        if(Available(preferred,distance)&&lastPattern!=(int)preferred)return preferred;
        // Start scanning at a weighted candidate, avoiding a fixed repeating script.
        for(int i=1;i<=5;i++)
        {var candidate=(DojaehakPattern)(((int)preferred+i)%5);if((int)candidate!=lastPattern&&Available(candidate,distance))return candidate;}
        return preferred;
    }
    private bool Available(DojaehakPattern pattern,float distance)
    {
        if(PressureReady&&pattern==DojaehakPattern.SideHook&&distance<=2.6f)return true;
        float cooldown=pattern==DojaehakPattern.ChaseRoundhouse?6.5f:pattern==DojaehakPattern.HookFlurry?5:3.2f;
        if(Time.time-usedAt[(int)pattern]<cooldown)return false;
        if(pattern==DojaehakPattern.ChaseRoundhouse)return distance>=2.5f&&distance<=8;
        if(pattern==DojaehakPattern.OneTwoKick||pattern==DojaehakPattern.HookFlurry)return distance<=1.85f;
        return distance<=2.6f;
    }
    public bool TryPattern(DojaehakPattern pattern)
    {
        if(Running||!owner.Active||!actor.CanAct||owner.Target==null||!owner.Target.Health.Alive||Time.time<readyAt||!Available(pattern,Delta().magnitude))return false;
        if(owner.Director!=null&&!owner.Director.CanAttack(actor))return false;
        Running=true;CurrentPattern=pattern;lastPattern=(int)pattern;usedAt[(int)pattern]=Time.time;PatternsStarted++;
        sequence=StartCoroutine(Perform(pattern));return true;
    }
    private IEnumerator Perform(DojaehakPattern pattern)
    {
        if(pattern==DojaehakPattern.ChaseRoundhouse)
        {
            StopMotion();actor.SetFacing(Delta());actor.ShowBossCue("달려온다!",.4f);
            yield return new WaitForSeconds(.4f);
            Rushing=true;float end=Time.time+.95f;
            while(Valid()&&actor.CanAct&&Delta().magnitude>2.0f&&Time.time<end)
            {actor.SetFacing(Delta());Move(Delta().normalized*4.4f);yield return new WaitForFixedUpdate();}
            Rushing=false;StopMotion();
        }
        string[] chain=pattern==DojaehakPattern.OneTwoKick?new[]{"PunchLeft","PunchRight","MmaKick"}:
            pattern==DojaehakPattern.SideHook?new[]{"SideKick","HookPunch"}:
            pattern==DojaehakPattern.ChaseRoundhouse?new[]{"RoundhouseKick"}:
            pattern==DojaehakPattern.HookFlurry?new[]{"HookPunch","QuadPunch"}:new[]{"Kicking","RoundhouseKick"};
        foreach(string clip in chain)
        {
            if(!Valid()||!actor.CanAct)break;
            var move=DojaehakMoves.Get(clip);
            // A dodge/backstep can remove the follow-up rather than being chased by a snap turn.
            if(Delta().magnitude>move.Reach+.55f)break;
            actor.SetFacing(Delta());StopMotion();
            bool resolve=PressureReady&&move.Armor;
            if(!actor.TryBossMove(move,resolve))break;
            if(resolve)interruptedChains=0;
            while(Valid()&&actor.State==CombatActor.ActionState.ATTACK)yield return null;
            if(!Valid()||!actor.CanAct)break;
        }
        Rushing=false;Running=false;sequence=null;
        if(actor.CanAct)StopMotion();readyAt=Time.time+(PhaseTwo?.90f:1.25f);
    }
    private bool Valid()=>owner.Active&&actor.GameplayEnabled&&actor.Health.Alive&&owner.Target!=null&&owner.Target.Health.Alive&&actor.State!=CombatActor.ActionState.HIT;
    private Vector3 Delta(){Vector3 delta=owner.Target.transform.position-transform.position;delta.y=0;return delta;}
    private void Move(Vector3 planar){Vector3 v=body.linearVelocity;v.x=planar.x;v.z=planar.z;body.linearVelocity=v;}
    private void StopMotion()=>Move(Vector3.zero);
    private void OnMoveInterrupted()
    {
        if(cancelling||!Running)return;
        if(actor.Health.Alive)interruptedChains=Mathf.Min(2,interruptedChains+1);
        CancelPattern();readyAt=Time.time+.65f;
    }
    public void CancelPattern()
    {
        if(sequence!=null)StopCoroutine(sequence);sequence=null;Running=false;Rushing=false;
        if(actor==null)return;cancelling=true;actor.CancelBossMove();cancelling=false;if(actor.CanAct)StopMotion();
    }
    public void ResetForEncounter()
    {
        CancelPattern();phaseAnnounced=false;lastPattern=-1;interruptedChains=0;readyAt=Time.time+.8f;
        for(int i=0;i<usedAt.Length;i++)usedAt[i]=-100;
    }
    private void OnDisable(){if(owner!=null)CancelPattern();}
    private void OnDestroy(){if(actor!=null)actor.BossMoveInterrupted-=OnMoveInterrupted;}
}
