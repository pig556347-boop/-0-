using System;
using System.Collections;
using System.Collections.Generic;
using SchoolRuleZero;
using UnityEngine;

// Real hitboxes and CombatInput drive this fixture. It is installed only in HUDTest.
public sealed class BalancePlaytestRunner : MonoBehaviour
{
    private CombatActor player,target,extraA,extraB;
    private CombatInput input;
    private int checks,strikes,hits;
    private readonly List<float> preparedTimes=new List<float>();
    private readonly List<int> preparedCombo=new List<int>();
    private void Check(bool value,string label)
    {
        if(!value){Debug.LogError("BALANCE_TEST_FAIL "+label);throw new Exception(label);}
        checks++;
    }
    private CombatActor Actor(string name,int team,Vector3 point,bool visual)
    {
        var root=new GameObject(name);root.transform.position=point;
        if(team==0)input=root.AddComponent<CombatInput>();
        CombatActor actor=root.AddComponent<CombatActor>();actor.Configure(team);actor.ConfigureStats(500,12,.52f,.52f);
        Rigidbody body=actor.GetComponent<Rigidbody>();body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeAll;
        if(visual)
        {
            var model=new GameObject("Visual");model.transform.SetParent(root.transform,false);
            var view=model.AddComponent<PrototypeCharacterView>();view.Build(actor);
            if(team!=0)view.UseDeongchiModel();
            root.AddComponent<CombatVisualFeedback>();root.AddComponent<CombatAudioEmitter>();
            actor.HitConfirmed+=CombatEffects.Instance.OnHit;
        }
        return actor;
    }
    private void Place(CombatActor actor,Vector3 point)
    {actor.transform.position=point;actor.GetComponent<Rigidbody>().position=point;}
    private void ResetPair()
    {
        input.GameplayEnabled=false;
        player.RestoreForTutorial();target.RestoreForTutorial();extraA.RestoreForTutorial();extraB.RestoreForTutorial();
        player.Progress.FillGaugeForPractice();player.Progress.ConsumeUltimate();player.Progress.BreakCombo();
        target.Progress.FillGaugeForPractice();target.Progress.ConsumeUltimate();target.Progress.BreakCombo();
        Place(player,new Vector3(0,1,0));Place(target,new Vector3(1.3f,1,0));
        Place(extraA,new Vector3(8,1,0));Place(extraB,new Vector3(10,1,0));
        player.SetFacing(Vector3.right);target.SetFacing(Vector3.left);
        preparedTimes.Clear();preparedCombo.Clear();strikes=hits=0;Physics.SyncTransforms();
    }
    private IEnumerator Ready(float timeout=4)
    {
        float until=Time.time+timeout;
        while(!player.CanStartOffense&&Time.time<until)yield return null;
        Check(player.CanStartOffense,"action and shared recovery finish");
    }
    private void RejectOtherOffense(string label)
    {
        player.Progress.FillGaugeForPractice();
        Check(!player.TryAttack(false)&&!player.TryAttack(true)&&!player.TryPunch()&&!player.TryUltimate(),label);
    }
    private IEnumerator Preview(string kind)
    {
        Time.timeScale=0;yield return new WaitForEndOfFrame();Debug.Log("BALANCE_PREVIEW_READY kind="+kind);
        float until=Time.realtimeSinceStartup+10;
        while(!GameKeys.Down(KeyCode.Return)&&Time.realtimeSinceStartup<until)yield return null;
        Check(Time.realtimeSinceStartup<until,"browser captures "+kind+" feedback");Time.timeScale=1;
    }
    private IEnumerator Start()
    {
        // Keep sub-frame startup assertions meaningful on software-rendered CI.
        float previousMaximumDelta=Time.maximumDeltaTime;Time.maximumDeltaTime=.05f;
        var camera=new GameObject("Balance camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=new Vector3(3.6f,4.1f,-7);camera.transform.LookAt(new Vector3(.6f,1.2f,0));
        camera.orthographic=true;camera.orthographicSize=2.1f;camera.backgroundColor=new Color(.12f,.15f,.19f);
        camera.gameObject.AddComponent<AudioListener>();
        var light=new GameObject("Balance light").AddComponent<Light>();light.type=LightType.Directional;
        light.transform.rotation=Quaternion.Euler(45,-35,0);light.intensity=1.1f;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Classroom floor";
        floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(24,1,12);
        gameObject.AddComponent<CombatEffects>();gameObject.AddComponent<GameAudioChannels>();
        player=Actor("Balance player",0,Vector3.up,true);target=Actor("Balance target",1,new Vector3(1.3f,1,0),true);
        extraA=Actor("Second target",1,new Vector3(8,1,0),false);extraB=Actor("Third target",1,new Vector3(10,1,0),false);
        player.StrikeStarted+=()=>strikes++;
        player.HitConfirmed+=(source,victim,point)=>hits++;
        player.AttackPrepared+=()=>{preparedTimes.Add(Time.time);preparedCombo.Add(player.BasicComboHit);};
        ResetPair();yield return null;

        Check(player.TryAttack(false),"J starts");RejectOtherOffense("J windup cannot be cancelled by J K I Q");
        yield return new WaitForSeconds(.12f);Check(target.Health.Current==500,"J startup cannot hit early");
        yield return new WaitForSeconds(.15f);Check(target.Health.Current==490&&hits==1,"J first kick deals ten exactly once");
        RejectOtherOffense("J active recovery cannot be cancelled by another attack");
        while(!player.CanAct)yield return null;
        RejectOtherOffense("J shared end gap prevents switching keys to bypass recovery");
        yield return Ready();Check(player.TryPunch(),"I follows completed J recovery");
        yield return new WaitForSeconds(.20f);Check(target.Health.Current==483,"I first punch deals seven");
        while(!player.CanAct)yield return null;RejectOtherOffense("I end gap covers every offensive key");yield return Ready();

        ResetPair();Check(player.TryAttack(true),"K starts");float heavyStarted=Time.time;
        yield return new WaitForSeconds(.32f);Check(target.Health.Current==500,"K gives a readable startup");
        RejectOtherOffense("K startup cannot be cancelled into a faster attack");
        yield return new WaitForSeconds(.15f);Check(target.Health.Current==470&&strikes==1,"K deals thirty with one strike");
        yield return Preview("heavy");
        while(!player.CanAct)yield return null;
        RejectOtherOffense("K shared end gap blocks J I Q as well");yield return Ready();
        Check(Time.time-heavyStarted<1.85f&&!player.TryAttack(true),"K stays unavailable after animation recovery");
        Check(player.TryPunch(),"K reuse does not lock all other recovered actions");yield return Ready();
        while(player.HeavyRemaining>0)yield return null;
        Check(player.TryAttack(true),"K becomes usable after reuse deadline");yield return Ready();

        ResetPair();Place(extraA,new Vector3(1.3f,1,.35f));Place(extraB,new Vector3(1.3f,1,-.35f));Physics.SyncTransforms();
        Check(player.TryAttack(false),"multi target J starts");yield return new WaitForSeconds(.28f);
        Check(hits==3&&target.Health.Current==490&&extraA.Health.Current==490&&extraB.Health.Current==490,"one real hitbox damages all three once");
        Check(Mathf.Abs(player.Progress.Gauge-8)<.01f,"three targets cannot multiply meter gain");yield return Ready();

        ResetPair();Place(extraA,new Vector3(1.3f,1,.35f));Place(extraB,new Vector3(1.3f,1,-.35f));Physics.SyncTransforms();
        int simultaneousParries=0;Action countParry=()=>simultaneousParries++;
        target.Parried+=countParry;extraA.Parried+=countParry;extraB.Parried+=countParry;
        Check(player.TryAttack(false),"crowd parry cancellation starts");yield return new WaitForSeconds(.12f);
        target.TryGuard(true);extraA.TryGuard(true);extraB.TryGuard(true);yield return new WaitForSeconds(.15f);
        Check(simultaneousParries==1&&player.State==CombatActor.ActionState.HIT,"first parry cancels current overlap scan before other targets");
        Check(target.Health.Current==500&&extraA.Health.Current==500&&extraB.Health.Current==500,"cancelled hitbox leaves no same-frame crowd damage");
        target.Parried-=countParry;extraA.Parried-=countParry;extraB.Parried-=countParry;

        for(int tier=1;tier<=3;tier++)
        {
            ResetPair();player.Progress.ConfirmHit(Time.time,gain:tier==1?33:tier==2?66:100);
            Check(player.TryUltimate(),"Q starts tier="+tier);
            if(tier==2){yield return new WaitForSeconds(.2f);player.ReceiveHit(new HitRequest(target.gameObject,8,Vector3.left));}
            if(tier==3)
            {
                yield return new WaitForSeconds(.15f);float health=player.Health.Current;
                player.ReceiveHit(new HitRequest(target.gameObject,5,Vector3.left,false,1));
                Check(player.State==CombatActor.ActionState.ULTIMATE&&player.Health.Current==health-5,"tier three resists light stagger while still taking damage");
            }
            while(!player.CanAct)yield return null;
            Check(strikes==CombatActor.UltimateHitCount(tier)&&hits==strikes,"Q contacts are not duplicated tier="+tier);
            Check(target.Health.Current==500-(tier==3?64:tier==2?44:30),"Q total damage tier="+tier);
            RejectOtherOffense("Q cannot cancel its ending recovery tier="+tier);yield return Ready();
            Check(!player.TryUltimate(),"refilling meter does not bypass Q reuse tier="+tier);
            Check(player.Progress.Gauge==100,"blocked test attempts do not spend prefilled meter");
        }
        ResetPair();player.Progress.ConfirmHit(Time.time,gain:66);Check(player.TryUltimate(),"counter Q miss starts");
        yield return Ready();Check(strikes==0&&target.Health.Current==500&&player.Progress.Gauge==0,"untriggered counter Q spends meter without free damage");

        ResetPair();Check(player.TryDodge(Vector3.back),"dodge starts");
        Check(!player.ReceiveHit(new HitRequest(target.gameObject,10,Vector3.left))&&player.Health.Current==500,"dodge avoids hit during short invulnerability");
        yield return new WaitForSeconds(.19f);
        Check(player.ReceiveHit(new HitRequest(target.gameObject,10,Vector3.left))&&player.Health.Current==490,"dodge animation is not invulnerable throughout");
        yield return new WaitForSeconds(.30f);Check(!player.TryDodge(Vector3.back),"ending dodge early by taking damage does not reset its reuse");
        while(player.DodgeRemaining>0)yield return null;Check(player.TryDodge(Vector3.back),"dodge returns after reuse");
        yield return new WaitForSeconds(.32f);

        ResetPair();Check(player.TryGuard(false),"held guard starts without fresh parry");
        int breaks=0;player.GuardBroken+=()=>breaks++;
        for(int i=0;i<4;i++)player.ReceiveHit(new HitRequest(target.gameObject,10,Vector3.left,false,0));
        Check(breaks==1&&player.State==CombatActor.ActionState.HIT&&player.Health.Current==496,"sustained frontal guard chips health and breaks after pressure");
        Check(!player.TryGuard(true)&&!player.TryAttack(false),"guard break creates an actual punish window");
        yield return Preview("guard");yield return new WaitForSeconds(.65f);
        Check(player.CanAct,"guard break does not permanently lock actions");
        float integrity=player.GuardIntegrity;yield return new WaitForSeconds(1.10f);
        Check(player.GuardIntegrity>integrity&&player.GuardIntegrity<=100,"guard integrity regenerates after pressure ends");

        ResetPair();Check(player.TryGuard(true),"fresh parry starts");
        Check(!player.ReceiveHit(new HitRequest(target.gameObject,10,Vector3.left,false,0))&&player.Health.Current==500&&target.State==CombatActor.ActionState.HIT,"timed parry negates hit and staggers attacker");
        yield return Preview("parry");player.ReleaseGuard();yield return new WaitForSeconds(.15f);
        Check(player.TryGuard(true),"rapid repress still allows ordinary guard");
        player.ReceiveHit(new HitRequest(target.gameObject,10,Vector3.left,false,0));
        Check(player.Health.Current==499,"repeated L cannot refresh parry window during reuse");
        player.ReleaseGuard();yield return new WaitForSeconds(.30f);Check(player.TryGuard(true),"parry becomes available again");
        Check(!player.ReceiveHit(new HitRequest(target.gameObject,10,Vector3.left,false,0))&&player.Health.Current==499,"new valid press can parry again");
        player.ReleaseGuard();yield return new WaitForSeconds(.20f);
        Check(target.State==CombatActor.ActionState.HIT,"successful parry leaves enough attacker stun to punish");

        ResetPair();Check(player.TryAttack(true),"pause case K starts");yield return new WaitForSeconds(.1f);
        float elapsed=player.AttackElapsed,reuse=player.HeavyRemaining;Time.timeScale=0;
        yield return new WaitForSecondsRealtime(.20f);
        Check(strikes==0&&Mathf.Abs(player.AttackElapsed-elapsed)<.001f&&Mathf.Abs(player.HeavyRemaining-reuse)<.001f,"pause freezes startup and reuse together");
        Time.timeScale=1;player.ReceiveHit(new HitRequest(target.gameObject,1000,Vector3.left,true,0));
        yield return new WaitForSeconds(.55f);Check(strikes==0&&target.Health.Current==500&&!player.TryAttack(false),"death cancels delayed K damage and rejects further offense");
        player.RestoreForTutorial();Check(player.CanStartOffense&&player.HeavyRemaining==0&&player.DodgeRemaining==0&&player.GuardIntegrity==100,"retry restores every action gate and defense resource");

        ResetPair();input.GameplayEnabled=true;
        Debug.Log("BALANCE_INPUT_READY key=k duration=3.2");float until=Time.time+3.2f;
        while(Time.time<until)yield return null;
        input.GameplayEnabled=false;Debug.Log("BALANCE_INPUT_DONE key=k");
        yield return Ready();Check(preparedTimes.Count==2&&strikes==2&&target.Health.Current==440,"real K spam permits only two thirty-damage attacks in 3.2 seconds");
        Check(preparedTimes[1]-preparedTimes[0]>=1.84f,"real K presses cannot bypass reuse");

        ResetPair();input.GameplayEnabled=true;
        Debug.Log("BALANCE_INPUT_READY key=j duration=3.4");until=Time.time+3.4f;
        while(Time.time<until)yield return null;
        input.GameplayEnabled=false;Debug.Log("BALANCE_INPUT_DONE key=j");yield return Ready();
        Check(strikes>=4&&strikes<=6,"real J spam has a bounded strike rate");
        bool third=false,reset=false;float expectedDamage=0;
        for(int i=0;i<preparedCombo.Count;i++)
        {
            int combo=preparedCombo[i];Check(combo>=1&&combo<=3,"J queued chain cannot exceed three");
            expectedDamage+=combo==3?16:combo==2?12:10;
            if(combo==3)third=true;
            if(i>0&&preparedCombo[i-1]==3)
            {reset=true;Check(combo==1&&preparedTimes[i]-preparedTimes[i-1]>=.87f,"third kick must finish its recovery and shared gap before a new chain");}
        }
        Check(third&&reset&&hits==strikes&&target.Health.Current==500-expectedDamage,"real input reaches three kicks, resets and deals correct sequence damage");
        Time.maximumDeltaTime=previousMaximumDelta;
        Debug.Log("BALANCE_TEST_PASS checks="+checks+" kStrikes=2 jStrikes="+strikes);
    }
}
