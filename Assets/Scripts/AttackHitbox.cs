using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct HitRequest
{
    public readonly GameObject Source;
    public readonly float Damage;
    public readonly Vector3 Direction;
    public readonly bool Unblockable;
    public readonly float Knockback;
    public HitRequest(GameObject source, float damage, Vector3 direction,
        bool unblockable = false, float knockback = 4f)
    { Source = source; Damage = damage; Direction = direction;
      Unblockable = unblockable; Knockback = knockback; }
}

public interface IHitReceiver
{
    int Team { get; }
    bool ReceiveHit(HitRequest hit);
}

public sealed class AttackHitbox : MonoBehaviour
{
    private readonly Collider[] overlaps = new Collider[64];
    private readonly HashSet<IHitReceiver> struck = new HashSet<IHitReceiver>();
    private GameObject owner;
    private int team;
    private HitRequest request;
    private Vector3 halfExtents;
    private float expires;
    private Action<IHitReceiver, Vector3> confirmedHit;

    public void Configure(GameObject source, int sourceTeam, HitRequest hit,
        Vector3 size, float duration, Action<IHitReceiver, Vector3> onHit)
    {
        owner = source; team = sourceTeam; request = hit;
        halfExtents = size * .5f; expires = Time.time + duration;
        confirmedHit = onHit;
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true; box.size = size;
        // Trigger callbacks plus an overlap query catch targets already inside the box.
        // A slow rendered frame can outlast the short hit window before FixedUpdate.
        // Resolve the initial overlap immediately using the same bounds and deduplication.
        Physics.SyncTransforms();
        ScanOverlaps();
    }
    private void FixedUpdate()
    {
        if (owner == null || Time.time >= expires) { Destroy(gameObject); return; }
        ScanOverlaps();
    }
    private void ScanOverlaps()
    {
        int count = Physics.OverlapBoxNonAlloc(transform.position, halfExtents,
            overlaps, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++) TryHit(overlaps[i]);
    }
    private void OnTriggerEnter(Collider other) => TryHit(other);
    private void TryHit(Collider other)
    {
        if (owner == null || Time.time >= expires || other.transform.IsChildOf(owner.transform)) return;
        foreach (MonoBehaviour component in other.GetComponentsInParent<MonoBehaviour>())
        {
            IHitReceiver receiver = component as IHitReceiver;
            if (receiver == null || receiver.Team == team || struck.Contains(receiver)) continue;
            struck.Add(receiver);
            if (receiver.ReceiveHit(request))
                confirmedHit?.Invoke(receiver, other.ClosestPoint(transform.position));
            break;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2);
    }
}
