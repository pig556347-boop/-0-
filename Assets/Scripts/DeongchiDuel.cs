using UnityEngine;

// First real fight after the protected lessons; Enter continues to the campaign.
public sealed class DeongchiDuel : MonoBehaviour
{
    public enum DuelPhase { Waiting, Countdown, Fighting, Victory, Defeat, Complete }
    public DuelPhase Phase { get; private set; }
    public bool ReadyForCampaign => Phase==DuelPhase.Complete;
    private CombatActor player,enemy;
    private PrototypeHUD hud;
    private EnemyBrain brain;
    private float readyAt;
    public void Initialize(CombatActor protagonist,CombatActor opponent,PrototypeHUD ui)
    {player=protagonist;enemy=opponent;hud=ui;brain=enemy.GetComponent<EnemyBrain>();}
    public void Begin()
    {
        enemy.GetComponentInChildren<PrototypeCharacterView>().UseDeongchiModel();
        enemy.name="덩치";enemy.Health.Infinite=false;player.Health.Infinite=false;
        player.RestoreForTutorial();enemy.RestoreForTutorial();
        enemy.ConfigureStats(160,8,.50f,.46f);enemy.TelegraphEnabled=true;enemy.BossUltimateEnabled=false;
        player.Progress.BreakCombo();
        Place(player,new Vector3(-3,1.05f,-2),Vector3.right);
        Place(enemy,new Vector3(3,1.05f,-2),Vector3.left);
        brain.Target=player;brain.Active=false;brain.Director=null;brain.Difficulty=1;brain.IsBoss=false;
        brain.MoveSpeed=1.73f;brain.ThinkInterval=.30f;brain.AttackInterval=1.58f;
        enemy.GameplayEnabled=false;SetPlayerControls(false);
        hud.SetEnemyTarget(enemy);hud.QuestText="";hud.EncounterText="튜토리얼 실전 · 덩치와 1대1";
        hud.StoryMessage="연습은 끝났다. 이제 덩치를 쓰러뜨려 보세요!";
        Phase=DuelPhase.Countdown;readyAt=Time.time+1.5f;
        Debug.Log("DEONGCHI_DUEL Countdown hp="+enemy.Health.Maximum+" infinite="+enemy.Health.Infinite);
    }
    private static void Place(CombatActor actor,Vector3 position,Vector3 facing)
    {
        var body=actor.GetComponent<Rigidbody>();body.position=position;actor.transform.position=position;
        body.linearVelocity=Vector3.zero;actor.SetFacing(facing);
    }
    private void SetPlayerControls(bool active)
    {
        player.GameplayEnabled=active;
        var input=player.GetComponent<CombatInput>();if(input!=null)input.GameplayEnabled=active;
        var motor=player.GetComponent<PlayerController>();if(motor!=null)motor.GameplayEnabled=active;
    }
    private void Update()
    {
        if(Phase==DuelPhase.Waiting||Phase==DuelPhase.Complete||Time.timeScale==0)return;
        if(Phase==DuelPhase.Countdown)
        {
            if(Time.time<readyAt)return;
            Phase=DuelPhase.Fighting;SetPlayerControls(true);enemy.GameplayEnabled=true;brain.Active=true;
            hud.StoryMessage="";hud.QuestText="덩치를 쓰러뜨리세요. 이제 체력이 실제로 감소해요.";
            Debug.Log("DEONGCHI_DUEL Fighting");
        }
        if(Phase==DuelPhase.Fighting)
        {
            if(!player.Health.Alive)
            {Phase=DuelPhase.Defeat;brain.Active=false;StopEnemy();hud.StoryMessage="쓰러졌습니다. R · 덩치 대결 다시 도전";}
            else if(!enemy.Health.Alive)
            {
                Phase=DuelPhase.Victory;brain.Active=false;SetPlayerControls(false);
                hud.QuestText="덩치 제압 완료";hud.StoryMessage="덩치 대결 클리어!\nEnter · 1-1 실전 시작   R · 다시 대결";
                Debug.Log("DEONGCHI_DUEL Victory");
            }
        }
        if((Phase==DuelPhase.Defeat||Phase==DuelPhase.Victory)&&GameKeys.Down(KeyCode.R))Begin();
        else if(Phase==DuelPhase.Victory&&GameKeys.Down(KeyCode.Return))
        {Phase=DuelPhase.Complete;SetPlayerControls(true);hud.StoryMessage="";hud.QuestText="";Debug.Log("DEONGCHI_DUEL Complete");}
    }
    private void StopEnemy()
    {var body=enemy.GetComponent<Rigidbody>();if(!body.isKinematic){Vector3 v=body.linearVelocity;v.x=v.z=0;body.linearVelocity=v;}}
}
