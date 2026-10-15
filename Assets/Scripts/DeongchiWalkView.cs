using System;
using System.Collections.Generic;
using UnityEngine;

// Retarget only the supplied Walking clip. Attacks and combat clocks are untouched.
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
    public float WalkWeight => blend;
    public float SampleTime { get; private set; }
    public string CurrentMotion => blend>.01f?"Walking":"Rest";
    public void Initialize(CombatActor owner,Transform skin)
    {
        actor=owner;body=owner.GetComponent<Rigidbody>();model=skin;modelHome=skin.localPosition;
        var names=new Dictionary<string,Transform>();
        foreach(var bone in skin.GetComponentsInChildren<Transform>())names[bone.name]=bone;
        var aliases=new Dictionary<string,string>{{"Spine2","Chest"},{"LeftArm","LeftUpperArm"},{"RightArm","RightUpperArm"},
            {"LeftForeArm","LeftForearm"},{"RightForeArm","RightForearm"},{"LeftUpLeg","LeftThigh"},{"RightUpLeg","RightThigh"},
            {"LeftLeg","LeftShin"},{"RightLeg","RightShin"}};
        var library=AuthoredMotionLibrary.Load();walk=library.Clips["Walking"];
        bones=new Transform[library.BoneNames.Length];restModel=new Quaternion[bones.Length];restLocal=new Quaternion[bones.Length];
        for(int i=0;i<bones.Length;i++)
        {
            string name=library.BoneNames[i].Substring(10);if(aliases.ContainsKey(name))name=aliases[name];
            if(!names.TryGetValue(name,out bones[i]))throw new InvalidOperationException("Deongchi walking bone missing: "+name);
            restModel[i]=Quaternion.Inverse(model.rotation)*bones[i].rotation;restLocal[i]=bones[i].localRotation;
        }
        hips=names["Hips"];hipHome=model.InverseTransformPoint(hips.position);
        feet=new[]{names["LeftFoot"],names["RightFoot"]};
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
        if(blend<=0)
        {
            for(int i=0;i<bones.Length;i++)bones[i].localRotation=restLocal[i];
            model.localPosition=modelHome;hips.position=model.TransformPoint(hipHome);return;
        }
        clock+=Time.deltaTime*speed/1.8f;SampleTime=Mathf.Repeat(clock,walk.Length);
        float frame=SampleTime*walk.FPS;int a=Mathf.FloorToInt(frame),b=Mathf.Min(a+1,walk.Frames.Length-1);float t=frame-a;
        for(int i=0;i<bones.Length;i++)bones[i].localRotation=restLocal[i];
        for(int i=0;i<bones.Length;i++)
        {
            Quaternion rest=Quaternion.Inverse(model.rotation)*bones[i].rotation;
            Quaternion pose=Quaternion.Slerp(walk.Frames[a].Rotations[i],walk.Frames[b].Rotations[i],t)*restModel[i];
            bones[i].rotation=model.rotation*Quaternion.Slerp(rest,pose,blend);
        }
        Vector3 delta=Vector3.Lerp(walk.Frames[a].HipDelta,walk.Frames[b].HipDelta,t)*scale*blend;
        hips.position=model.TransformPoint(hipHome+delta);
        model.localPosition=modelHome+Vector3.up*(sole-LowestFoot());
    }
}
