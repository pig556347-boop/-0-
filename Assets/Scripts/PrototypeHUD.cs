using UnityEngine;

// Compatibility adapter: existing directors keep their interface; gameplay stays untouched.
public sealed class PrototypeHUD : MonoBehaviour
{
    public string StoryMessage { get; set; } = "";
    public string QuestText { get; set; } = "";
    public string EncounterText { get; set; } = "";
    private CombatHUDView view;
    private CombatActor player, enemy;
    public void SetEnemyTarget(CombatActor target) => enemy = target;
    public void Initialize(CombatActor playerActor, CombatActor enemyActor)
    {
        player = playerActor; enemy = enemyActor;
        view = gameObject.AddComponent<CombatHUDView>();
        view.Initialize();
    }
    private void LateUpdate()
    {
        if (view != null)
            view.Present(player, enemy, StoryMessage, QuestText, EncounterText,
                HitImpactManager.Instance != null && HitImpactManager.Instance.Paused);
    }
}
