using System.Collections;
using UnityEngine;

public sealed class CombatEffects : MonoBehaviour
{
    private Material sparkMaterial;
    private Material ghostMaterial;
    private void Awake()
    {
        Shader shader = Resources.Load<Shader>("Shaders/Impact");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        sparkMaterial = new Material(shader); sparkMaterial.color = Color.red;
        ghostMaterial = new Material(shader); ghostMaterial.color = new Color(.7f, .05f, .03f, .2f);
    }
    public void OnHit(CombatActor source, CombatActor target, Vector3 position)
    {
        GameObject effect = new GameObject("Red kick impact"); effect.transform.position = position;
        ParticleSystem particles = effect.AddComponent<ParticleSystem>(); particles.Stop();
        var main = particles.main; main.loop = false; main.duration = .3f;
        main.startLifetime = .25f; main.startSpeed = 3; main.startSize = .06f;
        main.startColor = Color.red; main.gravityModifier = .8f;
        var emission = particles.emission; emission.enabled = false;
        var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .08f;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = sparkMaterial;
        particles.Play(); particles.Emit(22); Destroy(effect, .6f);
        Ghost(source.transform.position, source.Facing);
    }
    public void DodgeTrail(CombatActor source) => StartCoroutine(Trail(source));
    private IEnumerator Trail(CombatActor source)
    {
        for (int i = 0; i < 5 && source != null; i++)
        { Ghost(source.transform.position, source.Facing); yield return new WaitForSeconds(.045f); }
    }
    private void Ghost(Vector3 position, Vector3 facing)
    {
        GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ghost.name = "Red afterimage"; ghost.transform.position = position;
        ghost.transform.rotation = Quaternion.LookRotation(facing);
        ghost.transform.localScale = new Vector3(.65f, .9f, .65f);
        Collider collider = ghost.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        ghost.GetComponent<Renderer>().sharedMaterial = ghostMaterial; Destroy(ghost, .18f);
    }
    private void OnDestroy() { Destroy(sparkMaterial); Destroy(ghostMaterial); }
}
