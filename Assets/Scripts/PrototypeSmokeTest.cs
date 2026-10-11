using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Run through the editor menu; this never auto-runs during normal play.</summary>
public sealed class PrototypeSmokeTest : MonoBehaviour
{
    [System.Serializable]
    private sealed class Report
    { public string generatedUtc; public int passed; public int failed; public string[] results; }
    private readonly List<string> results = new List<string>();
    private int failures;
    private CombatActor player, enemy;

    private IEnumerator Start()
    {
        player = GameObject.Find("Sian").GetComponent<CombatActor>();
        enemy = GameObject.Find("Enemy").GetComponent<CombatActor>();
        player.GetComponent<CombatInput>().GameplayEnabled = false;
        player.GetComponent<PlayerController>().GameplayEnabled = false;
        enemy.GetComponent<EnemyBrain>().Active = false;
        ResetActors(); yield return new WaitForSeconds(.15f);

        float hp = enemy.Health.Current;
        player.TryAttack(false);
        yield return new WaitForSeconds(.75f);
        Check(enemy.Health.Current < hp, "Real hitbox damages enemy");
        Check(player.Progress.Combo == 1, "Trigger + overlap apply only one hit");

        ResetActors(); enemy.TryGuard(false);
        enemy.ReceiveHit(new HitRequest(player.gameObject, 20, Vector3.right));
        Check(Mathf.Approximately(enemy.Health.Maximum - enemy.Health.Current, 5), "Guard reduces damage to 25%");

        ResetActors(); enemy.TryGuard(true);
        bool parryHit = enemy.ReceiveHit(new HitRequest(player.gameObject, 20, Vector3.right));
        Check(!parryHit && enemy.Health.Current == enemy.Health.Maximum &&
            player.State == CombatActor.ActionState.HIT, "Timing defense prevents damage and staggers attacker");

        ResetActors(); enemy.TryDodge(Vector3.forward);
        bool dodgeHit = enemy.ReceiveHit(new HitRequest(player.gameObject, 20, Vector3.right));
        Check(!dodgeHit && enemy.Health.Current == enemy.Health.Maximum, "Dodge invulnerability prevents damage");
        yield return new WaitForSeconds(.4f);

        ResetActors(); bool grabbed = player.TryGrab();
        yield return new WaitForSeconds(.65f);
        Check(grabbed && enemy.Health.Current < enemy.Health.Maximum && enemy.transform.position.x > .4f,
            "Grab holds, damages and pushes opponent");

        ResetActors(); player.Progress.ConfirmHit(Time.time, gain: 100);
        bool ultimate = player.TryUltimate(); yield return new WaitForSeconds(1.1f);
        Check(ultimate && enemy.Health.Current < enemy.Health.Maximum && player.Progress.Gauge == 0,
            "Tier-three ultimate hits and consumes gauge");

        Report report = new Report { generatedUtc = System.DateTime.UtcNow.ToString("O"),
            passed = results.Count - failures, failed = failures, results = results.ToArray() };
        string path = Path.Combine(Application.persistentDataPath, "prototype-smoke-results.json");
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        string summary = $"Smoke checks: {report.passed} passed, {failures} failed. Report: {path}";
        if (failures == 0) Debug.Log(summary); else Debug.LogError(summary);
        GetComponent<PrototypeHUD>().QuestText = $"검증 결과: 성공 {report.passed}, 실패 {failures}";
    }
    private void ResetActors()
    {
        player.RestoreForTutorial(); enemy.RestoreForTutorial();
        player.transform.position = new Vector3(-.8f, 1.05f, -2);
        enemy.transform.position = new Vector3(.4f, 1.05f, -2);
        player.SetFacing(Vector3.right); enemy.SetFacing(Vector3.left);
        Physics.SyncTransforms();
    }
    private void Check(bool condition, string name)
    {
        string result = (condition ? "PASS " : "FAIL ") + name;
        results.Add(result); if (condition) Debug.Log(result); else { failures++; Debug.LogError(result); }
    }
}
