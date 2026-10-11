using System;
using UnityEngine;

// Visual-only adapter for the user-supplied Tripo model. Gameplay owns all states.
public sealed class ImportedPlayerView : MonoBehaviour
{
    private CombatActor actor;
    private PlayerController motor;
    private PrototypeCharacterView owner;
    private Transform leftThigh, rightThigh, leftArm, rightArm, chest;
    private Quaternion leftLegRest, rightLegRest, leftArmRest, rightArmRest, chestRest;
    private Material material;
    private CombatActor.ActionState previous;
    private float actionStarted;
    public bool IsReady => leftThigh != null && rightThigh != null && leftArm != null && rightArm != null;
    public static bool TryBuild(PrototypeCharacterView owner, CombatActor actor)
    {
        GameObject source = Resources.Load<GameObject>("Characters/Player/PlayerModel");
        if (source == null) { Debug.LogError("Imported player model missing"); return false; }
        ImportedPlayerView view = owner.gameObject.AddComponent<ImportedPlayerView>();
        view.owner = owner; view.actor = actor; view.motor = actor.GetComponent<PlayerController>();
        GameObject model = Instantiate(source, owner.transform, false);
        model.name = "Imported player model";
        model.transform.localPosition = new Vector3(0, -.95f, 0);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        view.leftThigh = Find(model, "LeftThigh"); view.rightThigh = Find(model, "RightThigh");
        view.leftArm = Find(model, "LeftUpperArm"); view.rightArm = Find(model, "RightUpperArm");
        view.chest = Find(model, "Chest");
        if (!view.IsReady) { Debug.LogError("Imported player rig missing required bones"); Destroy(model); Destroy(view); return false; }
        view.leftLegRest = view.leftThigh.localRotation; view.rightLegRest = view.rightThigh.localRotation;
        view.leftArmRest = view.leftArm.localRotation; view.rightArmRest = view.rightArm.localRotation;
        if (view.chest != null) view.chestRest = view.chest.localRotation;
        Shader shader = Resources.Load<Shader>("Shaders/SchoolCharacter");
        view.material = new Material(shader);
        view.material.mainTexture = Resources.Load<Texture2D>("Characters/Player/Player_BaseColor");
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterial = view.material;
        foreach (Animator animator in model.GetComponentsInChildren<Animator>()) animator.enabled = false;
        actor.StrikeStarted += view.OnStrike;
        Debug.Log("PLAYER_MODEL_READY bones=19 source=Tripo");
        return true;
    }
    private static Transform Find(GameObject model, string name)
    {
        foreach (Transform bone in model.GetComponentsInChildren<Transform>()) if (bone.name == name) return bone;
        return null;
    }
    private void Pose(Transform bone, Quaternion rest, float degrees)
    {
        Vector3 axis = bone.parent.InverseTransformDirection(transform.right);
        bone.localRotation = Quaternion.AngleAxis(degrees, axis) * rest;
    }
    public void ForcedKick(float amount) { if (IsReady) Pose(rightThigh, rightLegRest, -95 * amount); }
    private void LateUpdate()
    {
        if (actor == null || !IsReady) return;
        transform.rotation = Quaternion.LookRotation(actor.Facing);
        if (owner.CinematicPose) return;
        if (previous != actor.State) { previous = actor.State; actionStarted = Time.time; }
        bool moving = motor != null && motor.CurrentState == PlayerController.PlayerState.MOVE;
        float walk = moving ? Mathf.Sin(Time.time * 13) * 24 : 0;
        Pose(leftThigh, leftLegRest, walk); Pose(rightThigh, rightLegRest, -walk);
        Pose(leftArm, leftArmRest, -walk * .55f); Pose(rightArm, rightArmRest, walk * .55f);
        if (chest != null) Pose(chest, chestRest, motor != null && motor.IsCrouching ? 12 : 0);
        bool attack = actor.State == CombatActor.ActionState.ATTACK || actor.State == CombatActor.ActionState.ULTIMATE;
        if (attack)
        {
            float kick = Mathf.Sin(Mathf.Clamp01((Time.time - actionStarted) / .35f) * Mathf.PI);
            Pose(rightThigh, rightLegRest, -95 * kick); Pose(leftArm, leftArmRest, -45 * kick);
        }
        if (actor.State == CombatActor.ActionState.GUARD)
        { Pose(leftArm, leftArmRest, -70); Pose(rightArm, rightArmRest, -70); }
        if (actor.State == CombatActor.ActionState.DEAD) transform.localRotation *= Quaternion.Euler(0, 0, 75);
    }
    private void OnStrike() => actionStarted = Time.time - .12f;
    private void OnDestroy()
    { if (actor != null) actor.StrikeStarted -= OnStrike; if (material != null) Destroy(material); }
}
