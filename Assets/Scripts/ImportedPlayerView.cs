using System.Collections.Generic;
using UnityEngine;

// Visual rig driver: actor physics, hitboxes and combat timing remain authoritative.
public sealed class ImportedPlayerView : MonoBehaviour
{
    private sealed class Joint
    {
        public Transform Bone;
        public Quaternion Rest;
        public Vector3 Axis;
        public float Angle, Target;
    }
    private readonly Dictionary<string, Joint> joints = new Dictionary<string, Joint>();
    private CombatActor actor;
    private PlayerController motor;
    private PrototypeCharacterView owner;
    private Transform model, hips;
    private Vector3 hipsRest;
    private Rigidbody body;
    private Material material;
    private CombatActor.ActionState previous;
    private float actionStarted, struckAt = -100, gait, crouch;
    private int strikeNumber;
    public bool IsReady => joints.Count == 13 && hips != null;
    public string ModelKind { get; private set; }
    public static bool TryBuild(PrototypeCharacterView owner, CombatActor actor, string kind = "Player")
    {
        GameObject source = Resources.Load<GameObject>("Characters/" + kind + "/" + kind + "Model");
        if (source == null) { Debug.LogError("Character model missing: " + kind); return false; }
        ImportedPlayerView view = owner.gameObject.AddComponent<ImportedPlayerView>();
        view.owner = owner; view.actor = actor; view.motor = actor.GetComponent<PlayerController>();
        view.body = actor.GetComponent<Rigidbody>(); view.ModelKind = kind;
        view.model = Instantiate(source, owner.transform, false).transform;
        view.model.name = "Imported " + kind + " model";
        view.model.localPosition = new Vector3(0, -.95f, 0);
        view.model.localRotation = Quaternion.identity; view.model.localScale = Vector3.one;
        foreach (string name in new[] { "LeftThigh", "RightThigh", "LeftShin", "RightShin", "LeftFoot", "RightFoot",
            "LeftUpperArm", "RightUpperArm", "LeftForearm", "RightForearm", "Spine", "Chest", "Head" })
        {
            Transform bone = Find(view.model, name);
            if (bone == null) continue;
            view.joints.Add(name, new Joint { Bone = bone, Rest = bone.localRotation,
                Axis = bone.InverseTransformDirection(view.model.right).normalized });
        }
        view.hips = Find(view.model, "Hips");
        if (!view.IsReady) { Debug.LogError("Character rig incomplete: " + kind); Destroy(view.model.gameObject); Destroy(view); return false; }
        view.hipsRest = view.hips.localPosition;
        view.material = new Material(Resources.Load<Shader>("Shaders/SchoolCharacter"));
        view.material.mainTexture = Resources.Load<Texture2D>("Characters/" + kind + "/" + kind + "_BaseColor");
        foreach (Renderer renderer in view.model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = view.material;
        foreach (SkinnedMeshRenderer renderer in view.model.GetComponentsInChildren<SkinnedMeshRenderer>())
        renderer.updateWhenOffscreen = true;
        foreach (Animator animator in view.model.GetComponentsInChildren<Animator>()) animator.enabled = false;
        actor.StrikeStarted += view.OnStrike;
        Debug.Log((kind == "Player" ? "PLAYER_MODEL_READY" : "DEONGCHI_MODEL_READY") + " bones=19 anatomical_skin=true");
        return true;
    }
    private static Transform Find(Transform root, string name)
    {
        foreach (Transform bone in root.GetComponentsInChildren<Transform>()) if (bone.name == name) return bone;
        return null;
    }
    private void Set(string name, float degrees, bool instant = false)
    {
        Joint j = joints[name];
        j.Target = degrees;
        if (instant) { j.Angle = degrees; j.Bone.localRotation = j.Rest * Quaternion.AngleAxis(j.Angle, j.Axis); }
    }
    // Two-joint sagittal IK; targets are relative to each hip, in metres.
    private void Leg(string side, float down, float forward, bool instant = false)
    {
        const float upper = .40f, lower = .38f;
        float distance = Mathf.Clamp(Mathf.Sqrt(down * down + forward * forward), .12f, upper + lower - .0001f);
        float alpha = Mathf.Acos(Mathf.Clamp((upper * upper + distance * distance - lower * lower) / (2 * upper * distance), -1, 1));
        float knee = 180 - Mathf.Acos(Mathf.Clamp((upper * upper + lower * lower - distance * distance) / (2 * upper * lower), -1, 1)) * Mathf.Rad2Deg;
        float thigh = -Mathf.Atan2(forward, down) * Mathf.Rad2Deg - alpha * Mathf.Rad2Deg;
        Set(side + "Thigh", thigh, instant); Set(side + "Shin", knee, instant);
        Set(side + "Foot", -thigh - knee, instant);
    }
    public void ForcedKick(float amount)
    {
        if (!IsReady) return;
        amount = Mathf.Clamp01(amount);
        Set("RightThigh", -105 * amount, true);
        Set("RightShin", 65 * Mathf.Sin(amount * Mathf.PI), true);
        Set("RightFoot", 15 * amount, true);
        Set("LeftUpperArm", -28 * amount, true); Set("LeftForearm", -75 * amount, true);
    }
    private void LateUpdate()
    {
        if (actor == null || !IsReady || owner.CinematicPose) return;
        transform.rotation = Quaternion.LookRotation(actor.Facing);
        if (previous != actor.State) { previous = actor.State; actionStarted = Time.time; }
        bool crouching = motor != null ? motor.IsCrouching : actor.State == CombatActor.ActionState.CROUCH;
        crouch = Mathf.MoveTowards(crouch, crouching ? 1 : 0, Time.deltaTime * 7);
        float speed = body == null ? 0 : new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude;
        bool moving = speed > .15f && actor.State == CombatActor.ActionState.IDLE;
        gait += Time.deltaTime * Mathf.Lerp(6, 13, Mathf.Clamp01(speed / 5));
        float stride = moving ? Mathf.Clamp(speed * .045f, .05f, .23f) * (1 - crouch * .5f) : 0;
        float bob = moving ? .014f * Mathf.Cos(gait * 2) : .003f * Mathf.Sin(Time.time * 2);
        float drop = .31f * crouch;
        // Hips translate without squashing the mesh. Foot IK preserves floor contact.
        hips.localPosition = hipsRest + hips.parent.InverseTransformVector(model.up * (-drop + bob) - model.forward * (.11f * crouch));
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "Left" : "Right";
            float phase = gait + i * Mathf.PI;
            float lift = moving ? .075f * Mathf.Max(0, Mathf.Sin(phase)) : 0;
            Leg(side, .78f - drop + bob - lift, .11f * crouch + stride * Mathf.Cos(phase));
        }
        float swing = moving ? Mathf.Sin(gait) * 12 : 0;
        Set("LeftUpperArm", -20 - swing); Set("RightUpperArm", -20 + swing);
        Set("LeftForearm", -55); Set("RightForearm", -55);
        Set("Spine", 9 * crouch); Set("Chest", 6 * crouch); Set("Head", -8 * crouch);
        bool attack = actor.State == CombatActor.ActionState.ATTACK || actor.State == CombatActor.ActionState.ULTIMATE;
        if (attack)
        {
            float since = Time.time - struckAt;
            float extend = since >= 0 && since < .23f ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.06f, .23f, since)) : 0;
            float chamber = struckAt >= actionStarted ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, .34f, since)) :
                Mathf.Clamp01((Time.time - actionStarted) / Mathf.Max(.03f, actor.VisualWindup));
            if (actor.Team == 0)
            {
                string leg = strikeNumber % 2 == 0 ? "Left" : "Right";
                Set(leg + "Thigh", -Mathf.Lerp(62, actor.HeavyStrike ? 100 : 86, extend) * chamber);
                Set(leg + "Shin", Mathf.Lerp(100, 9, extend) * chamber);
                Set(leg + "Foot", 10 * chamber);
                Set("Spine", -8 * extend); Set("Chest", -7 * extend);
                Set("LeftUpperArm", -28); Set("RightUpperArm", -22);
                Set("LeftForearm", -90); Set("RightForearm", -95);
            }
            else
            {
                string arm = actor.HeavyStrike || strikeNumber % 2 == 0 ? "Right" : "Left";
                Set(arm + "UpperArm", Mathf.Lerp(-30, -87, extend));
                Set(arm + "Forearm", Mathf.Lerp(-95, -12, extend)); Set("Chest", 8 * extend);
            }
        }
        if (actor.State == CombatActor.ActionState.GUARD || actor.State == CombatActor.ActionState.GRAB)
        {
            Set("LeftUpperArm", -32); Set("RightUpperArm", -32);
            Set("LeftForearm", -105); Set("RightForearm", -105); Set("Chest", 8);
        }
        if (actor.State == CombatActor.ActionState.HIT) { Set("Spine", -13); Set("Chest", -10); }
        if (actor.State == CombatActor.ActionState.DODGE) { Set("Spine", 18); Set("Chest", 12); }
        if (motor != null && !motor.IsGrounded) { Set("LeftThigh", -22); Set("RightThigh", -32); Set("LeftShin", 45); Set("RightShin", 58); }
        foreach (Joint j in joints.Values)
        { j.Angle = Mathf.Lerp(j.Angle, j.Target, 1 - Mathf.Exp(-30 * Time.deltaTime));
          j.Bone.localRotation = j.Rest * Quaternion.AngleAxis(j.Angle, j.Axis); }
        if (actor.State == CombatActor.ActionState.DEAD)
            transform.localRotation *= Quaternion.Euler(0, 0, Mathf.SmoothStep(0, 75, (Time.time - actionStarted) / .35f));
    }
    private void OnStrike() { struckAt = Time.time; strikeNumber++; }
    private void OnDestroy()
    { if (actor != null) actor.StrikeStarted -= OnStrike; if (material != null) Destroy(material); }
}
