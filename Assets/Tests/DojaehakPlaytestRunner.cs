using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class DojaehakPlaytestRunner : MonoBehaviour
{
    private int checks;
    private CombatActor player,boss;
    private EnemyBrain owner;
    private DojaehakBrain brain;
    private Minion1View view;
    private CombatInput input;
    private Rigidbody playerBody,bossBody;
    private readonly List<string> prepared=new List<string>();
    private int strikes;
    private void Check(bool value,string label){if(!value)throw new Exception("DOJAEHAK_TEST_FAIL "+label);checks++;}
    private void ResetPair(float distance=1.3f)
    {
        owner.Active=false;owner.enabled=false;brain.CancelPattern();boss.RestoreForTutorial();player.RestoreForTutorial();brain.ResetForEncounter();
        boss.transform.position=new Vector3(0,1.05f,0);player.transform.position=new Vector3(distance,1.05f,0);
        bossBody.useGravity=playerBody.useGravity=false;bossBody.constraints=playerBody.constraints=RigidbodyConstraints.FreezeAll;
        bossBody.linearVelocity=playerBody.linearVelocity=Vector3.zero;boss.SetFacing(Vector3.right);player.SetFacing(Vector3.left);
        input.GameplayEnabled=false;prepared.Clear();strikes=0;Physics.SyncTransforms();
    }
    private Bounds SkinBounds(SkinnedMeshRenderer skin)
    {
        var mesh=skin.sharedMesh;var bind=mesh.bindposes;var vertices=mesh.vertices;var weights=mesh.boneWeights;
        var matrices=new Matrix4x4[bind.Length];for(int i=0;i<bind.Length;i++)matrices[i]=skin.bones[i].localToWorldMatrix*bind[i];
        Bounds bounds=default;bool valid=true;
        for(int i=0;i<vertices.Length;i++)
        {
            var w=weights[i];Vector3 v=vertices[i];
            Vector3 p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+
                matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
            valid&=!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&(p-boss.transform.position).magnitude<4;
            if(i==0)bounds=new Bounds(p,Vector3.zero);else bounds.Encapsulate(p);
        }
        Check(valid,"finite deformed boss skin");Check(bounds.size.y>.6f&&bounds.size.y<2.5f,"boss human proportions "+bounds);return bounds;
    }
    private IEnumerator Start()
    {
        var camera=new GameObject("Boss camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=new Vector3(5,4,-7);camera.transform.LookAt(new Vector3(.5f,1,0));camera.orthographic=true;camera.orthographicSize=3.3f;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Classroom floor";floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(28,1,18);
        var root=new GameObject("Boss review player");root.transform.position=new Vector3(1.3f,1.05f,0);
        root.AddComponent<CapsuleCollider>().height=2;playerBody=root.AddComponent<Rigidbody>();input=root.AddComponent<CombatInput>();input.GameplayEnabled=false;
        player=root.AddComponent<CombatActor>();player.Configure(0);player.ConfigureStats(600,12,.12f,.28f);
        var visual=new GameObject("Player visual");visual.transform.SetParent(root.transform,false);visual.AddComponent<PrototypeCharacterView>().Build(player);
        boss=PrototypeBootstrap.CreateOpponent("도재학",new Vector3(0,1.05f,0),player);boss.ConfigureStats(240,10,.24f,.48f);
        owner=boss.GetComponent<EnemyBrain>();owner.Active=false;owner.Difficulty=5;owner.IsBoss=true;
        bossBody=boss.GetComponent<Rigidbody>();brain=DojaehakBrain.Install(boss);view=boss.GetComponentInChildren<Minion1View>();
        boss.AttackPrepared+=()=>{if(boss.CurrentBossMove!=null)prepared.Add(boss.CurrentBossMove.Clip);};boss.StrikeStarted+=()=>strikes++;
        ResetPair();Check(view.ModelKind=="Dojaehak"&&view.UsesSuppliedWalk,"new boss skin and native walk installed");
        var skin=view.GetComponentInChildren<SkinnedMeshRenderer>();Check(skin.sharedMesh.triangles.Length==150000,"all source boss triangles retained");Check(skin.sharedMaterial.mainTexture!=null,"boss supplied texture loaded");
        foreach(var w in skin.sharedMesh.boneWeights)Check(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.005f,"normalized boss weights");
        foreach(string clip in new[]{"PunchLeft","PunchRight","HookPunch","QuadPunch","Kicking","MmaKick","SideKick","RoundhouseKick","Walking","RunForward"})
        {
            for(int frame=0;frame<=10;frame++){view.Preview(clip,frame/10f);SkinBounds(skin);}
            bool movement=clip=="Walking"||clip=="RunForward";
            if(movement)view.Preview(clip,.25f);
            else
            {
                var move=DojaehakMoves.Get(clip);view.Preview(clip,move.NativeContacts[0]/(clip=="PunchLeft"?62/60f:clip=="PunchRight"?51/60f:clip=="HookPunch"||clip=="QuadPunch"?130/60f:clip=="Kicking"?129/60f:clip=="MmaKick"?122/60f:clip=="SideKick"?120/60f:150/60f));
                for(int i=0;i<move.StrikeTimes.Length;i++)Check(Mathf.Abs(move.SampleTime(move.StrikeTimes[i])-move.NativeContacts[i])<.0001f,"authored contact matches gameplay "+clip+" "+i);
            }
            yield return new WaitForEndOfFrame();Debug.Log("DOJAEHAK_PREVIEW_READY clip="+clip);float timeout=Time.time+12;
            while(!GameKeys.Down(KeyCode.Return)&&Time.time<timeout)yield return null;Check(Time.time<timeout,"captured boss preview "+clip);
            while(GameKeys.Held(KeyCode.Return))yield return null;yield return null;
        }
        view.ResumeRuntime();
        // One physical hit per quad contact, not one per physics scan, and normal guard works.
        ResetPair();var quad=DojaehakMoves.Get("QuadPunch");float hp=player.Health.Current;
        Check(boss.TryBossMove(quad),"quad starts");yield return new WaitForSeconds(quad.Duration+.1f);
        Check(strikes==4,"exactly four quad contact windows");Check(Mathf.Abs(hp-player.Health.Current-18)<.05f,"four real hits without duplicate overlap damage");Check(boss.CanAct,"quad recovery returns control");
        ResetPair();hp=player.Health.Current;Check(player.TryGuard(false),"sustained guard starts");Check(boss.TryBossMove(quad),"guarded quad starts");yield return new WaitForSeconds(quad.Duration+.1f);
        Check(Mathf.Abs(hp-player.Health.Current-4.5f)<.05f,"all quad contacts are blockable with bounded chip");
        ResetPair();Check(boss.TryBossMove(DojaehakMoves.Get("SideKick")),"dodge test kick starts");yield return new WaitForSeconds(.4f);
        hp=player.Health.Current;playerBody.constraints=RigidbodyConstraints.FreezeRotation;Check(player.TryDodge(Vector3.back),"real dodge starts");yield return new WaitForSeconds(.5f);
        Check(Mathf.Approximately(hp,player.Health.Current),"dodge avoids boss contact");Check(player.transform.position.z<-.4f,"dodge actually moves player");
        // Late light hits deal damage through limited commitment; strong hits still interrupt.
        ResetPair();Check(boss.TryBossMove(quad),"armored quad starts");yield return new WaitForSeconds(.45f);float bossHP=boss.Health.Current;
        Check(boss.ReceiveHit(new HitRequest(player.gameObject,8,Vector3.left,false,1.2f)),"light hit damages committed boss");
        Check(boss.State==CombatActor.ActionState.ATTACK&&boss.Health.Current==bossHP-8,"limited armor is not invulnerability");
        Check(boss.ReceiveHit(new HitRequest(player.gameObject,12,Vector3.left,false,6)),"heavy hit interrupts boss");
        Check(boss.State==CombatActor.ActionState.HIT&&boss.CurrentBossMove==null,"heavy clears committed move");int stopped=strikes;yield return new WaitForSeconds(1.8f);Check(strikes==stopped,"interruption leaves no delayed contacts");
        ResetPair();Check(boss.TryBossMove(quad),"mid-quad interrupt starts");yield return new WaitForSeconds(.73f);
        Check(strikes==1,"first quad contact happened before interruption");boss.ReceiveHit(new HitRequest(player.gameObject,12,Vector3.left,false,6));
        yield return new WaitForSeconds(1.9f);Check(strikes==1,"mid-quad interruption removes remaining three contacts");
        ResetPair();Check(boss.TryBossMove(quad),"ultimate interrupt starts");yield return new WaitForSeconds(.45f);
        player.Progress.ConsumeUltimate();player.Progress.ConfirmHit(Time.time,gain:33);Check(player.TryUltimate(),"tier-one ultimate starts against armor");
        yield return new WaitForSeconds(.2f);Check(boss.State==CombatActor.ActionState.HIT&&boss.CurrentBossMove==null,"ultimate also breaks limited commitment");
        yield return new WaitForSeconds(.8f);
        // Source-facing is locked during a strike: no turn-in-place tracking after a side dodge.
        ResetPair();Check(boss.TryBossMove(DojaehakMoves.Get("SideKick")),"facing lock move starts");yield return new WaitForSeconds(.3f);
        player.transform.position=new Vector3(-1.3f,1.05f,0);Physics.SyncTransforms();hp=player.Health.Current;yield return new WaitForSeconds(.5f);
        Check(Mathf.Approximately(hp,player.Health.Current)&&boss.Facing.x>.9f,"moving behind avoids locked strike");
        // A real timing defense stops the attack source, including the rest of a chain.
        ResetPair();yield return new WaitForSeconds(.85f);owner.Active=true;Check(brain.TryPattern(DojaehakPattern.OneTwoKick),"parry chain starts");
        yield return new WaitForSeconds(.37f);Check(player.TryGuard(true),"timed parry starts");hp=player.Health.Current;
        yield return new WaitForSeconds(.12f);Check(boss.State==CombatActor.ActionState.HIT&&boss.CurrentBossMove==null,"parry staggers boss and clears move");
        owner.Active=false;yield return new WaitForSeconds(.8f);Check(prepared.Count==1&&Mathf.Approximately(hp,player.Health.Current),"parry cancels follow-up without damage");
        var expected=new Dictionary<DojaehakPattern,string[]>{
            {DojaehakPattern.OneTwoKick,new[]{"PunchLeft","PunchRight","MmaKick"}},
            {DojaehakPattern.SideHook,new[]{"SideKick","HookPunch"}},
            {DojaehakPattern.ChaseRoundhouse,new[]{"RoundhouseKick"}},
            {DojaehakPattern.HookFlurry,new[]{"HookPunch","QuadPunch"}},
            {DojaehakPattern.MixedKicks,new[]{"Kicking","RoundhouseKick"}}};
        foreach(var pair in expected)
        {
            ResetPair(pair.Key==DojaehakPattern.ChaseRoundhouse?5.5f:1.3f);yield return new WaitForSeconds(.85f);
            bossBody.constraints=RigidbodyConstraints.FreezeRotation;bossBody.useGravity=true;owner.Active=true;
            Check(brain.TryPattern(pair.Key),"pattern starts "+pair.Key);float timeout=Time.time+10;bool runSeen=false;
            while(brain.Running&&Time.time<timeout){runSeen|=view.CurrentMotion=="RunForward";yield return null;}
            owner.Active=false;Check(Time.time<timeout,"pattern finishes "+pair.Key);
            Check(prepared.Count==pair.Value.Length,"complete authored chain "+pair.Key+" got="+string.Join(",",prepared));
            for(int i=0;i<pair.Value.Length;i++)Check(prepared[i]==pair.Value[i],"chain order "+pair.Key+" "+i);
            Check(brain.ReadyAt>Time.time,"real punish window after pattern "+pair.Key);
            if(pair.Key==DojaehakPattern.ChaseRoundhouse)Check(runSeen&&boss.transform.position.x>1,"native run moves real physics root");
        }
        ResetPair();
        foreach(var interrupted in new[]{DojaehakPattern.OneTwoKick,DojaehakPattern.MixedKicks})
        {
            yield return new WaitForSeconds(.85f);owner.Active=true;Check(brain.TryPattern(interrupted),"pressure adaptation chain starts");
            yield return new WaitForSeconds(.1f);boss.ReceiveHit(new HitRequest(player.gameObject,4,Vector3.left,false,1.2f));
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();owner.Active=false;
        }
        Check(brain.PressureReady&&brain.ChoosePattern(1.3f,false,.1f)==DojaehakPattern.SideHook,"two interruptions select a readable resolve kick");
        yield return new WaitForSeconds(.85f);owner.Active=true;Check(brain.TryPattern(DojaehakPattern.SideHook),"resolve counter starts");
        Check(boss.BossMoveEarlyArmor&&boss.WarningText.Contains("버티고"),"resolve has explicit tell and early limited armor");
        boss.ReceiveHit(new HitRequest(player.gameObject,4,Vector3.left,false,1.2f));Check(boss.State==CombatActor.ActionState.ATTACK,"light mashing cannot indefinitely restart boss");
        boss.ReceiveHit(new HitRequest(player.gameObject,12,Vector3.left,false,6));Check(boss.State==CombatActor.ActionState.HIT,"resolve still loses to heavy counter");owner.Active=false;
        // A retreat removes optional follow-ups rather than producing inescapable homing.
        ResetPair();yield return new WaitForSeconds(.85f);owner.Active=true;Check(brain.TryPattern(DojaehakPattern.OneTwoKick),"retreat chain starts");
        yield return new WaitForSeconds(.2f);player.transform.position=new Vector3(6,1.05f,0);Physics.SyncTransforms();yield return new WaitForSeconds(.65f);owner.Active=false;
        Check(prepared.Count==1&&!brain.Running,"retreat removes out-of-range follow-ups");
        ResetPair();Check(!brain.PhaseTwo,"phase one starts above half health");boss.ReceiveHit(new HitRequest(player.gameObject,121,Vector3.left,true,0));Check(brain.PhaseTwo,"half health changes pressure phase");
        Check(brain.ChoosePattern(5,false,.2f)==DojaehakPattern.ChaseRoundhouse,"distance selects chase");
        Check(brain.ChoosePattern(1.3f,true,.2f)==DojaehakPattern.HookFlurry,"guarding favors readable pressure");
        ResetPair();Check(boss.TryBossMove(quad),"pause move starts");yield return new WaitForSeconds(.2f);float elapsed=boss.BossMoveElapsed;Time.timeScale=0;
        yield return new WaitForSecondsRealtime(.3f);Check(Mathf.Abs(boss.BossMoveElapsed-elapsed)<.001f&&strikes==0,"pause freezes timeline and hits");Time.timeScale=1;boss.Stagger(.3f);
        // Exercise the real browser K key against an armored tell.
        ResetPair();Check(boss.TryBossMove(DojaehakMoves.Get("SideKick")),"browser heavy interrupt starts");input.GameplayEnabled=true;
        Debug.Log("DOJAEHAK_INPUT_READY");float inputTimeout=Time.time+12;
        while(boss.State==CombatActor.ActionState.ATTACK&&Time.time<inputTimeout)yield return null;
        input.GameplayEnabled=false;Check(Time.time<inputTimeout&&boss.State==CombatActor.ActionState.HIT,"browser K interrupts boss windup");yield return new WaitForSeconds(.5f);
        ResetPair(4.5f);bossBody.useGravity=true;bossBody.constraints=RigidbodyConstraints.FreezeRotation;yield return new WaitForSeconds(.85f);int startedBefore=brain.PatternsStarted;owner.enabled=true;owner.Active=true;
        hp=player.Health.Current;float aiTimeout=Time.time+14;
        while(player.Health.Current>=hp&&Time.time<aiTimeout)yield return null;
        Check(player.Health.Current<hp&&brain.PatternsStarted>startedBefore,"autonomous boss closes and lands real damage");
        owner.Active=false;brain.CancelPattern();yield return new WaitForSeconds(.3f);
        Check(boss.ReceiveHit(new HitRequest(player.gameObject,9999,Vector3.left,true,0)),"boss remains defeatable");yield return new WaitForSeconds(.8f);
        var dead=boss.GetComponent<DefeatedBody>();Check(dead.Settled&&dead.TryBounds(out Bounds bounds)&&Mathf.Abs(bounds.min.y-.015f)<.025f,"new boss corpse grounds correctly");
        int afterDeath=strikes;yield return new WaitForSeconds(.5f);Check(strikes==afterDeath&&!brain.Running,"death cancels every pending attack");
        Debug.Log("DOJAEHAK_TEST_PASS checks="+checks);
    }
}
