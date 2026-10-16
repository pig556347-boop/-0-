using System.Collections.Generic;
using SchoolRuleZero;
using UnityEngine;

public sealed class EncounterDirector : MonoBehaviour
{
    public int Stage { get; private set; } = 1;
    public int Area { get; private set; } = 1;
    private CombatActor player, practice;
    private TutorialDirector tutorial;
    private PrototypeHUD hud;
    private HitImpactManager impact;
    private CombatEffects effects;
    private readonly List<CombatActor> enemies = new List<CombatActor>();
    private readonly List<GameObject> corpses = new List<GameObject>();
    private WaveProgress wave;
    private bool started, campaignStarted, cleared, finished, dead;
    private DeongchiDuel duel;
    private float messageUntil;
    public void Initialize(CombatActor protagonist, CombatActor first, TutorialDirector lessons, PrototypeHUD ui)
    {
        player = protagonist; practice = first; tutorial = lessons; hud = ui;
        duel=gameObject.AddComponent<DeongchiDuel>();duel.Initialize(player,practice,hud);
        impact = GetComponent<HitImpactManager>(); effects = GetComponent<CombatEffects>();
        hud.EncounterText = "학교 1-1 · 교실 튜토리얼";
    }
    public bool CanAttack(CombatActor requester)
    {
        int attacking = 0;
        foreach (CombatActor enemy in enemies)
            if (enemy != null && enemy != requester && enemy.Health.Alive &&
                (enemy.State == CombatActor.ActionState.ATTACK || enemy.State == CombatActor.ActionState.GRAB ||
                 enemy.State == CombatActor.ActionState.ULTIMATE)) attacking++;
        return attacking < SchoolCampaign.AttackLimit(Stage);
    }
    private void Update()
    {
        if (tutorial == null || Time.timeScale == 0) return;
        if (!started)
        {
            if (tutorial.Progress.Step != TutorialStep.Complete||player.State==CombatActor.ActionState.ULTIMATE) return;
            started = true; tutorial.enabled = false;duel.Begin();return;
        }
        if(!campaignStarted)
        {
            if(!duel.ReadyForCampaign)return;
            campaignStarted=true;
            if (practice != null) { practice.gameObject.SetActive(false); Destroy(practice.gameObject); }
            StartArea(true);
        }
        if (!player.Health.Alive)
        {
            if (!dead) { dead = true; foreach (CombatActor e in enemies) if (e != null) e.GetComponent<EnemyBrain>().Active = false; }
            hud.StoryMessage = "쓰러졌습니다. R · 현재 라운드 다시 도전";
            if (GameKeys.Down(KeyCode.R)) StartArea(true);
            return;
        }
        if (finished)
        {
            if (GameKeys.Down(KeyCode.R)) { Stage = Area = 1; StartArea(true); }
            return;
        }
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            CombatActor enemy = enemies[i];
            if (enemy.Health.Alive) continue;
            wave.Defeat();
            // DefeatedBody settles the visible mesh on the floor and disables collision.
            enemy.GetComponent<EnemyBrain>().Active = false;
            corpses.Add(enemy.gameObject); enemies.RemoveAt(i);
        }
        corpses.RemoveAll(corpse=>corpse==null);
        SpawnAvailable();
        hud.SetEnemyTarget(enemies.Count > 0 ? enemies[0] : null);
        hud.EncounterText = $"학교 1-{Stage} · {SchoolCampaign.AreaName(Stage, Area)}\n" +
            $"라운드 {Area}/{SchoolCampaign.AreaCount(Stage)} · 남은 적 {wave.Remaining}" +
            (Stage <= 4 ? "\n매우쉬움" : "\n보통");
        if (!cleared && wave.Complete)
        {
            cleared = true;
            if (Stage == SchoolCampaign.LastStage && Area == SchoolCampaign.AreaCount(Stage))
            { finished = true; hud.StoryMessage = "황빈 제압! 1학년 완료\nR · 1-1 실전부터 다시 시작"; return; }
            hud.StoryMessage = Area == SchoolCampaign.AreaCount(Stage) ?
                $"1-{Stage} 클리어! Enter · 다음 스테이지" : "라운드 정리 완료! 오른쪽 끝 또는 Enter · 다음 라운드";
        }
        if (cleared && (GameKeys.Down(KeyCode.Return) ||
            (Area < SchoolCampaign.AreaCount(Stage) && player.transform.position.x > 11.5f)))
        {
            Area++;
            if (Area > SchoolCampaign.AreaCount(Stage)) { Stage++; Area = 1; }
            StartArea(false);
        }
        if (!cleared && Time.time > messageUntil) hud.StoryMessage = "";
    }
    private void StartArea(bool retry)
    {
        cleared = finished = dead = false;
        foreach (CombatActor enemy in enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
        foreach (GameObject corpse in corpses) if (corpse != null) { corpse.SetActive(false); Destroy(corpse); }
        enemies.Clear(); corpses.Clear();
        bool boss = SchoolCampaign.IsBossRound(Stage, Area);
        if (retry || Area == 1) player.RestoreForTutorial();
        else
        {
            player.Stagger(.05f);
            if (boss) player.Health.Heal(Mathf.Max(0, 96 - player.Health.Current));
            else player.Health.Heal(18);
        }
        player.Progress.BreakCombo();
        Rigidbody body = player.GetComponent<Rigidbody>();
        body.position = new Vector3(-7, 1.05f, -2); body.linearVelocity = Vector3.zero;
        PrototypeBootstrap.SetArena(Stage, Area, boss);
        wave = new WaveProgress(SchoolCampaign.EnemyCount(Stage, Area));
        SpawnAvailable();
        hud.SetEnemyTarget(enemies[0]); hud.QuestText = "";
        hud.StoryMessage = boss ? SchoolCampaign.BossName(Stage) + " 등장" : "잔몹 라운드 시작";
        messageUntil = Time.time + 2.5f;
    }
    private void SpawnAvailable()
    {
        bool boss = SchoolCampaign.IsBossRound(Stage, Area);
        while (wave.Spawn(SchoolCampaign.VisibleLimit(Stage)))
        {
            int index = wave.Spawned - 1;
            string name = boss ? SchoolCampaign.BossName(Stage) : "불량 학생 " + (index + 1);
            // Reuse an empty spawn point, avoiding currently occupied positions.
            Vector3 spawn = new Vector3(8, 1.05f, -5);
            float best = -1;
            for (int slot = 0; slot < 8; slot++)
            {
                Vector3 candidate = new Vector3(3 + slot % 2 * 4, 1.05f, -5 + slot / 2 * 3);
                float distance = (candidate - player.transform.position).sqrMagnitude;
                foreach (CombatActor other in enemies) distance = Mathf.Min(distance, (candidate - other.transform.position).sqrMagnitude);
                if (distance > best) { best = distance; spawn = candidate; }
            }
            CombatActor enemy = PrototypeBootstrap.CreateOpponent(name, spawn, player);
            enemy.GetComponent<DefeatedBody>().LifetimeAfterSettled=1.25f;
            if(!boss)enemy.GetComponentInChildren<PrototypeCharacterView>().UseMinion1Model("Minion"+((index+Area-1)%4+1));
            enemy.ConfigureStats(boss ? SchoolCampaign.HP(Stage, true) : SchoolCampaign.EnemyHP(Stage, index),
                SchoolCampaign.Damage(Stage, boss), Stage <= 4 ? .65f : .24f, .48f);
            enemy.TelegraphEnabled = SchoolCampaign.Telegraph(Stage);
            enemy.BossUltimateEnabled = boss;
            enemy.GrabCounterChance = boss && Stage == 11 ? .4f : 0;
            EnemyBrain brain = enemy.GetComponent<EnemyBrain>();
            brain.Director = this; brain.Difficulty = Stage; brain.IsBoss = boss;
            brain.ThinkInterval = SchoolCampaign.ThinkInterval(Stage);
            brain.AttackInterval = Stage <= 4 ? 2.4f : Stage == 11 && boss ? .9f : 1.6f;
            brain.MoveSpeed = Stage == 11 && boss ? 3.6f : Stage >= 5 ? 2.3f : 1.6f;
            enemy.HitConfirmed += impact.OnHit; enemy.HitConfirmed += effects.OnHit;
            enemies.Add(enemy);
        }
    }
}
