using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SchoolRuleZero;
using UnityEngine;

// Test scene only: real production bootstrap, AI and hit handling; lesson signals are accelerated.
public sealed class DeongchiDuelPlaytestRunner : MonoBehaviour
{
    private int checks;
    private void Check(bool value,string label)
    {if(!value)throw new Exception("DUEL_TEST_FAIL "+label);checks++;}
    private IEnumerator Start()
    {
        var bootstrap=gameObject.AddComponent<PrototypeBootstrap>();bootstrap.enabled=false;bootstrap.BeginGame();
        var tutorial=GetComponent<TutorialDirector>();var duel=GetComponent<DeongchiDuel>();
        CombatActor player=null,enemy=null;
        foreach(var actor in FindObjectsByType<CombatActor>(FindObjectsSortMode.None))
            if(actor.Team==0)player=actor;else enemy=actor;
        float timeout=Time.time+30;
        while(tutorial.Progress.Step==TutorialStep.Opening&&Time.time<timeout)yield return null;
        Check(tutorial.Progress.Step==TutorialStep.Move,"opening finished");
        Check(enemy.Health.Infinite,"training opponent protected");
        Check(duel.Phase==DeongchiDuel.DuelPhase.Waiting,"duel waits for lessons");
        foreach(TutorialSignal signal in Enum.GetValues(typeof(TutorialSignal)))
            Check(tutorial.Progress.Accept(signal),"lesson "+signal);
        yield return null;yield return null;
        Check(duel.Phase==DeongchiDuel.DuelPhase.Countdown,"completion enters duel countdown");
        Check(!tutorial.enabled,"lesson coach stops");
        Check(!enemy.Health.Infinite&&!player.Health.Infinite,"real health enabled");
        Check(enemy.Health.Current==160,"opponent starts with full 160 HP");
        var walk=enemy.GetComponentInChildren<DeongchiWalkView>();
        Check(walk!=null,"Walking driver installed on Deongchi");
        Check(enemy.GetComponentInChildren<AuthoredPlayerView>()==null,"no player attack driver attached");
        yield return new WaitForSeconds(1.7f);
        Check(duel.Phase==DeongchiDuel.DuelPhase.Fighting,"AI duel starts");
        Vector3 start=enemy.transform.position;
        yield return new WaitForSeconds(.7f);
        Check(walk.WalkWeight>.9f&&walk.CurrentMotion=="Walking","AI movement plays Walking only");
        Check(Vector3.Distance(start,enemy.transform.position)>.3f,"AI approaches player");
        float sample=walk.SampleTime;yield return new WaitForSeconds(.15f);
        Check(Mathf.Abs(sample-walk.SampleTime)>.01f,"walk pose advances");
        Debug.Log("DUEL_WALK_PREVIEW_READY");
        yield return new WaitForSeconds(.5f);
        float initialHP=player.Health.Current;timeout=Time.time+14;
        while(player.Health.Current>=initialHP&&Time.time<timeout)yield return null;
        Check(player.Health.Current<initialHP,"AI attacks deal real damage");
        enemy.GetComponent<EnemyBrain>().Active=false;
        enemy.RestoreForTutorial();enemy.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
        yield return new WaitForSeconds(.4f);
        Check(walk.WalkWeight==0&&walk.CurrentMotion=="Rest","walking fades out while stationary");
        player.ReceiveHit(new HitRequest(null,9999,Vector3.right,false,1));yield return null;yield return null;
        Check(duel.Phase==DeongchiDuel.DuelPhase.Defeat,"player death enters defeat");
        Debug.Log("DUEL_RETRY_READY");timeout=Time.time+20;
        while(duel.Phase==DeongchiDuel.DuelPhase.Defeat&&Time.time<timeout)yield return null;
        Check(duel.Phase==DeongchiDuel.DuelPhase.Countdown,"R restarts duel");
        Check(player.Health.Alive&&enemy.Health.Current==160,"retry restores both actors");
        yield return new WaitForSeconds(1.8f);
        Check(duel.Phase==DeongchiDuel.DuelPhase.Fighting,"retry starts fighting");
        enemy.GetComponent<EnemyBrain>().Active=false;
        Check(enemy.ReceiveHit(new HitRequest(player.gameObject,20,Vector3.right,false,1)),"enemy accepts hit");
        Check(enemy.Health.Current==140,"enemy health decreases");
        yield return new WaitForSeconds(1);
        Check(enemy.ReceiveHit(new HitRequest(player.gameObject,9999,Vector3.right,false,1)),"finishing hit accepted");
        yield return null;yield return null;
        Check(duel.Phase==DeongchiDuel.DuelPhase.Victory,"defeating Deongchi wins duel");
        Debug.Log("DUEL_VICTORY_READY");timeout=Time.time+20;
        while(duel.Phase==DeongchiDuel.DuelPhase.Victory&&Time.time<timeout)yield return null;
        yield return null;yield return null;
        Check(duel.ReadyForCampaign,"Enter completes duel");
        Check(enemy==null||!enemy.gameObject.activeSelf,"training opponent removed after victory");
        int alive=0;foreach(var actor in FindObjectsByType<CombatActor>(FindObjectsSortMode.None))
            if(actor.Team==1&&actor.Health.Alive)alive++;
        Check(alive>0,"campaign opponents spawn after duel");
        Check(FindObjectsByType<Minion1View>(FindObjectsSortMode.None).Length==alive,"campaign ordinary opponents use supplied student skins");
        bool firstSkin=false,secondSkin=false,thirdSkin=false;foreach(var v in FindObjectsByType<Minion1View>(FindObjectsSortMode.None))
        {firstSkin|=v.ModelKind=="Minion1";secondSkin|=v.ModelKind=="Minion2";thirdSkin|=v.ModelKind=="Minion3";}
        Check(firstSkin&&secondSkin&&thirdSkin,"all three supplied student variants spawn in real campaign");
        Check(player.CanAct,"player controls restored for campaign");
        Debug.Log("CAMPAIGN_MINION_PREVIEW_READY");yield return new WaitForSeconds(.5f);
        var encounter=GetComponent<EncounterDirector>();player.Health.Infinite=true;
        foreach(int stage in new[]{2,5,6,10,11})
        {
            // Jump only in this fixture to real production late-stage spawning/cleanup.
            typeof(EncounterDirector).GetProperty("Stage").SetValue(encounter,stage);
            typeof(EncounterDirector).GetProperty("Area").SetValue(encounter,SchoolCampaign.IsBoss(stage)?SchoolCampaign.AreaCount(stage):1);
            typeof(EncounterDirector).GetMethod("StartArea",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(encounter,new object[]{true});
            if(stage==2)
            {
                bool fourthSeen=false;foreach(var skin in FindObjectsByType<Minion1View>(FindObjectsSortMode.None))fourthSeen|=skin.ModelKind=="Minion4";
                Check(fourthSeen,"fourth student appears by stage 1-2 despite three-enemy limit");
            }
            if(stage==6)
            {
                var kinds=new HashSet<string>();
                foreach(var skin in FindObjectsByType<Minion1View>(FindObjectsSortMode.None))kinds.Add(skin.ModelKind);
                Check(kinds.SetEquals(new[]{"Minion1","Minion2","Minion3","Minion4"}),"all four supplied student skins coexist in real campaign");
            }
            var defeated=new List<CombatActor>();int expected=SchoolCampaign.EnemyCount(stage,encounter.Area);
            float until=Time.time+5;
            while(defeated.Count<expected&&Time.time<until)
            {
                foreach(var foe in FindObjectsByType<CombatActor>(FindObjectsSortMode.None))
                {
                    if(foe.Team!=1||!foe.Health.Alive)continue;
                    Check(foe.GetComponent<DefeatedBody>().LifetimeAfterSettled>0,"late-stage corpse cleanup configured stage="+stage);
                    foe.GetComponent<EnemyBrain>().Active=false;
                    Check(foe.ReceiveHit(new HitRequest(player.gameObject,99999,Vector3.right,true,0)),"late-stage lethal hit stage="+stage);
                    defeated.Add(foe);
                }
                yield return null;yield return null;
            }
            Check(defeated.Count==expected,"all late-stage wave enemies defeated stage="+stage);
            yield return new WaitForSeconds(.65f);
            int shown=0;foreach(var foe in defeated)if(foe!=null){shown++;Check(foe.GetComponent<DefeatedBody>().Settled,"death settles before cleanup stage="+stage);}
            Check(shown>0,"death shown before cleanup stage="+stage);
            if(stage==5)
            {
                GetComponent<HitImpactManager>().SetPaused(true);yield return new WaitForSecondsRealtime(.4f);
                foreach(var foe in defeated)Check(foe!=null,"paused corpse lifetime suspended");
                GetComponent<HitImpactManager>().SetPaused(false);
            }
            yield return new WaitForSeconds(1.5f);
            foreach(var foe in defeated)Check(foe==null,"corpse removed without changing area stage="+stage);
            Check(player.Health.Alive,"living player survives cleanup stage="+stage);
            Debug.Log("CAMPAIGN_CLEANUP_PASS stage="+stage+" count="+expected);
            if(stage==5)
            {
                Debug.Log("CLEANUP_ADVANCE_READY");until=Time.time+15;
                while(encounter.Stage==5&&Time.time<until)yield return null;
                Check(encounter.Stage==6,"1-5 victory still advances to 1-6 after corpse removal");
            }
        }
        Debug.Log("CAMPAIGN_CLEANUP_PREVIEW_READY");yield return new WaitForSeconds(.5f);
        Debug.Log("DUEL_TEST_PASS checks="+checks);
    }
}
