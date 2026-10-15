using System.Collections.Generic;
using UnityEngine;

/// <summary>Original procedural school-uniform placeholder; no external asset licence required.</summary>
public sealed class PrototypeCharacterView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private CombatActor actor;
    private PlayerController motor;
    private Transform leftLeg, rightLeg, leftArm, rightArm;
    private readonly List<Material> materials = new List<Material>();
    private CombatActor.ActionState previous;
    private float actionStarted;
    private Quaternion baseLeftArm, baseRightArm;
    public bool CinematicPose { get; set; }
    public bool UseAuthoredMotion { get; private set; }
    public void SetForcedKick(float amount)
    {
        ImportedPlayerView imported = GetComponent<ImportedPlayerView>();
        if (imported != null && !UseAuthoredMotion) imported.ForcedKick(amount);
        AuthoredPlayerView authored = GetComponent<AuthoredPlayerView>();
        if (authored != null) authored.PreviewKick(amount);
        if (rightLeg != null && !UseAuthoredMotion) rightLeg.localRotation = Quaternion.Euler(-95 * amount, 0, 0);
    }

    public bool UseDeongchiModel()
    {
        ImportedPlayerView current = GetComponent<ImportedPlayerView>();
        if (current != null) return current.ModelKind == "Deongchi";
        if (!ImportedPlayerView.TryBuild(this, actor, "Deongchi")) return false;
        foreach (Transform child in transform)
            if (child.name != "Imported Deongchi model") { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        leftLeg = rightLeg = leftArm = rightArm = null;
        transform.localScale = Vector3.one; transform.localPosition = Vector3.zero;
        return true;
    }

    public bool UseMinion1Model()
    {
        if(GetComponent<Minion1View>()!=null)return true;
        if(!Minion1View.TryBuild(this,actor))return false;
        foreach(Transform child in transform)
            if(child.name!="Minion1 skin"){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        leftLeg=rightLeg=leftArm=rightArm=null;return true;
    }
    public void Build(CombatActor owner, bool useAuthoredMotion = true)
    {
        UseAuthoredMotion = useAuthoredMotion;
        actor = owner; motor = owner.GetComponent<PlayerController>();
        owner.gameObject.AddComponent<DefeatedBody>().Initialize(owner,transform);
        actor.StrikeStarted += OnStrike;
        actor.AttackPrepared += ()=>actionStarted=Time.time;
        if (owner.Team == 0 && ImportedPlayerView.TryBuild(this, owner)) return;
        Material uniform = MakeMaterial(new Color(.055f, .06f, .075f));
        Material white = MakeMaterial(new Color(.88f, .88f, .85f));
        Material skin = MakeMaterial(new Color(.78f, .67f, .57f));
        Material hair = MakeMaterial(new Color(.025f, .025f, .03f));
        Material accent = MakeMaterial(owner.Team == 0 ? new Color(.3f, .3f, .32f) :
            new Color(.45f, .07f, .09f));
        Part("Blazer", PrimitiveType.Cube, transform, new Vector3(0, .15f, 0),
            new Vector3(.65f, .7f, .3f), uniform);
        Part("Shirt", PrimitiveType.Cube, transform, new Vector3(0, .23f, .16f),
            new Vector3(.25f, .52f, .03f), white);
        Part("Tie", PrimitiveType.Cube, transform, new Vector3(0, .25f, .185f),
            new Vector3(.065f, .35f, .025f), accent);
        Part("Head", PrimitiveType.Sphere, transform, new Vector3(0, .73f, 0),
            new Vector3(.38f, .43f, .34f), skin);
        Part("Hair", PrimitiveType.Sphere, transform, new Vector3(0, .86f, -.025f),
            new Vector3(.41f, .25f, .36f), hair);
        Part("School bag", PrimitiveType.Cube, transform, new Vector3(0, .1f, -.26f),
            new Vector3(.47f, .55f, .18f), hair);
        leftLeg = Limb("Left leg", new Vector3(-.18f, -.2f, 0), .65f, uniform);
        rightLeg = Limb("Right leg", new Vector3(.18f, -.2f, 0), .65f, uniform);
        Part("White sneaker", PrimitiveType.Cube, leftLeg, new Vector3(0, -.7f, .06f),
            new Vector3(.22f, .16f, .35f), white);
        Part("White sneaker", PrimitiveType.Cube, rightLeg, new Vector3(0, -.7f, .06f),
            new Vector3(.22f, .16f, .35f), white);
        leftArm = Limb("Left arm", new Vector3(-.4f, .43f, 0), .55f, uniform);
        rightArm = Limb("Right arm", new Vector3(.4f, .43f, 0), .55f, uniform);
        baseLeftArm = Quaternion.Euler(0, 0, -10); baseRightArm = Quaternion.Euler(0, 0, 10);
    }
    private Material MakeMaterial(Color color)
    {
        Shader shader = Resources.Load<Shader>("Shaders/SchoolToon");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader); material.color = color;
        materials.Add(material); return material;
    }
    private Transform Limb(string name, Vector3 position, float length, Material material)
    {
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(transform, false); pivot.localPosition = position;
        Part(name + " mesh", PrimitiveType.Cube, pivot, new Vector3(0, -length * .5f, 0),
            new Vector3(.17f, length, .2f), material); return pivot;
    }
    private static void Part(string name, PrimitiveType shape, Transform parent,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape); part.name = name;
        part.transform.SetParent(parent, false); part.transform.localPosition = position;
        part.transform.localScale = scale;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) { collider.enabled = false; Destroy(collider); }
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
    private void Update()
    {
        if (actor == null || !actor.Health.Alive || leftLeg == null || CinematicPose) return;
        if(UseAuthoredMotion)
        {
            // Temporary block-character enemies have no skeleton to retarget yet.
            // Their arm pivots follow the same authoritative left/right punch selection.
            if(actor.Team==1)
            {
                if(previous!=actor.State){previous=actor.State;actionStarted=Time.time;}
                transform.rotation=Quaternion.LookRotation(actor.Facing);
                leftArm.localRotation=baseLeftArm;rightArm.localRotation=baseRightArm;
                if(actor.State==CombatActor.ActionState.ATTACK&&actor.PunchStrike)
                {
                    float elapsed=Time.time-actionStarted;
                    float extension=elapsed<actor.VisualWindup?Mathf.Clamp01(elapsed/actor.VisualWindup):
                        1-Mathf.Clamp01((elapsed-actor.VisualWindup)/actor.VisualRecovery);
                    (actor.PunchLeft?leftArm:rightArm).localRotation=Quaternion.Euler(-95*extension,0,0);
                }
            }
            return;
        }
        if (previous != actor.State) { previous = actor.State; actionStarted = Time.time; }
        transform.rotation = Quaternion.LookRotation(actor.Facing);
        float walk = motor != null && motor.CurrentState == PlayerController.PlayerState.MOVE
            ? Mathf.Sin(Time.time * 13) * 25 : 0;
        leftLeg.localRotation = Quaternion.Euler(walk, 0, 0);
        rightLeg.localRotation = Quaternion.Euler(-walk, 0, 0);
        leftArm.localRotation = baseLeftArm; rightArm.localRotation = baseRightArm;
        bool attack = actor.State == CombatActor.ActionState.ATTACK ||
            actor.State == CombatActor.ActionState.ULTIMATE;
        if (attack)
        {
            float kick = Mathf.Sin(Mathf.Clamp01((Time.time - actionStarted) / .35f) * Mathf.PI);
            if (actor.Team == 0) rightLeg.localRotation = Quaternion.Euler(-95 * kick, 0, 0);
            else if (actor.HeavyStrike || actor.State == CombatActor.ActionState.ULTIMATE)
                rightArm.localRotation = Quaternion.Euler(-90 * kick, 0, 0);
            else leftArm.localRotation = Quaternion.Euler(-90 * kick, 0, 0);
            if (actor.Team == 0) leftArm.localRotation = Quaternion.Euler(-45, 0, -15);
        }
        if (actor.State == CombatActor.ActionState.GUARD)
        { leftArm.localRotation = Quaternion.Euler(-90, 0, -15);
          rightArm.localRotation = Quaternion.Euler(-90, 0, 15); }
        // Optional imported character Animator uses the same gameplay state.
        if (animator != null)
        {
            animator.SetBool("Grounded", motor == null || motor.IsGrounded);
            animator.SetBool("Crouching", motor != null && motor.IsCrouching);
            animator.SetBool("Guarding", actor.State == CombatActor.ActionState.GUARD);
            animator.SetBool("Attacking", attack);
            animator.SetFloat("MoveSpeed", Mathf.Abs(walk) > .01f ? 1 : 0);
        }
    }
    private void OnStrike() => actionStarted = Time.time - .12f;
    private void OnDestroy()
    {
        if (actor != null) actor.StrikeStarted -= OnStrike;
        foreach (Material material in materials) Destroy(material);
    }
}
