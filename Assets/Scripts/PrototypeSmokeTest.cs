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
        player = GameObject.Find("권민준").GetComponent<CombatActor>();
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

        // Physics regressions for the reported desk / standing-on-enemy defects.
        ResetActors();
        Check(Physics.GetIgnoreCollision(player.GetComponent<Collider>(), enemy.GetComponent<Collider>()),
            "Actor body collisions cannot create an enemy platform");
        bool decorationIsNonSolid = true;
        foreach (Collider shape in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            if (shape.name.StartsWith("Desk") || shape.name.StartsWith("Drawer"))
                decorationIsNonSolid &= !shape.enabled;
        Check(decorationIsNonSolid, "Desk and drawer decorations do not block movement");
        Rigidbody playerBody = player.GetComponent<Rigidbody>();
        playerBody.position = enemy.transform.position + Vector3.up * 3;
        playerBody.linearVelocity = Vector3.zero;
        Physics.SyncTransforms(); yield return new WaitForSeconds(1.5f);
        Check(player.transform.position.y < 1.2f && player.GetComponent<PlayerController>().IsGrounded,
            "Player falls to floor instead of standing on opponent");
        ResetActors(); yield return new WaitForSeconds(.15f);
        float startY = player.transform.position.y, apex = startY;
        playerBody.linearVelocity = Vector3.up * 4.4f;
        float until = Time.time + 1.2f;
        while (Time.time < until)
        { apex = Mathf.Max(apex, player.transform.position.y); yield return new WaitForFixedUpdate(); }
        Check(apex - startY > .7f && apex - startY < 1.15f,
            "Revised full jump height is approximately one world unit");

        ResetActors(); enemy.Progress.ConfirmHit(Time.time, gain: 100);
        Check(!enemy.TryUltimate() && !enemy.TryBossUltimate("forbidden"),
            "Ordinary enemies cannot use player or boss ultimates even with a full gauge");
        enemy.BossUltimateEnabled = true;
        float playerHP = player.Health.Current;
        bool bossUltimate = enemy.TryBossUltimate("주먹작렬");
        yield return new WaitForSeconds(1.7f);
        Check(bossUltimate && player.Health.Current < playerHP,
            "Authorized boss straight ultimate damages player through a real hitbox");
        enemy.BossUltimateEnabled = false;
        ResetActors(); yield return new WaitForSeconds(.15f);
        CapsuleCollider enemyShape = enemy.GetComponent<CapsuleCollider>();
        float feet = enemyShape.center.y - enemyShape.height * .5f;
        bool crouching = enemy.TryCrouch();
        Check(crouching && Mathf.Approximately(feet, enemyShape.center.y - enemyShape.height * .5f),
            "Enemy crouch changes the capsule without moving its feet");
        yield return new WaitForSeconds(.5f);
        Check(enemy.State == CombatActor.ActionState.IDLE && Mathf.Approximately(enemyShape.height, 2),
            "Enemy crouch restores its standing capsule and idle state");

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
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        enemy.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        player.SetFacing(Vector3.right); enemy.SetFacing(Vector3.left);
        Physics.SyncTransforms();
    }
    private void Check(bool condition, string name)
    {
        string result = (condition ? "PASS " : "FAIL ") + name;
        results.Add(result); if (condition) Debug.Log(result); else { failures++; Debug.LogError(result); }
    }
}
