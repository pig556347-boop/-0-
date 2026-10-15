using UnityEngine;

// Read-only telemetry, enabled only by the automated browser's verification URL.
public sealed class MotionVerificationProbe : MonoBehaviour
{
    private CombatActor player, enemy;
    private float next;
    public static void Install(CombatActor player, CombatActor enemy)
    {
        if (!Application.absoluteURL.Contains("motionVerification=1")) return;
        MotionVerificationProbe probe = player.gameObject.AddComponent<MotionVerificationProbe>();
        probe.player = player; probe.enemy = enemy;
    }
    private void Update()
    {
        if (Time.unscaledTime < next || player == null || enemy == null) return;
        next = Time.unscaledTime + .1f;
        Debug.Log("MOTION_LIVE " + JsonUtility.ToJson(new Sample {
            px = player.transform.position.x, pz = player.transform.position.z,
            ex = enemy.transform.position.x, ez = enemy.transform.position.z,
            state = player.State.ToString(), combo = player.Progress.Combo,
            jumpStage = player.GetComponentInChildren<AuthoredPlayerView>() == null ? "Grounded" :
                player.GetComponentInChildren<AuthoredPlayerView>().JumpStage,
            visualMotion = player.GetComponentInChildren<AuthoredPlayerView>() == null ? "legacy" :
                player.GetComponentInChildren<AuthoredPlayerView>().CurrentMotion,
            sx = Camera.main.WorldToViewportPoint(player.transform.position).x,
            sy = Camera.main.WorldToViewportPoint(player.transform.position).y,
            cameraX = Camera.main.transform.parent.position.x, cameraY = Camera.main.transform.parent.position.y,
            cameraZ = Camera.main.transform.parent.position.z, cameraSize = Camera.main.orthographicSize
        }));
    }
    [System.Serializable] private sealed class Sample
    { public float px, pz, ex, ez, sx, sy, cameraSize, cameraX, cameraY, cameraZ; public string state, visualMotion, jumpStage; public int combo; }
}
