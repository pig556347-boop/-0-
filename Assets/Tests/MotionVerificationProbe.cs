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
            state = player.State.ToString(), combo = player.Progress.Combo
        }));
    }
    [System.Serializable] private sealed class Sample
    { public float px, pz, ex, ez; public string state; public int combo; }
}
