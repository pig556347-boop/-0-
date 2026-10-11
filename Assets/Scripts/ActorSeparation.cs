using System.Collections.Generic;
using UnityEngine;

// Body contact is planar only: actors never become floors for other actors.
// Hitboxes use overlap queries, so disabling body/body collision preserves damage.
[RequireComponent(typeof(CombatActor))]
public sealed class ActorSeparation : MonoBehaviour
{
    private static readonly List<ActorSeparation> actors = new List<ActorSeparation>();
    private CombatActor actor;
    private Rigidbody body;
    private void OnEnable()
    {
        actor = GetComponent<CombatActor>(); body = GetComponent<Rigidbody>();
        Collider own = GetComponent<Collider>();
        foreach (ActorSeparation other in actors)
            if (other != null) Physics.IgnoreCollision(own, other.GetComponent<Collider>());
        actors.Add(this);
    }
    private void FixedUpdate()
    {
        if (actor.Health == null || !actor.Health.Alive || body.isKinematic) return;
        foreach (ActorSeparation other in actors)
        {
            if (other == null || other == this || !other.actor.Health.Alive) continue;
            // Pass over an opponent during a jump; separate again on landing.
            if (Mathf.Abs(body.position.y - other.body.position.y) > .35f) continue;
            Vector3 delta = body.position - other.body.position; delta.y = 0;
            float distance = delta.magnitude;
            if (distance >= .82f) continue;
            Vector3 direction = distance > .001f ? delta / distance :
                (GetInstanceID() < other.GetInstanceID() ? Vector3.left : Vector3.right);
            Vector3 position = body.position + direction * Mathf.Min(.08f, (.82f - distance) * .5f);
            position.x = Mathf.Clamp(position.x, -13, 13);
            position.z = Mathf.Clamp(position.z, -8, 8);
            body.position = position;
        }
    }
    private void OnDisable() => actors.Remove(this);
}
