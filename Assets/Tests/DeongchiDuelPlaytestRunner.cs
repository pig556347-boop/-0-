using System;
using System.Collections;
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
        Check(FindObjectsByType<Minion1View>(FindObjectsSortMode.None).Length==alive,"campaign ordinary opponents use supplied Minion1 skin");
        Check(player.CanAct,"player controls restored for campaign");
        Debug.Log("DUEL_TEST_PASS checks="+checks);
    }
}
