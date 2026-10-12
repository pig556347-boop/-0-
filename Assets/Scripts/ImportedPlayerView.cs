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
        public bool FootIK;
    }
    private readonly Dictionary<string, Joint> joints = new Dictionary<string, Joint>();
    private CombatActor actor;
    private PlayerController motor;
    private PrototypeCharacterView owner;
    private Transform model, hips;
    private Vector3 hipsRest;
    private Quaternion hipsRestRotation;
    private Vector3 hipsPitchAxis, hipsRollAxis, leftFootHome, rightFootHome;
    private Quaternion leftFootBasis;
    private Rigidbody body;
    private Material material;
    private CombatActor.ActionState previous;
    private float actionStarted, struckAt = -100, gait, crouch, walkBlend, visualSpeed;
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
        view.hipsRestRotation = view.hips.localRotation;
        view.hipsPitchAxis = view.hips.InverseTransformDirection(view.model.right);
        view.hipsRollAxis = view.hips.InverseTransformDirection(view.model.forward);
        view.leftFootHome = view.model.InverseTransformPoint(view.joints["LeftFoot"].Bone.position) + view.model.localPosition;
        view.rightFootHome = view.model.InverseTransformPoint(view.joints["RightFoot"].Bone.position) + view.model.localPosition;
        view.leftFootBasis = Quaternion.Inverse(view.model.rotation) * view.joints["LeftFoot"].Bone.rotation;
        view.material = new Material(Resources.Load<Shader>("Shaders/SchoolCharacter"));
        view.material.mainTexture = Resources.Load<Texture2D>("Characters/" + kind + "/" + kind + "_BaseColor");
        foreach (Renderer renderer in view.model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = view.material;
        foreach (SkinnedMeshRenderer renderer in view.model.GetComponentsInChildren<SkinnedMeshRenderer>())
        renderer.updateWhenOffscreen = true;
        foreach (Animator animator in view.model.GetComponentsInChildren<Animator>()) animator.enabled = false;
        actor.StrikeStarted += view.OnStrike; actor.AttackPrepared += view.OnPrepare;
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
        j.Target = degrees; j.FootIK = false;
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
        joints[side + "Thigh"].FootIK = joints[side + "Shin"].FootIK = joints[side + "Foot"].FootIK = true;
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
        walkBlend = Mathf.MoveTowards(walkBlend, moving ? 1 : 0, Time.deltaTime * 8);
        visualSpeed = Mathf.Lerp(visualSpeed, speed, 1 - Mathf.Exp(-12 * Time.deltaTime));
        float stride = Mathf.Clamp(.15f + visualSpeed * .1f, .2f, .52f) * walkBlend * (1 - crouch * .55f);
        if (moving) gait += Time.deltaTime * Mathf.PI * speed / Mathf.Max(.12f, 2 * stride);
        float leftPhase = Mathf.Repeat(gait, 2 * Mathf.PI);
        float stancePhase = leftPhase < Mathf.PI ? leftPhase : leftPhase - Mathf.PI;
        float stanceForward = Mathf.Lerp(stride, -stride, stancePhase / Mathf.PI);
        float drop = .44f * crouch;
        // The planted foot travels backwards at body speed; the other foot swings up.
        // Hip height follows the reach of the planted leg instead of stretching it.
        float reach = .76f - drop;
        float contactBob = Mathf.Sqrt(Mathf.Max(.01f, reach * reach - stanceForward * stanceForward)) - (.78f - drop);
        float bob = Mathf.Lerp(.003f * Mathf.Sin(Time.time * 2), contactBob, walkBlend) * (1 - crouch);
        hips.localPosition = hipsRest + hips.parent.InverseTransformVector(model.up * (-drop + bob) - model.forward * (.14f * crouch));
        hips.localRotation = hipsRestRotation;
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "Left" : "Right";
            float phase = Mathf.Repeat(gait + i * Mathf.PI, 2 * Mathf.PI);
            bool planted = phase < Mathf.PI;
            float t = planted ? phase / Mathf.PI : (phase - Mathf.PI) / Mathf.PI;
            float forward = planted ? Mathf.Lerp(stride, -stride, t) : Mathf.Lerp(-stride, stride, Mathf.SmoothStep(0, 1, t));
            float lift = planted ? 0 : .14f * walkBlend * Mathf.Sin(t * Mathf.PI);
            Leg(side, .78f - drop + bob - lift, .14f * crouch + forward);
        }
        float swing = Mathf.Sin(gait) * 20 * walkBlend;
        float readyGuard = actor.Team == 0 ? 1 - walkBlend : 0;
        Set("LeftUpperArm", Mathf.Lerp(-20 - swing, -55, readyGuard));
        Set("RightUpperArm", Mathf.Lerp(-20 + swing, -55, readyGuard));
        Set("LeftForearm", Mathf.Lerp(-55, -110, readyGuard)); Set("RightForearm", Mathf.Lerp(-55, -110, readyGuard));
        Set("Spine", 18 * crouch + 3 * walkBlend); Set("Chest", 8 * crouch + 6 * walkBlend); Set("Head", -12 * crouch);
        bool attack = actor.State == CombatActor.ActionState.ATTACK || actor.State == CombatActor.ActionState.ULTIMATE;
        if (attack)
        {
            float since = Time.time - struckAt;
            float extend = since >= 0 && since < .23f ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.06f, .23f, since)) : 0;
            float chamber = struckAt >= actionStarted ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, .34f, since)) :
                Mathf.Clamp01((Time.time - actionStarted) / Mathf.Max(.03f, actor.VisualWindup));
            int poseStrike = struckAt >= actionStarted ? strikeNumber : strikeNumber + 1;
            if (actor.Team == 0)
            {
                string leg = poseStrike % 2 == 0 ? "Left" : "Right";
                Set(leg + "Thigh", -Mathf.Lerp(62, actor.HeavyStrike ? 100 : 86, extend) * chamber);
                Set(leg + "Shin", Mathf.Lerp(100, 9, extend) * chamber);
                Set(leg + "Foot", 10 * chamber);
                Set("Spine", -8 * extend); Set("Chest", -7 * extend);
                Set("LeftUpperArm", -55); Set("RightUpperArm", -55);
                Set("LeftForearm", -110); Set("RightForearm", -110);
            }
            else
            {
                string arm = actor.HeavyStrike || poseStrike % 2 == 0 ? "Right" : "Left";
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
        { j.Angle = j.FootIK ? j.Target : Mathf.Lerp(j.Angle, j.Target, 1 - Mathf.Exp(-30 * Time.deltaTime));
          j.Bone.localRotation = j.Rest * Quaternion.AngleAxis(j.Angle, j.Axis); }
        if (actor.Team == 0 && actor.State == CombatActor.ActionState.ATTACK && !actor.HeavyStrike && actor.BasicComboHit == 3)
        {
            float phase = struckAt >= actionStarted ? .45f + .55f * Mathf.Clamp01((Time.time - struckAt) / actor.VisualRecovery) :
                .45f * Mathf.Clamp01((Time.time - actionStarted) / actor.VisualWindup);
            ApplyHookPose(phase);
        }
        if (actor.State == CombatActor.ActionState.DEAD)
            transform.localRotation *= Quaternion.Euler(0, 0, Mathf.SmoothStep(0, 75, (Time.time - actionStarted) / .35f));
    }
    private void OnPrepare()
    {
        actionStarted = Time.time; previous = actor.State;
        if (actor.Team == 0 && !actor.HeavyStrike && actor.BasicComboHit == 3)
            Debug.Log("HOOK_KICK_START combo=3 right_leg=true");
    }

    private static float Ease(float start, float end, float phase) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, end, phase));

    // Preview calls the same solver as the real third-hit event. It never changes actor physics.
    public void PreviewHook(float phase)
    {
        foreach (Joint j in joints.Values) { j.Angle = j.Target = 0; j.Bone.localRotation = j.Rest; }
        hips.localPosition = hipsRest; hips.localRotation = hipsRestRotation;
        ApplyHookPose(Mathf.Clamp01(phase));
    }

    private void ApplyHookPose(float phase)
    {
        Quaternion facing = Quaternion.LookRotation(actor.Facing);
        float yaw = 360 * Ease(0, .45f, phase);
        transform.rotation = facing * Quaternion.AngleAxis(yaw, Vector3.up);
        float lift = Ease(.18f, .38f, phase) * (1 - Ease(.76f, 1, phase));
        float extension = Ease(.36f, .45f, phase) * (1 - Ease(.60f, .76f, phase));
        float lean = extension;
        hips.localRotation = hipsRestRotation * Quaternion.AngleAxis(-32 * lean, hipsPitchAxis) *
            Quaternion.AngleAxis(16 * lean, hipsRollAxis);
        Set("LeftUpperArm", -55, true); Set("RightUpperArm", -55, true);
        Set("LeftForearm", -110, true); Set("RightForearm", -110, true);

        // The support ankle remains in the same world location while the sole pivots.
        Vector3 support = actor.transform.position + facing * leftFootHome;
        SolveHookLeg("Left", support, facing * Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.forward,
            facing * Quaternion.AngleAxis(yaw, Vector3.up) * leftFootBasis);

        Vector3 home = actor.transform.position + facing * rightFootHome;
        Vector3 folded = actor.transform.position + facing * new Vector3(-.12f, .20f, .13f);
        float sweep = Mathf.Lerp(.22f, -.12f, Ease(.45f, .60f, phase));
        Vector3 high = actor.transform.position + facing * new Vector3(sweep, .70f, .27f);
        Vector3 target = Vector3.Lerp(home, Vector3.Lerp(folded, high, extension), lift);
        Vector3 bend = facing * Vector3.forward;
        SolveHookLeg("Right", target, bend, null);
        // Neutral ankle relative to the shin: no independent sideways ankle twist.
        joints["RightFoot"].Bone.localRotation = joints["RightFoot"].Rest;
    }

    private void SolveHookLeg(string side, Vector3 target, Vector3 bend, Quaternion? footRotation)
    {
        Transform thigh = joints[side + "Thigh"].Bone, shin = joints[side + "Shin"].Bone, foot = joints[side + "Foot"].Bone;
        Vector3 origin = thigh.position;
        float upper = Vector3.Distance(origin, shin.position), lower = Vector3.Distance(shin.position, foot.position);
        Vector3 delta = target - origin;
        float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + .001f, upper + lower - .001f);
        Vector3 direction = delta.sqrMagnitude > .000001f ? delta.normalized : Vector3.down;
        Vector3 perpendicular = Vector3.ProjectOnPlane(bend, direction).normalized;
        if (perpendicular.sqrMagnitude < .01f) perpendicular = Vector3.ProjectOnPlane(Vector3.right, direction).normalized;
        float along = (upper * upper + distance * distance - lower * lower) / (2 * distance);
        float height = Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
        Vector3 knee = origin + direction * along + perpendicular * height;
        Vector3 reached = origin + direction * distance;
        thigh.rotation = Quaternion.FromToRotation(shin.position - origin, knee - origin) * thigh.rotation;
        shin.rotation = Quaternion.FromToRotation(foot.position - shin.position, reached - shin.position) * shin.rotation;
        if (footRotation.HasValue) foot.rotation = footRotation.Value;
    }
    private void OnStrike() { struckAt = Time.time; strikeNumber++; }
    private void OnDestroy()
    { if (actor != null) { actor.StrikeStarted -= OnStrike; actor.AttackPrepared -= OnPrepare; } if (material != null) Destroy(material); }
}
