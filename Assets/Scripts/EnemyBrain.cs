using UnityEngine;
[RequireComponent(typeof(CombatActor))]
public sealed class EnemyBrain : MonoBehaviour
{
    public CombatActor Target { get; set; }
    public bool Active { get; set; } = true;
    private CombatActor actor;
    private Rigidbody body;
    private float nextAttack;
    private void Awake() { actor = GetComponent<CombatActor>(); body = GetComponent<Rigidbody>(); }
    private void FixedUpdate()
    {
        if (Target == null || !Target.Health.Alive || !actor.CanAct) return;
        Vector3 delta = Target.transform.position - transform.position; delta.y = 0;
        actor.SetFacing(delta);
        Vector3 direction = Active && delta.magnitude > 1.5f ? delta.normalized * 2 : Vector3.zero;
        Vector3 velocity = body.linearVelocity;
        velocity.x = direction.x; velocity.z = direction.z; body.linearVelocity = velocity;
        if (Active && delta.magnitude <= 1.6f && Time.time >= nextAttack)
        { actor.TryAttack(false); nextAttack = Time.time + 1.4f; }
    }
}
