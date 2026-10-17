using System;
using System.Collections;
using SchoolRuleZero;
using UnityEngine;

public sealed class ShortcutPlaytestRunner : MonoBehaviour
{
    private int checks;
    private void Check(bool value,string label){if(!value)throw new Exception("SHORTCUT_TEST_FAIL "+label);checks++;}
    private EncounterDirector encounter;
    private IEnumerator Key(string key,int stage)
    {
        Debug.Log("SHORTCUT_INPUT_READY key="+key);
        float timeout=Time.unscaledTime+15;
        while(!GameKeys.Down(key=="1"?KeyCode.Alpha1:key=="2"?KeyCode.Alpha2:key=="3"?KeyCode.Alpha3:KeyCode.Alpha4)&&Time.unscaledTime<timeout)yield return null;
        Check(Time.unscaledTime<timeout,"browser key received "+key);
        yield return null;yield return null;
        Check(encounter.Stage==stage&&encounter.Area==(stage==1?1:SchoolCampaign.AreaCount(stage)),"correct real destination "+stage);
    }
    private IEnumerator Start()
    {
        var audio=gameObject.AddComponent<GameAudioChannels>();
        var boot=gameObject.AddComponent<PrototypeBootstrap>();boot.enabled=false;boot.BeginGame();
        encounter=GetComponent<EncounterDirector>();var lesson=GetComponent<TutorialDirector>();
        CombatActor player=null;foreach(var actor in FindObjectsByType<CombatActor>(FindObjectsSortMode.None))if(actor.Team==0)player=actor;
        Check(TestCombatShortcuts.Enabled&&GetComponent<TestCombatShortcuts>()!=null,"temporary navigation installed");
        Check(lesson.Progress.Step==TutorialStep.Opening,"skip during actual sleeping intro");
        Check(!encounter.JumpForTesting(6),"unsupported stage rejected");
        yield return Key("1",1);
        Check(!lesson.enabled&&lesson.Progress.Step==TutorialStep.Complete,"lessons and coroutine ended");
        Check(GameObject.Find("Opening desk")==null&&GameObject.Find("Opening motion chair")==null,"intro props removed");
        Check(!player.GetComponentInChildren<PrototypeCharacterView>().CinematicPose,"sleep pose released");
        Check(player.CanAct&&player.GetComponent<CombatInput>().GameplayEnabled&&player.GetComponent<PlayerController>().GameplayEnabled,"controls unlocked");
        Check(!GetComponent<DeongchiDuel>().enabled,"duel cannot overwrite destination later");
        player.Progress.FillGaugeForPractice();Check(player.TryUltimate(),"navigation during ultimate");
        yield return Key("2",5);
        Check(player.State==CombatActor.ActionState.IDLE&&player.Health.Current==player.Health.Maximum&&!player.Health.Infinite,"boss entry cancels ultimate and restores finite health");
        yield return new WaitForSecondsRealtime(.40f);Check(audio.CurrentMusic=="BossMusic","dedicated Dojaehak music selected");
        Debug.Log("BOSS_MUSIC_READY");
        var old=FindFirstObjectByType<DojaehakBrain>();Check(old!=null,"real authored Dojaehak spawned");
        old.GetComponent<EnemyBrain>().enabled=false;Check(old.GetComponent<CombatActor>().TryBossMove(DojaehakMoves.Get("SideKick")),"boss attack active before navigation");
        yield return Key("3",10);Check(old==null,"attacking old boss removed");
        yield return new WaitForSecondsRealtime(.40f);Check(audio.CurrentMusic=="BattleMusic","leaving Dojaehak restores regular battle music");
        CombatActor boss=null;int count=0;foreach(var actor in FindObjectsByType<CombatActor>(FindObjectsSortMode.None))if(actor.Team==1){boss=actor;count++;}
        Check(count==1&&boss.name=="한승빈","only requested 1-10 boss remains");
        boss.GetComponent<EnemyBrain>().Active=false;boss.ReceiveHit(new HitRequest(player.gameObject,99999,Vector3.right,true,0));
        yield return null;yield return null;
        yield return Key("4",11);Check(boss==null,"previous defeated body removed");
        count=0;foreach(var actor in FindObjectsByType<CombatActor>(FindObjectsSortMode.None))if(actor.Team==1){boss=actor;count++;}
        Check(count==1&&boss.name=="황빈","only requested final boss remains");
        boss.GetComponent<EnemyBrain>().Active=false;
        var impact=GetComponent<HitImpactManager>();impact.SetPaused(true);
        Debug.Log("SHORTCUT_INPUT_READY key=2");yield return new WaitForSecondsRealtime(.8f);
        Check(encounter.Stage==11&&impact.Paused,"paused shortcut ignored");impact.SetPaused(false);
        player.ReceiveHit(new HitRequest(null,99999,Vector3.right,true,0));
        yield return Key("1",1);Check(player.Health.Alive&&player.CanAct,"navigation recovers player after defeat");
        yield return new WaitForSeconds(.4f);Check(encounter.Stage==1&&!lesson.enabled,"no delayed intro or duel overwrite");
        Debug.Log("SHORTCUT_TEST_PASS checks="+checks);
    }
}
