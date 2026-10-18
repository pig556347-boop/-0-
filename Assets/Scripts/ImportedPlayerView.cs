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
    private Quaternion rightFootBasis, headBasis, fightFacing;
    private readonly Dictionary<string, Quaternion> segmentBasis = new Dictionary<string, Quaternion>();
    private Transform leftHand, rightHand;
    private Quaternion leftHandRest, rightHandRest;
    private Vector3 leftHandAxis, rightHandAxis;
    private PrototypeHUD focusHUD;
    private float nextFocusScan, fightGait, lastDrop = .11f;
    private bool wasMoving;
    private readonly FootContact leftContact = new FootContact(), rightContact = new FootContact();
    private Vector3 attackLeftStart, attackRightStart, attackLeftHandStart, attackRightHandStart;
    private float attackDropStart = .11f;
    private Quaternion attackRotationStart;
    private float attackSpineStart, attackChestStart, guardWeight;
    private bool inPreview;
    private float ultimateWindup = .12f;
    private sealed class FootContact { public bool Planted; public Vector3 Anchor; }
    private float hookSide, hookRollSign;
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
        if(kind=="Deongchi")actor.GetComponent<CombatHurtbox>().SetBuild(.70f,.48f,1.90f);
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
        view.rightFootBasis = Quaternion.Inverse(view.model.rotation) * view.joints["RightFoot"].Bone.rotation;
        view.headBasis = Quaternion.Inverse(view.model.rotation) * view.joints["Head"].Bone.rotation;
        view.fightFacing = Quaternion.LookRotation(actor.Facing);
        foreach (string side in new[] { "Left", "Right" })
        {
            foreach (string part in new[] { "Thigh", "Shin" })
            {
                Transform bone = view.joints[side + part].Bone;
                Transform child = view.joints[side + (part == "Thigh" ? "Shin" : "Foot")].Bone;
                Vector3 primary = (child.position - bone.position).normalized;
                Vector3 pole = Vector3.ProjectOnPlane(view.model.forward, primary).normalized;
                view.segmentBasis[side + part] = Quaternion.LookRotation(bone.InverseTransformDirection(primary), bone.InverseTransformDirection(pole));
            }
        }
        view.leftHand = Find(view.model, "LeftHand"); view.rightHand = Find(view.model, "RightHand");
        view.leftHandRest = view.leftHand.localRotation; view.rightHandRest = view.rightHand.localRotation;
        view.leftHandAxis = view.leftHand.InverseTransformDirection(view.leftHand.position - view.joints["LeftForearm"].Bone.position).normalized;
        view.rightHandAxis = view.rightHand.InverseTransformDirection(view.rightHand.position - view.joints["RightForearm"].Bone.position).normalized;
        view.hookSide = Mathf.Sign(view.rightFootHome.x);
        // FBX coordinate conversion can mirror lateral axes. Measure rather than assume
        // which roll lowers the support hip; the original pose is restored immediately.
        float supportHeight = view.joints["LeftThigh"].Bone.position.y;
        view.hips.localRotation = view.hipsRestRotation * Quaternion.AngleAxis(5, view.hipsRollAxis);
        view.hookRollSign = view.joints["LeftThigh"].Bone.position.y > supportHeight ? -1 : 1;
        view.hips.localRotation = view.hipsRestRotation;
        view.material = new Material(Resources.Load<Shader>("Shaders/SchoolCharacter"));
        view.material.mainTexture = Resources.Load<Texture2D>("Characters/" + kind + "/" + kind + "_BaseColor");
        foreach (Renderer renderer in view.model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = view.material;
        foreach (SkinnedMeshRenderer renderer in view.model.GetComponentsInChildren<SkinnedMeshRenderer>())
        renderer.updateWhenOffscreen = true;
        foreach (Animator animator in view.model.GetComponentsInChildren<Animator>()) animator.enabled = false;
        if (kind == "Deongchi" && owner.UseAuthoredMotion) owner.gameObject.AddComponent<DeongchiWalkView>().Initialize(actor,view.model);
        if (kind == "Player" && owner.UseAuthoredMotion) AuthoredPlayerView.TryBuild(owner, actor, view.model);
        if (!owner.UseAuthoredMotion)
        { actor.StrikeStarted += view.OnStrike; actor.AttackPrepared += view.OnPrepare; actor.UltimateStarted += view.OnUltimate; }
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
        if (actor == null || !actor.Health.Alive || !IsReady || owner.CinematicPose || owner.UseAuthoredMotion) return;
        transform.rotation = Quaternion.LookRotation(actor.Facing);
        if (previous != actor.State) { previous = actor.State; actionStarted = Time.time; }
        bool crouching = motor != null ? motor.IsCrouching : actor.State == CombatActor.ActionState.CROUCH;
        crouch = Mathf.MoveTowards(crouch, crouching ? 1 : 0, Time.deltaTime * 7);
        float speed = body == null ? 0 : new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude;
        bool moving = speed > .15f && actor.State == CombatActor.ActionState.IDLE;
        walkBlend = Mathf.MoveTowards(walkBlend, moving ? 1 : 0, Time.deltaTime * (ModelKind == "Deongchi" ? 5 : 8));
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
        if (actor.Team == 0 && actor.State != CombatActor.ActionState.DEAD)
        {
            bool grounded = motor == null || motor.IsGrounded;
            if (grounded)
            {
                if (actor.State == CombatActor.ActionState.ATTACK || actor.State == CombatActor.ActionState.ULTIMATE)
                {
                    bool ultimate = actor.State == CombatActor.ActionState.ULTIMATE;
                    float phase = struckAt >= actionStarted ? .45f + .55f * Mathf.Clamp01((Time.time - struckAt) / (ultimate ? .18f : actor.VisualRecovery)) :
                        .45f * Mathf.Clamp01((Time.time - actionStarted) / (ultimate ? ultimateWindup : actor.VisualWindup));
                    if (!ultimate && !actor.HeavyStrike && actor.BasicComboHit == 3) ApplyHookPose(phase);
                    else ApplyRoundhousePose(actor.HeavyStrike || actor.BasicComboHit != 2 ? "Right" : "Left", phase);
                }
                else
                {
                    fightFacing = Quaternion.Slerp(fightFacing, Quaternion.LookRotation(actor.Facing), 1 - Mathf.Exp(-14 * Time.deltaTime));
                    ApplyFightingPose(speed, fightGait, walkBlend, false);
                }
            }
            else
            {
                lastDrop = Mathf.MoveTowards(lastDrop, 0, Time.deltaTime * .9f);
                SetHip(lastDrop, 0, 0);
                SetGuard();
            }
            AimHead();
        }
        if (ModelKind == "Deongchi" && actor.State != CombatActor.ActionState.DEAD)
        {
            if (actor.State == CombatActor.ActionState.ATTACK)
            {
                float phase = struckAt >= actionStarted ? .45f + .55f * Mathf.Clamp01((Time.time - struckAt) / actor.VisualRecovery) :
                    .45f * Mathf.Clamp01((Time.time - actionStarted) / actor.VisualWindup);
                int number = struckAt >= actionStarted ? strikeNumber : strikeNumber + 1;
                ApplyPunchPose(actor.HeavyStrike || number % 2 == 0 ? "Right" : "Left", phase);
            }
            else if (actor.State != CombatActor.ActionState.ULTIMATE)
            {
                fightFacing = Quaternion.Slerp(fightFacing, Quaternion.LookRotation(actor.Facing), 1 - Mathf.Exp(-10 * Time.deltaTime));
                ApplyFightingPose(speed, fightGait, walkBlend, false);
            }
            AimHead();
        }
    }
    private void OnPrepare()
    {
        actionStarted = Time.time; previous = actor.State;
        if (ModelKind == "Deongchi")
        {
            wasMoving = false;
            leftContact.Planted = rightContact.Planted = false;
        }
        attackLeftStart = joints["LeftFoot"].Bone.position; attackRightStart = joints["RightFoot"].Bone.position;
        attackLeftHandStart = leftHand.position; attackRightHandStart = rightHand.position;
        attackDropStart = lastDrop;
        attackRotationStart = transform.rotation;
        attackSpineStart = joints["Spine"].Angle; attackChestStart = joints["Chest"].Angle;
        if (actor.Team == 0 && !actor.HeavyStrike && actor.BasicComboHit == 3)
            Debug.Log("HOOK_KICK_START combo=3 right_leg=true");
    }
    private void OnUltimate(int tier)
    {
        ultimateWindup = tier == 3 ? .4f : tier == 2 ? .65f : .12f;
        attackLeftStart = joints["LeftFoot"].Bone.position; attackRightStart = joints["RightFoot"].Bone.position;
        attackDropStart = lastDrop; attackRotationStart = transform.rotation;
        attackSpineStart = joints["Spine"].Angle; attackChestStart = joints["Chest"].Angle;
    }

    private static float Ease(float start, float end, float phase) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, end, phase));

    // Preview calls the same solver as the real third-hit event. It never changes actor physics.
    public void PreviewHook(float phase)
    {
        ResetPreview();
        ApplyHookPose(Mathf.Clamp01(phase), true);
        AimHead(true);
        inPreview = false;
    }

    public void PreviewFighting(float gaitPhase = 0, float moveBlend = 0)
    {
        ResetPreview(); ApplyFightingPose(0, gaitPhase, moveBlend, true); AimHead(true); inPreview = false;
    }
    public void PreviewRoundhouse(float phase, bool left = false)
    {
        ResetPreview(); ApplyRoundhousePose(left ? "Left" : "Right", Mathf.Clamp01(phase), true); AimHead(true); inPreview = false;
    }
    public void PreviewPunch(float phase, bool right = false)
    {
        ResetPreview(); ApplyPunchPose(right ? "Right" : "Left", Mathf.Clamp01(phase), true); AimHead(true); inPreview = false;
    }
    private void ResetPreview()
    {
        inPreview = true;
        foreach (Joint j in joints.Values) { j.Angle = j.Target = 0; j.Bone.localRotation = j.Rest; }
        hips.localPosition = hipsRest; hips.localRotation = hipsRestRotation;
        fightFacing = Quaternion.LookRotation(actor.Facing);
    }
    private Vector3 ReadyFoot(string side, float movement = 0, float crouchAmount = 0)
    {
        Vector3 home = side == "Left" ? leftFootHome : rightFootHome;
        home.x *= Mathf.Lerp(Mathf.Lerp(2, 1.25f, crouchAmount), 1.55f, movement);
        home.z += (side == "Left" ? .20f : -.17f) * (1 - crouchAmount * .5f) * (1 - movement);
        return home;
    }
    private Quaternion FlatFoot(string side, Quaternion facing, float yaw = 0)
    {
        float sign = Mathf.Sign((side == "Left" ? leftFootHome : rightFootHome).x);
        return facing * Quaternion.AngleAxis(yaw + sign * 12, Vector3.up) * (side == "Left" ? leftFootBasis : rightFootBasis);
    }
    private void SetHip(float drop, float forward, float lateral)
    {
        Quaternion facing = Quaternion.LookRotation(actor.Facing);
        hips.localPosition = hipsRest + hips.parent.InverseTransformVector(Vector3.down * drop + facing * new Vector3(lateral, 0, forward));
        hips.localRotation = hipsRestRotation;
        lastDrop = drop;
    }
    private void SetGuard()
    {
        guardWeight = inPreview ? 1 : Mathf.MoveTowards(guardWeight, 1, Time.deltaTime * 6);
        Set("LeftUpperArm", -58 * guardWeight, true); Set("RightUpperArm", -58 * guardWeight, true);
        Set("LeftForearm", -100 * guardWeight, true); Set("RightForearm", -100 * guardWeight, true);
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "Left" : "Right";
            Transform hand = i == 0 ? leftHand : rightHand;
            float sign = Mathf.Sign((i == 0 ? leftFootHome : rightFootHome).x);
            Vector3 target = joints["Head"].Bone.position + model.rotation * new Vector3(sign * (ModelKind == "Deongchi" ? .22f : .14f), 0, ModelKind == "Deongchi" ? .22f : .17f);
            target = Vector3.Lerp(hand.position, target, guardWeight);
            SolveArm(side, target);
        }
        leftHand.localRotation = leftHandRest * Quaternion.AngleAxis(-hookSide * 40, leftHandAxis);
        rightHand.localRotation = rightHandRest * Quaternion.AngleAxis(hookSide * 40, rightHandAxis);
    }
    private void SolveArm(string side, Vector3 target)
    {
        Transform upper = joints[side + "UpperArm"].Bone, elbow = joints[side + "Forearm"].Bone;
        Transform hand = side == "Left" ? leftHand : rightHand;
        float sign = Mathf.Sign((side == "Left" ? leftFootHome : rightFootHome).x);
        Vector3 origin = upper.position, delta = target - origin;
        float a = Vector3.Distance(origin, elbow.position), b = Vector3.Distance(elbow.position, hand.position);
        float d = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
        Vector3 direction = delta.normalized;
        Vector3 pole = Vector3.ProjectOnPlane(model.rotation * new Vector3(sign * .35f, -1, .1f), direction).normalized;
        float along = (a * a + d * d - b * b) / (2 * d);
        Vector3 knee = origin + direction * along + pole * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
        upper.rotation = Quaternion.FromToRotation(elbow.position - origin, knee - origin) * upper.rotation;
        elbow.rotation = Quaternion.FromToRotation(hand.position - elbow.position, origin + direction * d - elbow.position) * elbow.rotation;
    }
    private void ApplyPunchPose(string side, float phase, bool preview = false)
    {
        Quaternion facing = Quaternion.LookRotation(actor.Facing);
        float sign = Mathf.Sign((side == "Left" ? leftFootHome : rightFootHome).x);
        float extension = Ease(.26f, .45f, phase) * (1 - Ease(.52f, .88f, phase));
        float anticipation = Ease(0, .25f, phase) * (1 - extension) * (1 - Ease(.75f, 1, phase));
        float readyYaw = -hookSide * 20;
        float punchYaw = side == "Right" ? -hookSide * 45 : hookSide * 8;
        Quaternion desired = facing * Quaternion.AngleAxis(Mathf.Lerp(readyYaw, punchYaw, extension), Vector3.up);
        transform.rotation = preview ? desired : Quaternion.Slerp(attackRotationStart, desired, Ease(0, .15f, phase));
        SetHip(preview ? .09f : Mathf.Lerp(attackDropStart, .09f, Ease(0, .15f, phase)),
            .035f + .055f * extension - .015f * anticipation, -sign * .025f * extension);
        Set("Spine", preview ? 6 + 4 * extension : Mathf.Lerp(attackSpineStart, 6 + 4 * extension, Ease(0, .15f, phase)), true);
        Set("Chest", 2.1f + 2 * extension, true);
        joints["Chest"].Bone.rotation = Quaternion.AngleAxis(-sign * 8 * extension, Vector3.up) * joints["Chest"].Bone.rotation;
        foreach (string leg in new[] { "Left", "Right" })
        {
            Vector3 target = actor.transform.position + facing * ReadyFoot(leg);
            if (!preview) target = Vector3.Lerp(leg == "Left" ? attackLeftStart : attackRightStart, target, Ease(0, .18f, phase));
            float pivot = leg == side ? (punchYaw - readyYaw) * extension : 0;
            SolveHookLeg(leg, target, facing * Vector3.forward, FlatFoot(leg, facing, pivot));
        }
        SetGuard();
        Transform hand = side == "Left" ? leftHand : rightHand;
        Vector3 guard = hand.position;
        Transform shoulder = joints[side + "UpperArm"].Bone, elbow = joints[side + "Forearm"].Bone;
        float reach = Vector3.Distance(shoulder.position, elbow.position) + Vector3.Distance(elbow.position, hand.position);
        Vector3 strike = shoulder.position + facing * new Vector3(-sign * .03f, .09f, reach * .98f);
        Vector3 targetHand = Vector3.Lerp(guard - actor.Facing * .025f * anticipation, strike, extension);
        if (!preview) targetHand = Vector3.Lerp(side == "Left" ? attackLeftHandStart : attackRightHandStart, targetHand, Ease(0, .15f, phase));
        SolveArm(side, targetHand);
    }
    private void AimHead(bool preview = false)
    {
        Vector3 direction = actor.Facing;
        if (!preview && Time.time >= nextFocusScan)
        { nextFocusScan = Time.time + .5f; if (focusHUD == null) focusHUD = FindFirstObjectByType<PrototypeHUD>(); }
        if (!preview && actor.Team == 0 && focusHUD != null && focusHUD.CurrentEnemy != null && actor.State != CombatActor.ActionState.ATTACK)
        {
            Vector3 toTarget = focusHUD.CurrentEnemy.transform.position - actor.transform.position; toTarget.y = 0;
            if (toTarget.sqrMagnitude > .01f && Vector3.Dot(toTarget.normalized, actor.Facing) > .25f) direction = toTarget.normalized;
        }
        // Spot the target within a human neck range, then reacquire after a full turn.
        Transform head = joints["Head"].Bone;
        head.rotation = Quaternion.RotateTowards(head.rotation, Quaternion.LookRotation(direction) * headBasis, 65);
    }
    private void ApplyFightingPose(float speed, float phase, float movement, bool preview)
    {
        bool moving = !preview && speed > .15f && actor.State == CombatActor.ActionState.IDLE && crouch < .1f;
        bool heavyBody = ModelKind == "Deongchi";
        float duty = heavyBody ? .55f : .4f;
        float stride = heavyBody ? Mathf.Clamp(.24f + visualSpeed * .12f, .24f, .5f) : Mathf.Clamp(.20f + speed * .046f, .20f, .43f);
        if (preview) stride = heavyBody ? .48f : .43f;
        if (moving && !wasMoving)
        {
            fightGait = duty * 2 * Mathf.PI * .27f;
            leftContact.Anchor = actor.transform.position + fightFacing * ReadyFoot("Left"); leftContact.Planted = true;
            rightContact.Planted = false;
        }
        if (moving) fightGait += Time.deltaTime * Mathf.PI * duty * speed / stride;
        if (!preview) phase = fightGait;
        wasMoving = moving;
        float yaw = -hookSide * 20 * (1 - movement);
        transform.rotation = fightFacing * Quaternion.AngleAxis(yaw, Vector3.up);
        float drop = Mathf.Lerp(heavyBody ? .09f : .11f, heavyBody ? .17f : .15f, movement) + (heavyBody ? .35f : .33f) * crouch;
        if (heavyBody) drop += .012f * movement * Mathf.Cos(phase * 2);
        SetHip(drop, Mathf.Lerp(.035f, 0, movement) - .10f * crouch, .018f * movement * Mathf.Sin(phase));
        float lean = (heavyBody ? 6 : 8) + (heavyBody ? 4 : 5) * movement + 18 * crouch + (actor.State == CombatActor.ActionState.HIT ? -12 : 0);
        Set("Spine", lean, true); Set("Chest", lean * .35f, true); Set("Head", -5, true); SetGuard();
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "Left" : "Right";
            FootContact contact = i == 0 ? leftContact : rightContact;
            Vector3 foot = ReadyFoot(side, movement, crouch);
            float p = Mathf.Repeat(phase + i * Mathf.PI, 2 * Mathf.PI) / (2 * Mathf.PI);
            bool planted = p < duty;
            float t = planted ? p / duty : (p - duty) / (1 - duty);
            float forward = planted ? Mathf.Lerp(stride, -stride, t) : Mathf.Lerp(-stride, stride, Mathf.SmoothStep(0, 1, t));
            foot.z += forward * movement;
            foot.y += planted ? 0 : (heavyBody ? .075f : .10f) * movement * Mathf.Sin(t * Mathf.PI);
            Vector3 target = actor.transform.position + fightFacing * foot;
            if (moving && movement > .2f && planted)
            { if (!contact.Planted) contact.Anchor = target; target = contact.Anchor; }
            contact.Planted = moving && planted;
            SolveHookLeg(side, target, fightFacing * Vector3.forward, FlatFoot(side, fightFacing));
        }
    }
    private void ApplyRoundhousePose(string leg, float phase, bool preview = false)
    {
        Quaternion facing = Quaternion.LookRotation(actor.Facing);
        float side = Mathf.Sign((leg == "Right" ? rightFootHome : leftFootHome).x);
        float lift = Ease(.18f, .38f, phase) * (1 - Ease(.76f, 1, phase));
        float extension = Ease(.36f, .45f, phase) * (1 - Ease(.60f, .76f, phase));
        float twist = Ease(.12f, .42f, phase) * (1 - Ease(.62f, 1, phase));
        float baseYaw = -hookSide * 20;
        float yaw = Mathf.Lerp(baseYaw, -side * 60, twist);
        Quaternion rotation = facing * Quaternion.AngleAxis(yaw, Vector3.up);
        transform.rotation = preview ? rotation : Quaternion.Slerp(attackRotationStart, rotation, Ease(0, .12f, phase));
        float startDrop = preview ? .11f : attackDropStart;
        SetHip(Mathf.Lerp(startDrop, .11f - .025f * extension, Ease(0, .18f, phase)), .035f + .04f * lift, -side * .025f * lift);
        Set("Spine", preview ? 8 - 14 * extension : Mathf.Lerp(attackSpineStart, 8 - 14 * extension, Ease(0, .18f, phase)), true);
        Set("Chest", preview ? 3 - 4 * extension : Mathf.Lerp(attackChestStart, 3 - 4 * extension, Ease(0, .18f, phase)), true); SetGuard();
        string supportSide = leg == "Right" ? "Left" : "Right";
        Vector3 support = actor.transform.position + facing * ReadyFoot(supportSide);
        if (!preview) support = Vector3.Lerp(supportSide == "Left" ? attackLeftStart : attackRightStart, support, Ease(0, .18f, phase));
        float pivot = -side * 110 * twist;
        SolveBlendedLeg(supportSide, support, facing * Quaternion.AngleAxis(pivot, Vector3.up) * Vector3.forward, FlatFoot(supportSide, facing, pivot), lift);
        Vector3 home = actor.transform.position + facing * ReadyFoot(leg);
        if (!preview) home = Vector3.Lerp(leg == "Left" ? attackLeftStart : attackRightStart, home, Ease(0, .18f, phase));
        Vector3 folded = actor.transform.position + facing * new Vector3(side * .20f, -.32f, .48f);
        Vector3 strike = actor.transform.position + facing * new Vector3(side * Mathf.Lerp(.18f, .06f, Ease(.45f, .60f, phase)), .20f, .90f);
        Vector3 target = Vector3.Lerp(home, Vector3.Lerp(folded, strike, extension), lift);
        Vector3 bend = Vector3.Slerp(facing * Vector3.forward, facing * Vector3.right * -side, extension);
        Quaternion pointed = facing * Quaternion.AngleAxis(side * 90, Vector3.forward) * (leg == "Right" ? rightFootBasis : leftFootBasis);
        SolveBlendedLeg(leg, target, bend, Quaternion.Slerp(FlatFoot(leg, facing), pointed, extension), lift);
    }

    private void SolveBlendedLeg(string side, Vector3 target, Vector3 bend, Quaternion footRotation, float blend)
    {
        SolveHookLeg(side, target, bend, footRotation);
        Transform thigh = joints[side + "Thigh"].Bone, shin = joints[side + "Shin"].Bone;
        Quaternion oldThigh = thigh.rotation, oldShin = shin.rotation;
        SolveFightingLeg(side, target, bend, footRotation);
        Quaternion newThigh = thigh.rotation, newShin = shin.rotation;
        thigh.rotation = Quaternion.Slerp(oldThigh, newThigh, blend);
        shin.rotation = Quaternion.Slerp(oldShin, newShin, blend);
        joints[side + "Foot"].Bone.rotation = footRotation;
    }

    private void ApplyHookPose(float phase, bool preview = false)
    {
        Quaternion facing = Quaternion.LookRotation(actor.Facing);
        float yaw = -hookSide * 360 * Ease(0, .45f, phase);
        float lift = Ease(.18f, .38f, phase) * (1 - Ease(.76f, 1, phase));
        float extension = Ease(.36f, .45f, phase) * (1 - Ease(.60f, .76f, phase));
        float blend = Ease(0, .18f, phase) * (1 - Ease(.76f, 1, phase));
        Quaternion rotation = facing * Quaternion.AngleAxis(yaw - hookSide * 20 * (1 - blend), Vector3.up);
        transform.rotation = preview ? rotation : Quaternion.Slerp(attackRotationStart, rotation, Ease(0, .12f, phase));
        float spinDrop = .025f * Mathf.Sin(Mathf.PI * Ease(0, .45f, phase));
        float drop = .11f * (1 - extension) + spinDrop;
        SetHip(preview ? drop : Mathf.Lerp(attackDropStart, drop, Ease(0, .18f, phase)), .035f, -hookSide * .025f * lift);
        float lean = extension;
        hips.localRotation = hipsRestRotation * Quaternion.AngleAxis(-14 * lean, hipsPitchAxis) *
            Quaternion.AngleAxis(hookRollSign * 30 * lean, hipsRollAxis);
        Set("Spine", preview ? 8 * (1 - extension) : Mathf.Lerp(attackSpineStart, 8 * (1 - extension), Ease(0, .18f, phase)), true);
        Set("Chest", preview ? 3 * (1 - extension) : Mathf.Lerp(attackChestStart, 3 * (1 - extension), Ease(0, .18f, phase)), true); SetGuard();

        // The support ankle remains in the same world location while the sole pivots.
        Vector3 support = actor.transform.position + facing * ReadyFoot("Left");
        if (!preview) support = Vector3.Lerp(attackLeftStart, support, Ease(0, .18f, phase));
        SolveHookLeg("Left", support, facing * Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.forward,
            FlatFoot("Left", facing, yaw));

        Vector3 home = actor.transform.position + facing * ReadyFoot("Right");
        if (!preview) home = Vector3.Lerp(attackRightStart, home, Ease(0, .18f, phase));
        Vector3 folded = actor.transform.position + facing * new Vector3(hookSide * .12f, .20f, .13f);
        float sweep = hookSide * Mathf.Lerp(.22f, .06f, Ease(.45f, .60f, phase));
        Vector3 high = actor.transform.position + facing * new Vector3(sweep, .70f, .33f);
        Vector3 target = Vector3.Lerp(home, Vector3.Lerp(folded, high, extension), lift);
        Vector3 bend = facing * Vector3.forward;
        SolveHookLeg("Right", target, bend, null);
        // Neutral ankle relative to the shin: no independent sideways ankle twist.
        Transform foot = joints["RightFoot"].Bone;
        Quaternion neutral = foot.parent.rotation * joints["RightFoot"].Rest;
        foot.rotation = Quaternion.Slerp(FlatFoot("Right", facing), neutral, lift);
    }

    private void SolveFightingLeg(string side, Vector3 target, Vector3 bend, Quaternion footRotation)
    {
        Transform thigh = joints[side + "Thigh"].Bone, shin = joints[side + "Shin"].Bone, foot = joints[side + "Foot"].Bone;
        Vector3 origin = thigh.position;
        float upper = Vector3.Distance(origin, shin.position), lower = Vector3.Distance(shin.position, foot.position);
        Vector3 delta = target - origin;
        float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + .001f, upper + lower - .001f);
        Vector3 direction = delta.normalized;
        Vector3 pole = Vector3.ProjectOnPlane(bend, direction).normalized;
        if (pole.sqrMagnitude < .01f) pole = Vector3.ProjectOnPlane(Vector3.right, direction).normalized;
        float along = (upper * upper + distance * distance - lower * lower) / (2 * distance);
        Vector3 knee = origin + direction * along + pole * Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
        Vector3 end = origin + direction * distance;
        thigh.rotation = Quaternion.LookRotation((knee - origin).normalized, pole) * Quaternion.Inverse(segmentBasis[side + "Thigh"]);
        shin.rotation = Quaternion.LookRotation((end - shin.position).normalized, pole) * Quaternion.Inverse(segmentBasis[side + "Shin"]);
        foot.rotation = footRotation;
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
    { if (actor != null) { actor.StrikeStarted -= OnStrike; actor.AttackPrepared -= OnPrepare; actor.UltimateStarted -= OnUltimate; } if (material != null) Destroy(material); }
}
