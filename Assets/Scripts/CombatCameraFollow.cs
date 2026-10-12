using UnityEngine;

// Follow the actor independently of the camera's local hit-shake offset.
public sealed class CombatCameraFollow : MonoBehaviour
{
    private CombatActor player;
    private Vector3 velocity;
    public void Initialize(CombatActor actor)
    {
        player = actor;
        transform.position = Focus();
    }
    private Vector3 Focus()
    {
        Vector3 point = player.transform.position + player.Facing * .7f;
        point.x = Mathf.Clamp(point.x, -10, 10);
        point.z = Mathf.Clamp(point.z, -6, 6);
        point.y = .8f;
        return point;
    }
    private void LateUpdate()
    {
        if (player == null || Time.timeScale == 0) return;
        transform.position = Vector3.SmoothDamp(transform.position, Focus(), ref velocity, .18f,
            Mathf.Infinity, Time.deltaTime);
    }
}
