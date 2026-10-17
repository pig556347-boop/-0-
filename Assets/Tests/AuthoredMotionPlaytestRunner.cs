using System;
using System.Collections;
using SchoolRuleZero;
using UnityEngine;

// Separate fixture: every supplied candidate is sampled, then production state wiring is exercised.
public sealed class AuthoredMotionPlaytestRunner : MonoBehaviour
{
    private int checks;
    private void Check(bool value,string message)
    {
        if (!value) { Debug.LogError("AUTHORED_TEST_FAIL "+message); throw new Exception(message); }
        checks++;
    }
    private Vector3[] Skin(SkinnedMeshRenderer render)
    {
        Mesh mesh=render.sharedMesh;BoneWeight[] weights=mesh.boneWeights;
        Matrix4x4[] matrices=new Matrix4x4[mesh.bindposes.Length];
        for(int i=0;i<matrices.Length;i++)matrices[i]=render.bones[i].localToWorldMatrix*mesh.bindposes[i];
        Vector3[] vertices=mesh.vertices,result=new Vector3[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            BoneWeight w=weights[i];Vector3 v=vertices[i];
            result[i]=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+
                matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+
                matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+
                matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
        }
        return result;
    }
    private Bounds BoundsOf(SkinnedMeshRenderer render)
    { Vector3[] vs=Skin(render);Bounds b=new Bounds(vs[0],Vector3.zero);foreach(Vector3 v in vs)b.Encapsulate(v);return b; }
    private void Bounded(SkinnedMeshRenderer render,Vector3 origin,string label)
    {
        bool good=true;
        foreach(Vector3 v in Skin(render))
            good &= !float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&
                !float.IsInfinity(v.y)&&!float.IsInfinity(v.z)&&(v-origin).magnitude<3.2f;
        if(!good)
        {
            Bounds b=BoundsOf(render);Transform hip=null;
            foreach(Transform bone in render.bones)if(bone.name.EndsWith("Hips",StringComparison.Ordinal))hip=bone;
            label+=" bounds="+b+" renderScale="+render.transform.lossyScale+" hip="+(hip==null?"missing":hip.position.ToString())+
                " hipLocal="+(hip==null?"missing":hip.localPosition.ToString())+" hipScale="+(hip==null?"missing":hip.lossyScale.ToString());
        }
        Check(good,label+" finite skin without escaping vertices");
    }
    private CombatActor CreateActor(string name,int team,Vector3 position)
    {
        GameObject go=new GameObject(name);go.transform.position=position;
        CombatActor actor=go.AddComponent<CombatActor>();actor.Configure(team);actor.ConfigureStats(120,12,.12f,.28f);
        Rigidbody body=go.GetComponent<Rigidbody>();body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeAll;
        return actor;
    }
    private IEnumerator Start()
    {
        int review=Application.absoluteURL.Contains("authoredReview=2")?2:1;
        // Pose assertions require observing short reaction/contact intervals even
        // when the CI software renderer misses its requested 30/60 Hz schedule.
        float previousMaximumDelta=Time.maximumDeltaTime;Time.maximumDeltaTime=.05f;
        Application.targetFrameRate=review==1?60:30;
        CombatActor actor=CreateActor("Authored 권민준",0,Vector3.zero);
        GameObject visual=new GameObject("Authored test visual");visual.transform.SetParent(actor.transform,false);
        PrototypeCharacterView character=visual.AddComponent<PrototypeCharacterView>();character.Build(actor);
        AuthoredPlayerView authored=visual.GetComponent<AuthoredPlayerView>();
        Check(authored!=null&&authored.IsReady,"all 23 authored clips available");
        SkinnedMeshRenderer render=authored.ModelRoot.GetComponentInChildren<SkinnedMeshRenderer>();
        Check(render.sharedMesh.isReadable&&render.sharedMesh.vertexCount<25000,"existing web character skin preserved");
        Check(render.sharedMaterial.mainTexture!=null,"original player texture preserved");
        character.CinematicPose=true;
        Vector3 origin=actor.transform.position;Quaternion physicalRotation=actor.transform.rotation;
        CapsuleCollider collider=actor.GetComponent<CapsuleCollider>();float colliderHeight=collider.height;
        string preview=Application.absoluteURL.Contains("authoredPose=")?
            Application.absoluteURL.Substring(Application.absoluteURL.IndexOf("authoredPose=",StringComparison.Ordinal)+13).Split('&')[0]:null;
        if(preview!=null)
        {
            float phase=preview=="RoundhouseKick"?.38f:preview=="MmaKick1"?.39f:preview=="MmaKick2"?.33f:preview=="Stunned"?.95f:.35f;
            authored.Preview(preview,phase);yield return new WaitForEndOfFrame();
            Debug.Log("AUTHORED_PREVIEW_READY "+preview);yield break;
        }
        if(Application.absoluteURL.Contains("flurryPose="))
        {
            int phase=int.Parse(Application.absoluteURL.Split(new[]{"flurryPose="},StringSplitOptions.None)[1].Split('&')[0]);
            character.CinematicPose=false;actor.Progress.ConfirmHit(Time.time,gain:100);actor.TryUltimate();
            while(actor.UltimateStrikeIndex<phase)yield return null;
            yield return new WaitForEndOfFrame();
            Time.timeScale=0;Debug.Log("FLURRY_PREVIEW_READY index="+phase+" clip="+authored.CurrentMotion);yield break;
        }
        foreach(string name in authored.ClipNames)
        {
            for(int phase=0;phase<=10;phase++)
            {
                authored.Preview(name,phase/10f);Bounded(render,origin,name+" phase "+phase);
                Check(actor.transform.position==origin&&actor.transform.rotation==physicalRotation&&collider.height==colliderHeight,
                    name+" does not move actor or change collider");
            }
            Debug.Log("AUTHORED_CANDIDATE_PASS review="+review+" name="+name+" samples=11");
            yield return null;
        }
        authored.Preview("FightingIdle",0);
        Bounds standing=BoundsOf(render);
        Check(standing.size.y>1.5f&&standing.size.y<2.1f,"authored fighting stance normal character scale");
        ImportedPlayerView disabledLegacy=visual.GetComponent<ImportedPlayerView>();
        Transform legacyFoot=null;
        foreach(Transform bone in disabledLegacy.GetComponentsInChildren<Transform>())
            if(bone.name=="LeftFoot" && bone.GetComponentInParent<SkinnedMeshRenderer>()==null) { legacyFoot=bone; break; }
        Check(legacyFoot!=null,"legacy initialization rig remains available for removal regression check");
        Quaternion legacyRotation=legacyFoot.localRotation;
        foreach(string openingClip in new[]{"Sitting","SitToStand"})
        {
            authored.PlayCinematic(openingClip,0);
            yield return new WaitForSeconds(.11f);
            for(int phase=0;phase<=10;phase++)
            {
                authored.PlayCinematic(openingClip,authored.MotionDuration(openingClip)*phase/10f);
                Bounded(render,origin,"opening clip="+openingClip+" phase="+phase);
                Check(actor.transform.position==origin,"opening animation preserves physics root");
                yield return null;
            }
        }
        authored.PlayCinematic("FightingIdle",0);
        character.CinematicPose=false;yield return null;yield return new WaitForEndOfFrame();
        Check(authored.CurrentMotion=="FightingIdle","idle state uses supplied fighting stance");
        foreach(bool heavy in new[]{false,true})
        {
            Transform support=authored.Bone(heavy?"RightFoot":"LeftFoot");
            Vector3 anchor=support.position;
            bool strikeObserved=false;Action observeStrike=()=>strikeObserved=true;
            actor.StrikeStarted+=observeStrike;
            Check(actor.TryAttack(heavy),"attack starts heavy="+heavy);
            float contactDeadline=Time.time+3;
            do { yield return new WaitForEndOfFrame(); } while(!strikeObserved&&Time.time<contactDeadline);
            actor.StrikeStarted-=observeStrike;
            Check(strikeObserved,"real contact observed heavy="+heavy);
            Check(authored.CurrentMotion==(heavy?"MmaKick1":"RoundhouseKick"),"attack selects distinct imported motion heavy="+heavy);
            Check(Mathf.Abs(authored.CurrentSampleTime-(heavy?37/60f:57/60f))<.15f,"visual impact synchronized with unchanged combat clock");
            Vector3 drift=support.position-anchor;drift.y=0;
            Check(drift.magnitude<.06f,"support ankle remains planted at attack impact heavy="+heavy);
            Bounded(render,origin,"attack impact");
            yield return new WaitForSeconds(actor.VisualRecovery+.25f);
            Check(actor.CanAct&&authored.CurrentMotion=="FightingIdle","attack returns to controllable fighting stance");
        }
        Check(legacyFoot.localRotation==legacyRotation,"legacy runtime driver does not rotate the hidden model");
        Check(actor.TryGuard(false),"guard starts");yield return new WaitForSeconds(.15f);
        Check(authored.CurrentMotion=="CenterBlock","guard uses CenterBlock");
        actor.ReceiveHit(new HitRequest(null,4,Vector3.left));yield return new WaitForEndOfFrame();
        Check(authored.CurrentMotion=="InwardBlock","blocked impact uses supplied InwardBlock");actor.ReleaseGuard();
        yield return new WaitForSeconds(.2f);
        Check(actor.TryAttack(false),"attack before interruption starts");
        actor.ReceiveHit(new HitRequest(null,8,Vector3.left));yield return new WaitForEndOfFrame();
        Check(authored.CurrentMotion=="HitReaction"&&actor.State==CombatActor.ActionState.HIT,"damage cancels attack animation");
        yield return new WaitForSeconds(.45f);
        Check(actor.CanAct&&authored.CurrentMotion=="FightingIdle","hit recovery has no stuck attack state");
        actor.ReceiveHit(new HitRequest(null,24,Vector3.left));yield return new WaitForEndOfFrame();
        Check(authored.CurrentMotion=="ReceivingABigUppercut","strong hit uses bounded reaction section");
        yield return new WaitForSeconds(.45f);
        Check(actor.TryCrouch(),"existing crouch action available");yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();
        Check(authored.CurrentMotion=="FightingIdle","missing crouch clip uses supplied neutral stance");
        Bounded(render,origin,"retargeted crouch fallback");
        Check(actor.State==CombatActor.ActionState.CROUCH,"removing custom crouch pose preserves crouch gameplay state");
        yield return new WaitForSeconds(.35f);
        Check(actor.TryDodge(Vector3.right),"dodge action available");yield return null;yield return new WaitForEndOfFrame();
        Check(authored.CurrentMotion=="FightingIdle","missing dodge clip uses supplied neutral stance");
        Bounded(render,origin,"dodge fallback");yield return new WaitForSeconds(.4f);
        CombatActor opponent=CreateActor("Grab target",1,Vector3.right);
        Check(actor.TryGrab(),"grab target acquired");yield return null;yield return new WaitForEndOfFrame();
        Check(authored.CurrentMotion=="FightingIdle","missing grab clip uses supplied neutral stance");
        yield return new WaitForSeconds(.5f);Destroy(opponent.gameObject);
        for(int tier=1;tier<=3;tier++)
        {
            actor.RestoreForTutorial();actor.Progress.ConsumeUltimate();actor.Progress.ConfirmHit(Time.time,gain:tier==1?33:tier==2?66:100);
            CombatActor victim=CreateActor("Flurry target",1,Vector3.right);
            int hits=0, strikes=0, lastIndex=-1;
            var selectedClips=new System.Collections.Generic.HashSet<string>();
            Action<CombatActor,CombatActor,Vector3> recordHit=(source,target,point)=> { if(target==victim)hits++; };
            Action recordStrike=()=>strikes++;
            actor.HitConfirmed+=recordHit;actor.StrikeStarted+=recordStrike;
            Check(actor.TryUltimate(),"ultimate starts tier="+tier);
            if(tier==2){yield return new WaitForSeconds(.2f);actor.ReceiveHit(new HitRequest(null,8,Vector3.left));}
            float timeout=Time.time+3;
            do
            {
                yield return null;yield return new WaitForEndOfFrame();
                selectedClips.Add(authored.CurrentMotion);
                if(lastIndex!=actor.UltimateStrikeIndex)
                { lastIndex=actor.UltimateStrikeIndex;Bounded(render,origin,"flurry impact tier="+tier+" index="+lastIndex); }
            } while(!actor.CanAct && Time.time<timeout);
            actor.HitConfirmed-=recordHit;actor.StrikeStarted-=recordStrike;
            Check(strikes==CombatActor.UltimateHitCount(tier),"ultimate spawns correct strike count tier="+tier);
            Check(hits==CombatActor.UltimateHitCount(tier),"each ultimate strike damages the target exactly once tier="+tier);
            Check(Mathf.Abs(victim.Health.Current-(120-(tier==3?64:tier==2?44:30)))<.01f,"flurry uses balanced total damage tier="+tier);
            Check(selectedClips.Contains("MmaKick1")&&selectedClips.Contains("MmaKick2")&&selectedClips.Contains("RoundhouseKick"),"flurry uses three supplied attack clips tier="+tier);
            Check(!selectedClips.Contains("FlyingKick"),"ultimate no longer uses airborne single-hit placeholder");
            yield return new WaitForSeconds(.15f);
            Check(actor.CanAct&&authored.CurrentMotion=="FightingIdle","ultimate returns to stance tier="+tier);
            Destroy(victim.gameObject);yield return null;
        }
        float sampleBefore=authored.CurrentSampleTime;Time.timeScale=0;
        yield return new WaitForSecondsRealtime(.12f);
        Check(authored.CurrentSampleTime==sampleBefore,"pause freezes imported visual playback");Time.timeScale=1;
        actor.Health.Infinite=false;actor.ReceiveHit(new HitRequest(null,1000,Vector3.left));yield return new WaitForSeconds(1.1f);yield return new WaitForEndOfFrame();
        Check(authored.CurrentMotion=="Stunned"&&actor.State==CombatActor.ActionState.DEAD,"death holds supplied fall pose");
        Bounded(render,origin,"death");
        actor.RestoreForTutorial();yield return new WaitForSeconds(.15f);
        Check(actor.CanAct&&authored.CurrentMotion=="FightingIdle","retry resets death animation");
        Check(actor.transform.position==origin&&collider.height==colliderHeight,"all actions preserve physical root and collider");
        Time.maximumDeltaTime=previousMaximumDelta;
        Debug.Log("AUTHORED_TEST_PASS review="+review+" checks="+checks+" candidates=23");
    }
}
