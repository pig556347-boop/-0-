using System;
using System.Collections.Generic;
using UnityEngine;

// Supplied weighted student skin and authored attacks. Never moves the actor's physics root.
[DefaultExecutionOrder(210)]
public sealed class Minion1View : MonoBehaviour
{
    private static readonly Dictionary<string,AuthoredMotionLibrary> attackLibraries=new Dictionary<string,AuthoredMotionLibrary>();
    private AuthoredMotionLibrary attacks;
    public string ModelKind { get; private set; }="Minion1";
    private AuthoredMotionLibrary locomotion;
    private CombatActor actor;
    private Rigidbody body;
    private Material skinMaterial;
    private Transform model,hips;
    private readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
    private readonly Dictionary<string,Quaternion> restWorld=new Dictionary<string,Quaternion>();
    private Transform[] all;
    private Quaternion[] restLocal,blendFrom;
    private Vector3 hipHome,home,blendHip;
    private float sole,started,hitAt=-100,gait,transition;
    private string attack="PunchLeft",current="";
    private int heavyIndex;
    public bool PreviewMode { get; set; }
    public string CurrentMotion { get; private set; }
    public Transform ModelRoot => model;
    public Transform Bone(string name)=>bones[name];
    public static bool TryBuild(PrototypeCharacterView owner,CombatActor actor,string modelKind="Minion1")
    {
        if(modelKind!="Minion1"&&modelKind!="Minion2")throw new ArgumentException("Unknown supplied minion model");
        var prefab=Resources.Load<GameObject>("Characters/"+modelKind+"/"+modelKind+"Model");
        if(prefab==null)throw new InvalidOperationException("Supplied "+modelKind+" model missing");
        var view=owner.gameObject.AddComponent<Minion1View>();view.ModelKind=modelKind;view.actor=actor;view.body=actor.GetComponent<Rigidbody>();
        view.model=Instantiate(prefab,owner.transform,false).transform;view.model.name=modelKind+" skin";
        view.model.localScale=Vector3.one;view.model.localRotation=Quaternion.identity;
        view.home=new Vector3(0,-.95f,0);view.model.localPosition=view.home;
        var importedSkin=view.model.GetComponentInChildren<SkinnedMeshRenderer>();
        float importedHeight=MeasuredHeight(importedSkin);
        if(importedHeight<=.0001f)throw new InvalidOperationException(modelKind+" imported skin bounds invalid");
        view.model.localScale*=1.85f/importedHeight;
        foreach(var animator in view.model.GetComponentsInChildren<Animator>())animator.enabled=false;
        foreach(var animation in view.model.GetComponentsInChildren<Animation>())animation.enabled=false;
        view.all=view.model.GetComponentsInChildren<Transform>();view.restLocal=new Quaternion[view.all.Length];view.blendFrom=new Quaternion[view.all.Length];
        for(int i=0;i<view.all.Length;i++)
        {
            var b=view.all[i];string name=b.name.StartsWith("mixamorig:")?b.name.Substring(10):b.name;
            view.bones[name]=b;view.restWorld[name]=Quaternion.Inverse(view.model.rotation)*b.rotation;view.restLocal[i]=b.localRotation;
        }
        view.hips=view.bones["Hips"];view.hipHome=view.model.InverseTransformPoint(view.hips.position);view.sole=view.LowestFoot();
        Debug.Log(modelKind.ToUpperInvariant()+"_LAYOUT measuredRestHeight="+importedHeight+" scale="+view.model.lossyScale+" hip="+view.hipHome+" skinScale="+view.model.GetComponentInChildren<SkinnedMeshRenderer>().transform.lossyScale);
        if(!attackLibraries.TryGetValue(modelKind,out view.attacks))
        {
            var data=Resources.Load<TextAsset>("Characters/"+modelKind+"/"+modelKind+"PoseData");
            if(data==null)throw new InvalidOperationException(modelKind+" authored attack data missing");
            view.attacks=AuthoredMotionLibrary.Read(data.bytes,modelKind=="Minion2"?5:4,modelKind=="Minion2"?41:33);
            attackLibraries.Add(modelKind,view.attacks);
        }
        foreach(string name in view.attacks.BoneNames)
            if(!view.bones.ContainsKey(name.Substring(10)))throw new InvalidOperationException(modelKind+" bone missing: "+name);
        view.locomotion=AuthoredMotionLibrary.Load();
        var material=new Material(Resources.Load<Shader>("Shaders/SchoolCharacter"));
        var texture=Resources.Load<Texture2D>("Characters/"+modelKind+"/"+modelKind+"_BaseColor");
        if(texture==null)throw new InvalidOperationException(modelKind+" base color texture resource missing at runtime");
        view.skinMaterial=material;material.mainTexture=texture;
        foreach(var renderer in view.model.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
        foreach(var skin in view.model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
        actor.AttackPrepared+=view.OnAttack;actor.StrikeStarted+=()=>view.hitAt=Time.time;
        actor.Damaged+=result=>{if(result.Kind==SchoolRuleZero.DamageKind.Hit){view.started=Time.time;view.current="";}};
        Debug.Log(modelKind.ToUpperInvariant()+"_READY bones="+view.attacks.BoneNames.Length+" weighted=true");return true;
    }
    private void OnDestroy(){if(skinMaterial!=null)Destroy(skinMaterial);}
    private static float MeasuredHeight(SkinnedMeshRenderer skin)
    {
        var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
        var matrices=new Matrix4x4[bind.Length];for(int i=0;i<bind.Length;i++)matrices[i]=skin.bones[i].localToWorldMatrix*bind[i];
        float lo=float.PositiveInfinity,hi=float.NegativeInfinity;
        for(int i=0;i<vertices.Length;i++)
        {
            var w=weights[i];var v=vertices[i];
            Vector3 point=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+
                matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
            lo=Mathf.Min(lo,point.y);hi=Mathf.Max(hi,point.y);
        }
        return hi-lo;
    }
    private void OnAttack()
    {
        started=Time.time;hitAt=-100;
        if(actor.HeavyStrike)
        {
            int index=heavyIndex++%(ModelKind=="Minion2"?3:2);
            attack=index==0?"MmaKick":index==1?"Kicking":"SideKick";
        }
        else attack=actor.PunchLeft?"PunchLeft":"PunchRight";
        current="";
    }
    private float LowestFoot()
    {
        float y=float.PositiveInfinity;
        foreach(string name in new[]{"LeftFoot","RightFoot","LeftToeBase","RightToeBase"})y=Mathf.Min(y,model.InverseTransformPoint(bones[name].position).y);
        return y;
    }
    private void LateUpdate()
    {
        if(PreviewMode||actor==null||!actor.Health.Alive||Time.timeScale==0)return;
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(actor.Facing),1-Mathf.Exp(-18*Time.deltaTime));
        if(actor.State==CombatActor.ActionState.ATTACK)
        {
            var clip=attacks.Clips[attack];float impact=Impact(attack);
            float elapsed=hitAt>=started?actor.VisualWindup+Time.time-hitAt:Mathf.Min(Time.time-started,actor.VisualWindup);
            float t=elapsed<=actor.VisualWindup?Mathf.Lerp(0,impact,Mathf.Clamp01(elapsed/actor.VisualWindup)):
                Mathf.Lerp(impact,clip.Length,Mathf.Clamp01((elapsed-actor.VisualWindup)/actor.VisualRecovery));
            Play(attacks,attack,t);return;
        }
        if(actor.State==CombatActor.ActionState.HIT)
        {Play(locomotion,"HitReaction",Mathf.Clamp01((Time.time-started)/.25f)*.6f);return;}
        if(actor.State==CombatActor.ActionState.GUARD)
        {Play(locomotion,"InwardBlock",.5f);return;}
        float speed=new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude;
        if(speed>.15f&&actor.State==CombatActor.ActionState.IDLE)
        {gait+=Time.deltaTime*Mathf.Clamp(speed/1.8f,.3f,2.2f);Play(locomotion,"Walking",gait%locomotion.Clips["Walking"].Length);}
        else {gait+=Time.deltaTime;Play(locomotion,"FightingIdle",gait%locomotion.Clips["FightingIdle"].Length);}
    }
    private float Impact(string name)
    {return name=="PunchLeft"?34/60f:name=="PunchRight"?22/60f:name=="SideKick"?49/60f:name=="MmaKick"?(ModelKind=="Minion2"?39:37)/60f:69/60f;}
    public void Preview(string name,float phase)
    {
        PreviewMode=true;current=name;transition=Time.time-1;
        var library=attacks.Clips.ContainsKey(name)?attacks:locomotion;
        Play(library,name,library.Clips[name].Length*Mathf.Clamp01(phase));
    }
    public void ResumeRuntime(){PreviewMode=false;current="";}
    private void Play(AuthoredMotionLibrary library,string name,float time)
    {
        if(current!=name)
        {
            for(int i=0;i<all.Length;i++)blendFrom[i]=all[i].localRotation;
            blendHip=model.InverseTransformPoint(hips.position);transition=Time.time;current=name;
        }
        CurrentMotion=name;var clip=library.Clips[name];float frame=Mathf.Clamp(time,0,clip.Length)*clip.FPS;
        int a=Mathf.Clamp(Mathf.FloorToInt(frame),0,clip.Frames.Length-1),b=Mathf.Min(a+1,clip.Frames.Length-1);float t=frame-a;
        model.localPosition=home;
        for(int i=0;i<all.Length;i++)all[i].localRotation=restLocal[i];
        for(int i=0;i<library.BoneNames.Length;i++)
        {
            string key=library.BoneNames[i].Substring(10);if(!bones.TryGetValue(key,out Transform bone))continue;
            Quaternion delta=Quaternion.Slerp(clip.Frames[a].Rotations[i],clip.Frames[b].Rotations[i],t);
            bone.rotation=model.rotation*delta*restWorld[key];
        }
        hips.position=model.TransformPoint(hipHome)+model.rotation*Vector3.Lerp(clip.Frames[a].HipDelta,clip.Frames[b].HipDelta,t);
        float mix=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-transition)/.07f));
        for(int i=0;i<all.Length;i++)all[i].localRotation=Quaternion.Slerp(blendFrom[i],all[i].localRotation,mix);
        hips.position=model.TransformPoint(Vector3.Lerp(blendHip,model.InverseTransformPoint(hips.position),mix));
        model.position+=Vector3.up*((sole-LowestFoot())*model.lossyScale.y);
    }
}
