using SchoolRuleZero;
using UnityEngine;

/// <summary>Four distinct opponents, one at a time, after the guided Q lesson.</summary>
public sealed class EncounterDirector : MonoBehaviour
{
    public EncounterProgress Progress { get; } = new EncounterProgress();
    private CombatActor player, current;
    private TutorialDirector tutorial;
    private PrototypeHUD hud;
    private HitImpactManager impact;
    private CombatEffects effects;
    private int opponentId;

    public void Initialize(CombatActor protagonist, CombatActor first,
        TutorialDirector lessons, PrototypeHUD ui)
    {
        player = protagonist; current = first; tutorial = lessons; hud = ui;
        impact = GetComponent<HitImpactManager>(); effects = GetComponent<CombatEffects>();
        hud.EncounterText = "불량 학생 제압 0/4\n조작 연습 후 시작";
    }
    private void Update()
    {
        if (tutorial == null || Time.timeScale == 0) return;
        if (!Progress.Active)
        {
            if (tutorial.Progress.Step != TutorialStep.Complete) return;
            // Do not count the recoverable practice opponent as a defeat.
            current.RestoreForTutorial(); Progress.Begin();
            hud.StoryMessage = "배운 동작을 활용해 불량 학생 네 명을 제압하세요.";
        }
        if (!Progress.Complete && current != null && !current.Health.Alive &&
            Progress.RecordDefeat(opponentId))
        {
            current.GetComponent<Collider>().enabled = false;
            if (!Progress.Complete)
            {
                opponentId++;
                Vector3 spawn = player.transform.position.x < 0
                    ? new Vector3(3, 1.05f, -3) : new Vector3(-3, 1.05f, -3);
                current = PrototypeBootstrap.CreateOpponent("Student " + (opponentId + 1), spawn, player);
                current.HitConfirmed += impact.OnHit;
                current.HitConfirmed += effects.OnHit;
                hud.SetEnemyTarget(current);
            }
        }
        hud.EncounterText = $"불량 학생 제압 {Progress.Count}/4" +
            (Progress.Complete ? "\n목표 달성!" : "");
        if (Progress.Complete) hud.StoryMessage = "지키고 싶은 게 생기면 나는 그냥 가만히 있을 수 없다.";
    }
}
