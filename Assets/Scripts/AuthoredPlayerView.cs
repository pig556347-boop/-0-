using System;
using System.Collections.Generic;
using SchoolRuleZero;
using UnityEngine;

// Imported motion is presentation only: combat clocks, colliders and Rigidbody remain authoritative.
[DefaultExecutionOrder(100)]
public sealed class AuthoredPlayerView : MonoBehaviour
{
    public static readonly string[] RequiredClips = { "FlyingKick", "MmaKick1", "MmaKick2", "RoundhouseKick",
        "CenterBlock", "RightBlock", "HitReaction", "ReceivingABigUppercut", "Stunned", "FightingIdle",
        "LeftStrafe", "RightStrafe", "Running", "WalkingBackwards", "Walking", "Jump", "Jumping1",
        "Jumping", "SitToStand", "Sitting" };
    public static string ClipKey(string importedName)
    {
        var clean = new System.Text.StringBuilder();
        foreach (char c in importedName) if (char.IsLetterOrDigit(c)) clean.Append(c);
        string value=clean.ToString();
        foreach(string name in RequiredClips)
            if(value.EndsWith(name,StringComparison.OrdinalIgnoreCase)) return name;
        return null;
    }
    private readonly Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
    private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
    private readonly List<BridgeBone> bridge = new List<BridgeBone>();
    private sealed class BridgeBone
    { public Transform Source, Target; }
    private PrototypeCharacterView owner;
    private CombatActor actor;
    private PlayerController motor;
    private Rigidbody body;
    private GameObject model;
    private Transform hips;
    private Transform[] poseBones;
    private Quaternion[] transitionRotations;
    private Vector3[] transitionPositions;
    private Material material;
    private Quaternion visualFacing;
    private Vector3 attackAnchor;
    private string supportName;
    private Vector3 home = new Vector3(0, -.95f, 0);
    private float soleHeight, transitionAt = -10, started, hitAt = -10, guardHitAt = -10;
    private float gaitTime, jumpAt, landingAt = -10;
    private int ultimateTier;
    private bool airborne;
    private string current = "", attack = "RoundhouseKick", hit = "HitReaction";
    private CombatActor.ActionState previous;
    public bool IsReady => model != null && clips.Count == 20 && hips != null;
    public float CurrentSampleTime { get; private set; }
    public string CurrentMotion { get; private set; } = "FightingIdle";
    public string[] ClipNames
    {
        get { var names = new List<string>(clips.Keys); names.Sort(StringComparer.Ordinal); return names.ToArray(); }
    }
    public Transform ModelRoot => model == null ? null : model.transform;
    public static bool TryBuild(PrototypeCharacterView owner, CombatActor actor, Transform legacyModel)
    {
        GameObject prefab = Resources.Load<GameObject>("Motions/PlayerMotions");
        if (prefab == null) return false;
        AuthoredPlayerView view = owner.gameObject.AddComponent<AuthoredPlayerView>();
        view.owner = owner; view.actor = actor; view.motor = actor.GetComponent<PlayerController>();
        view.body = actor.GetComponent<Rigidbody>();
        view.model = Instantiate(prefab, owner.transform, false);
        view.model.name = "Authored player motion skin";
        view.model.transform.localPosition = view.home;
        foreach (Animation animation in view.model.GetComponentsInChildren<Animation>()) animation.enabled = false;
        foreach (Animator animator in view.model.GetComponentsInChildren<Animator>()) animator.enabled = false;
        foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("Motions/PlayerMotions"))
            if (!clip.name.StartsWith("__preview__"))
            {
                string key=ClipKey(clip.name);
                if(key!=null) view.clips[key]=clip;
                else Debug.LogError("Unknown imported motion name: "+clip.name);
            }
        foreach (Transform bone in view.model.GetComponentsInChildren<Transform>())
            if (bone.name.StartsWith("mixamorig:")) view.bones[bone.name.Substring(10)] = bone;
        view.bones.TryGetValue("Hips", out view.hips);
        if (!view.IsReady)
        {
            Debug.LogError("Authored motion rig/clips incomplete: " + view.clips.Count);
            Destroy(view.model); Destroy(view); return false;
        }
        view.poseBones = view.model.GetComponentsInChildren<Transform>();
        view.transitionRotations = new Quaternion[view.poseBones.Length];
        view.transitionPositions = new Vector3[view.poseBones.Length];
        view.soleHeight = view.LowestFoot();
        view.material = new Material(Resources.Load<Shader>("Shaders/SchoolCharacter"));
        view.material.mainTexture = Resources.Load<Texture2D>("Characters/Player/Player_BaseColor");
        foreach (Renderer renderer in view.model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = view.material;
        foreach (SkinnedMeshRenderer skin in view.model.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen = true;
        foreach (Renderer renderer in legacyModel.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        view.MakeBridge(legacyModel);
        actor.AttackPrepared += view.OnAttack;
        actor.StrikeStarted += view.OnStrike;
        actor.UltimateStarted += view.OnUltimate;
        actor.Damaged += view.OnDamage;
        view.visualFacing = Quaternion.LookRotation(actor.Facing);
        owner.transform.rotation = view.visualFacing;
        view.Preview("FightingIdle", 0);
        Debug.Log("AUTHORED_PLAYER_READY candidates=20 bones=19 rootMotion=false");
        return true;
    }
    private void MakeBridge(Transform legacyModel)
    {
        var legacy = new Dictionary<string, Transform>();
        foreach (Transform bone in legacyModel.GetComponentsInChildren<Transform>()) legacy[bone.name] = bone;
        string[,] map = {
            {"Hips","Hips",""}, {"Spine","Spine","Chest"}, {"Spine1","Spine","Chest"},
            {"Spine2","Chest","Neck"}, {"Neck","Neck","Head"}, {"Head","Head",""},
            {"LeftShoulder","LeftShoulder","LeftUpperArm"}, {"RightShoulder","RightShoulder","RightUpperArm"},
            {"LeftArm","LeftUpperArm","LeftForearm"}, {"RightArm","RightUpperArm","RightForearm"},
            {"LeftForeArm","LeftForearm","LeftHand"}, {"RightForeArm","RightForearm","RightHand"},
            {"LeftHand","LeftHand",""}, {"RightHand","RightHand",""},
            {"LeftUpLeg","LeftThigh","LeftShin"}, {"RightUpLeg","RightThigh","RightShin"},
            {"LeftLeg","LeftShin","LeftFoot"}, {"RightLeg","RightShin","RightFoot"},
            {"LeftFoot","LeftFoot",""}, {"RightFoot","RightFoot",""}
        };
        for (int row=0; row<map.GetLength(0); row++)
        {
            if (!bones.ContainsKey(map[row,0])) continue;
            bridge.Add(new BridgeBone { Source=legacy[map[row,1]], Target=bones[map[row,0]] });
        }
    }
    private void OnAttack()
    {
        started = Time.time; hitAt = -10;
        attack = actor.HeavyStrike ? "MmaKick1" : actor.BasicComboHit == 2 ? "MmaKick2" : "RoundhouseKick";
        supportName=attack=="MmaKick1"?"RightFoot":"LeftFoot";
        attackAnchor=bones[supportName].position;
        current = ""; // Even two consecutive instances of one clip get a new blend origin.
    }
    private void OnStrike() { hitAt = Time.time; }
    private void OnUltimate(int tier) { ultimateTier = tier; started = Time.time; hitAt = -10; current = ""; }
    private void OnDamage(DamageResult result)
    {
        if (result.Kind == DamageKind.Guarded || result.Kind == DamageKind.Parried)
            guardHitAt = Time.time;
        else if (result.Kind == DamageKind.Hit)
        { started = Time.time; hit = result.Damage >= 20 ? "ReceivingABigUppercut" : "HitReaction"; current = ""; }
    }
    private void LateUpdate()
    {
        if (!IsReady || owner.CinematicPose || Time.timeScale == 0) return;
        if (previous != actor.State)
        {
            previous = actor.State;
            if (actor.State != CombatActor.ActionState.ATTACK && actor.State != CombatActor.ActionState.ULTIMATE)
                started = Time.time;
        }
        bool passive=actor.State==CombatActor.ActionState.IDLE || actor.State==CombatActor.ActionState.CROUCH;
        if (passive && ((motor != null && motor.IsCrouching && motor.IsGrounded) || actor.State == CombatActor.ActionState.CROUCH)) { ApplyBridge("Crouch fallback"); return; }
        if (actor.State == CombatActor.ActionState.ATTACK && !actor.HeavyStrike && actor.BasicComboHit == 3)
        { ApplyBridge("Spinning hook fallback"); return; }
        if (actor.State == CombatActor.ActionState.GRAB || actor.State == CombatActor.ActionState.DODGE)
        { ApplyBridge(actor.State == CombatActor.ActionState.GRAB ? "Grab fallback" : "Dodge fallback"); return; }
        Quaternion facing=Quaternion.LookRotation(actor.Facing);
        visualFacing=actor.State==CombatActor.ActionState.IDLE ?
            Quaternion.Slerp(visualFacing,facing,1-Mathf.Exp(-22*Time.deltaTime)) : facing;
        transform.rotation=visualFacing;
        float elapsed = Time.time - started;
        if (actor.State == CombatActor.ActionState.ATTACK)
        { PlayAttack(attack, elapsed, actor.VisualWindup, actor.VisualRecovery); return; }
        if (actor.State == CombatActor.ActionState.ULTIMATE)
        {
            float windup = ultimateTier == 3 ? .4f : ultimateTier == 2 ? .65f : .12f;
            if (ultimateTier == 2 && hitAt < started) Play("CenterBlock", Mathf.Min(.5f, elapsed), true);
            else PlayAttack(ultimateTier == 3 ? "FlyingKick" : ultimateTier == 2 ? "MmaKick1" : "MmaKick2",
                hitAt >= started && ultimateTier != 3 ? Time.time-hitAt+.12f : elapsed,
                ultimateTier == 3 ? .4f : .12f, ultimateTier == 3 ? .38f : .18f,
                ultimateTier == 3);
            return;
        }
        if (actor.State == CombatActor.ActionState.DEAD)
        { Play("Stunned", Mathf.Min(elapsed * 2.5f, clips["Stunned"].length), false); return; }
        if (actor.State == CombatActor.ActionState.HIT)
        {
            // Avoid presenting a floor knockdown while the unchanged gameplay state is standing hit-stun.
            float end = hit == "ReceivingABigUppercut" ? 32/60f : 40/60f;
            Play(hit, Mathf.Clamp01(elapsed/.25f)*end, true); return;
        }
        if (actor.State == CombatActor.ActionState.GUARD)
        {
            bool struck = Time.time-guardHitAt < .18f;
            Play(struck ? "RightBlock" : "CenterBlock", struck ? .32f+(Time.time-guardHitAt)*1.5f : .48f, true); return;
        }
        Vector3 velocity = body == null ? Vector3.zero : body.linearVelocity;
        if (motor != null && !motor.IsGrounded)
        {
            if (!airborne) { jumpAt = Time.time; airborne = true; current = ""; }
            float time = velocity.y > .1f ? Mathf.Lerp(.25f,.75f,Mathf.Clamp01((Time.time-jumpAt)/.42f)) : .85f;
            Play("Jumping1", time, false, true); return;
        }
        if (airborne) { airborne = false; landingAt = Time.time; current = ""; }
        if (Time.time - landingAt < .12f)
        { Play("Jumping1", Mathf.Lerp(1.3f,1.95f,(Time.time-landingAt)/.12f), true, true); return; }
        float speed = new Vector2(velocity.x,velocity.z).magnitude;
        if (speed < .15f) { gaitTime += Time.deltaTime; Play("FightingIdle", gaitTime % clips["FightingIdle"].length, true); }
        else
        {
            Vector3 local = Quaternion.Inverse(Quaternion.LookRotation(actor.Facing)) * velocity;
            string move = Mathf.Abs(local.x) > Mathf.Abs(local.z)*1.3f ? local.x < 0 ? "LeftStrafe" : "RightStrafe" :
                local.z < -.15f ? "WalkingBackwards" : speed > 3.2f ? "Running" : "Walking";
            float sourceSpeed = move == "Running" ? 5.76f : move == "Walking" ? 1.77f : move == "WalkingBackwards" ? 1.11f : 3.5f;
            gaitTime += Time.deltaTime * Mathf.Clamp(speed/(sourceSpeed*.86f),.4f,2.5f);
            Play(move, gaitTime % clips[move].length, true);
        }
    }
    private void PlayAttack(string name, float elapsed, float windup, float recovery, bool airborneAttack = false)
    {
        float first = name == "RoundhouseKick" ? 15/60f : 1/60f;
        float impact = name == "RoundhouseKick" ? 57/60f : name == "MmaKick1" ? 37/60f : name == "FlyingKick" ? 28/60f : 29/60f;
        float last = name == "RoundhouseKick" ? 115/60f : name == "MmaKick1" ? 85/60f : name == "FlyingKick" ? 85/60f : 75/60f;
        float time = elapsed <= windup ? Mathf.Lerp(first,impact,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/windup))) :
            Mathf.Lerp(impact,last,Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-windup)/recovery)));
        Play(name, time, !airborneAttack);
        if(actor.State==CombatActor.ActionState.ATTACK && supportName!=null)
        {
            Vector3 correction=attackAnchor-bones[supportName].position;correction.y=0;
            model.transform.position+=Vector3.ClampMagnitude(correction,.45f);
        }
    }
    private void Switch(string name)
    {
        if (current == name) return;
        for (int i=0; i<poseBones.Length; i++)
        { transitionRotations[i]=poseBones[i].localRotation; transitionPositions[i]=poseBones[i].localPosition; }
        transitionAt = Time.time; current = name;
    }
    private void Play(string name, float time, bool ground, bool physicalJump = false)
    {
        Switch(name); Sample(name,time);
        if (physicalJump)
        {
            // Source vertical flight is removed. Only Rigidbody supplies jump displacement.
            Vector3 p=model.transform.InverseTransformPoint(hips.position); p.y=.88f; hips.position=model.transform.TransformPoint(p);
        }
        BlendPose();
        if (ground) GroundFeet(name == "Running");
        CurrentMotion=name;
    }
    private void Sample(string name, float time)
    {
        model.transform.localPosition=home;
        model.transform.localRotation=Quaternion.identity;
        model.transform.localScale=Vector3.one;
        CurrentSampleTime=Mathf.Clamp(time,0,clips[name].length);
        clips[name].SampleAnimation(model,CurrentSampleTime);
    }
    private float LowestFoot()
    {
        float min=float.PositiveInfinity;
        foreach (string name in new[]{"LeftFoot","RightFoot","LeftToeBase","RightToeBase"})
            if (bones.ContainsKey(name)) min=Mathf.Min(min,model.transform.InverseTransformPoint(bones[name].position).y);
        return min;
    }
    private void GroundFeet(bool allowAir = false)
    {
        Vector3 p=model.transform.localPosition;
        float correction=soleHeight-LowestFoot();
        p.y=home.y+(allowAir?Mathf.Max(0,correction):correction);model.transform.localPosition=p;
    }
    private void BlendPose()
    {
        float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-transitionAt)/.09f));
        for (int i=0; i<poseBones.Length; i++)
        {
            poseBones[i].localRotation=Quaternion.Slerp(transitionRotations[i],poseBones[i].localRotation,blend);
            poseBones[i].localPosition=Vector3.Lerp(transitionPositions[i],poseBones[i].localPosition,blend);
        }
    }
    private void ApplyBridge(string label)
    {
        Switch(label); Sample("FightingIdle",0);
        // These clips were transferred to the exact existing rest rig. A name map
        // therefore copies local poses without another approximate retargeting step.
        foreach (BridgeBone joint in bridge)
        { joint.Target.localRotation=joint.Source.localRotation; joint.Target.localPosition=joint.Source.localPosition; }
        BlendPose(); GroundFeet(); CurrentMotion=label; visualFacing=transform.rotation;
    }
    public void PreviewFallback(string label)
    { Switch(label); transitionAt=-10; ApplyBridge(label); }
    public void Preview(string name,float normalizedTime)
    {
        if (!IsReady || !clips.ContainsKey(name)) throw new ArgumentException("Unknown motion "+name);
        Sample(name,clips[name].length*Mathf.Clamp01(normalizedTime));
        CurrentMotion=name;
    }
    public void PreviewCinematic(string name,float seconds)
    { if (IsReady && clips.ContainsKey(name)) { Sample(name,seconds); CurrentMotion=name; } }
    public void PreviewKick(float amount)
    { if (IsReady) { Sample("RoundhouseKick",Mathf.Lerp(15/60f,57/60f,Mathf.Clamp01(amount))); GroundFeet(); } }
    private void OnDestroy()
    {
        if (actor != null)
        { actor.AttackPrepared-=OnAttack; actor.StrikeStarted-=OnStrike; actor.UltimateStarted-=OnUltimate; actor.Damaged-=OnDamage; }
        if (material != null) Destroy(material);
    }
}
