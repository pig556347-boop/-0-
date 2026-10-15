using System;
using System.Collections.Generic;
using UnityEngine;

// Retarget supplied walking and punches onto the existing Deongchi skin. Physics owns movement.
[DefaultExecutionOrder(200)]
public sealed class DeongchiWalkView : MonoBehaviour
{
    private CombatActor actor;
    private Rigidbody body;
    private Transform model,hips;
    private Transform[] bones;
    private Quaternion[] restModel,restLocal;
    private Transform[] feet;
    private Vector3 hipHome,modelHome;
    private float sole,clock,blend,scale;
    private AuthoredMotionLibrary.Clip walk;
    private AuthoredMotionLibrary library;
    private float attackStarted,hitAt=float.NegativeInfinity;
    public string CurrentMotion { get; private set; }="Rest";
    public float WalkWeight => blend;
    public float SampleTime { get; private set; }
    public void Initialize(CombatActor owner,Transform skin)
    {
        actor=owner;body=owner.GetComponent<Rigidbody>();model=skin;modelHome=skin.localPosition;
        var names=new Dictionary<string,Transform>();
        foreach(var bone in skin.GetComponentsInChildren<Transform>())names[bone.name]=bone;
        var aliases=new Dictionary<string,string>{{"Spine2","Chest"},{"LeftArm","LeftUpperArm"},{"RightArm","RightUpperArm"},
            {"LeftForeArm","LeftForearm"},{"RightForeArm","RightForearm"},{"LeftUpLeg","LeftThigh"},{"RightUpLeg","RightThigh"},
            {"LeftLeg","LeftShin"},{"RightLeg","RightShin"}};
        library=AuthoredMotionLibrary.Load();walk=library.Clips["Walking"];
        bones=new Transform[library.BoneNames.Length];restModel=new Quaternion[bones.Length];restLocal=new Quaternion[bones.Length];
        for(int i=0;i<bones.Length;i++)
        {
            string name=library.BoneNames[i].Substring(10);if(aliases.ContainsKey(name))name=aliases[name];
            if(!names.TryGetValue(name,out bones[i]))throw new InvalidOperationException("Deongchi walking bone missing: "+name);
            restModel[i]=Quaternion.Inverse(model.rotation)*bones[i].rotation;restLocal[i]=bones[i].localRotation;
        }
        hips=names["Hips"];hipHome=model.InverseTransformPoint(hips.position);
        feet=new[]{names["LeftFoot"],names["RightFoot"]};
        actor.AttackPrepared+=()=>{attackStarted=Time.time;hitAt=float.NegativeInfinity;};
        actor.StrikeStarted+=()=>hitAt=Time.time;
        sole=LowestFoot();scale=Mathf.Clamp(hipHome.y/.95f,.8f,1.4f);
    }
    private float LowestFoot()
    {float value=float.PositiveInfinity;foreach(var foot in feet)value=Mathf.Min(value,model.InverseTransformPoint(foot.position).y);return value;}
    private void LateUpdate()
    {
        if(actor==null||!actor.Health.Alive||Time.timeScale==0)return;
        float speed=new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude;
        float wanted=actor.State==CombatActor.ActionState.IDLE&&speed>.1f?1:0;
        blend=Mathf.MoveTowards(blend,wanted,Time.deltaTime*7);
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(actor.Facing),1-Mathf.Exp(-14*Time.deltaTime));
        if(actor.State==CombatActor.ActionState.ATTACK&&actor.PunchStrike)
        {
            string name=actor.PunchLeft?"PunchLeft":"PunchRight";
            var clip=library.Clips[name];float impact=actor.PunchLeft?34/60f:24/60f;
            float elapsed=hitAt>=attackStarted?actor.VisualWindup+Time.time-hitAt:Mathf.Min(Time.time-attackStarted,actor.VisualWindup);
            float time=elapsed<=actor.VisualWindup?Mathf.Lerp(0,impact,Mathf.Clamp01(elapsed/actor.VisualWindup)):
                Mathf.Lerp(impact,clip.Length,Mathf.Clamp01((elapsed-actor.VisualWindup)/actor.VisualRecovery));
            CurrentMotion=name;Apply(clip,time,1);return;
        }
        if(actor.State==CombatActor.ActionState.GUARD)
        {CurrentMotion="InwardBlock";Apply(library.Clips[CurrentMotion],.5f,1);return;}
        CurrentMotion=blend>.01f?"Walking":"Rest";
        if(blend<=0)
        {
            for(int i=0;i<bones.Length;i++)bones[i].localRotation=restLocal[i];
            model.localPosition=modelHome;hips.position=model.TransformPoint(hipHome);return;
        }
        clock+=Time.deltaTime*speed/1.8f;SampleTime=Mathf.Repeat(clock,walk.Length);
        Apply(walk,SampleTime,blend);
    }
    private void Apply(AuthoredMotionLibrary.Clip clip,float time,float weight)
    {
        float frame=Mathf.Clamp(time,0,clip.Length)*clip.FPS;int a=Mathf.FloorToInt(frame),b=Mathf.Min(a+1,clip.Frames.Length-1);float t=frame-a;
        for(int i=0;i<bones.Length;i++)bones[i].localRotation=restLocal[i];
        for(int i=0;i<bones.Length;i++)
        {
            Quaternion rest=Quaternion.Inverse(model.rotation)*bones[i].rotation;
            Quaternion pose=Quaternion.Slerp(clip.Frames[a].Rotations[i],clip.Frames[b].Rotations[i],t)*restModel[i];
            bones[i].rotation=model.rotation*Quaternion.Slerp(rest,pose,weight);
        }
        Vector3 delta=Vector3.Lerp(clip.Frames[a].HipDelta,clip.Frames[b].HipDelta,t)*scale*weight;
        hips.position=model.TransformPoint(hipHome+delta);
        model.localPosition=modelHome+Vector3.up*(sole-LowestFoot());
    }
}
