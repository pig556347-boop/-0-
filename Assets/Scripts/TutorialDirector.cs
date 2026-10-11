using System.Collections;
using SchoolRuleZero;
using UnityEngine;

public sealed class TutorialDirector : MonoBehaviour
{
    public TutorialProgress Progress { get; } = new TutorialProgress();
    private CombatActor player, enemy;
    private PlayerController motor;
    private EnemyBrain brain;
    private PrototypeHUD hud;
    private Vector3 moveStart;
    private GameObject openingDesk;
    private PrototypeCharacterView openingView;
    private Vector3 openingVisualHome;
    private readonly TutorialComboLesson comboLesson = new TutorialComboLesson();
    private readonly string[] instructions =
    {
        "", "WASD로 바닥을 따라 이동하세요", "Space를 눌러 점프하세요",
        "C를 유지해 앉으세요", "J를 연속 입력해 기본공격 3회를 이어서 적중시키세요",
        "K로 강공격을 적중시키세요", "L을 유지해 정면 공격을 막으세요",
        "공격이 닿기 직전에 L을 눌러 타이밍 방어를 하세요",
        "Shift와 WASD를 함께 눌러 회피하세요", "가까이에서 I로 상대를 잡아 밀어내세요",
        "공격을 적중시켜 궁극기 구슬 3개를 채우세요",
        "상대 가까이에서 Q로 천파일격을 적중시키세요", "튜토리얼 완료! 자유롭게 전투를 연습하세요"
    };
    public void Initialize(CombatActor protagonist, CombatActor opponent, PrototypeHUD ui)
    {
        player = protagonist; enemy = opponent; hud = ui;
        motor = player.GetComponent<PlayerController>(); brain = enemy.GetComponent<EnemyBrain>();
        moveStart = player.transform.position;
        player.AttackLanded += heavy =>
        {
            if (heavy) { comboLesson.Break(); Signal(TutorialSignal.HeavyHit); }
            else if (Progress.Step == TutorialStep.Basic && comboLesson.ConfirmBasicHit(Time.time))
                Signal(TutorialSignal.BasicHit);
        };
        player.Damaged += result =>
        {
            if (result.Kind == DamageKind.Hit) comboLesson.Break();
            if (result.Kind == DamageKind.Guarded) Signal(TutorialSignal.Guarded);
            if (result.Kind == DamageKind.Parried) Signal(TutorialSignal.Parried);
        };
        player.Dodged += () => Signal(TutorialSignal.Dodged);
        player.Grabbed += () => Signal(TutorialSignal.Grabbed);
        player.UltimateLanded += tier => { if (tier == 3) Signal(TutorialSignal.UltimateTierThreeHit); };
        StartCoroutine(Opening());
    }
    private void Signal(TutorialSignal signal)
    {
        if (!Progress.Accept(signal)) return;
        if (Progress.Step != TutorialStep.Complete) enemy.RestoreForTutorial();
    }
    private void Update()
    {
        if (player == null || Time.timeScale == 0) return;
        TutorialStep step = Progress.Step;
        comboLesson.Tick(Time.time);
        if (step == TutorialStep.Opening) { brain.Active = false; return; }
        Vector3 moved = player.transform.position - moveStart; moved.y = 0;
        if (step == TutorialStep.Move && moved.magnitude > 2)
            Signal(TutorialSignal.Moved);
        if (step == TutorialStep.Jump && motor.CurrentState == PlayerController.PlayerState.JUMP)
            Signal(TutorialSignal.Jumped);
        if (step == TutorialStep.Crouch && motor.IsCrouching) Signal(TutorialSignal.Crouched);
        if (step == TutorialStep.Charge && player.Progress.UltimateTier == 3) Signal(TutorialSignal.FullyCharged);
        bool training = Progress.Step != TutorialStep.Complete;
        brain.Active = Progress.Step == TutorialStep.Guard || Progress.Step == TutorialStep.Parry || !training;
        if (training && !enemy.Health.Alive) enemy.RestoreForTutorial();
        if (training && !player.Health.Alive) player.RestoreForTutorial();
        hud.QuestText = Progress.Step == TutorialStep.Complete ? "" :
            "조작 연습\n" + instructions[(int)Progress.Step] +
            (Progress.Step == TutorialStep.Basic ? $"\n연속 적중 {Mathf.Min(comboLesson.Hits, 3)}/3" : "");
    }
    private IEnumerator Opening()
    {
        CombatInput input = player.GetComponent<CombatInput>();
        PrototypeCharacterView view = player.GetComponentInChildren<PrototypeCharacterView>();
        openingView = view;
        input.GameplayEnabled = false; motor.GameplayEnabled = false;
        player.GameplayEnabled = false; enemy.GameplayEnabled = false; brain.Active = false;
        view.CinematicPose = true;
        Vector3 home = view.transform.localPosition;
        openingVisualHome = home;
        Vector3 enemyHome = enemy.transform.position;
        GameObject desk = PrototypeBootstrap.CreateBox("Opening desk",
            player.transform.position + new Vector3(0, -.3f, .6f),
            new Vector3(1.6f, .15f, .9f), new Color(.5f, .45f, .38f));
        openingDesk = desk;
        view.transform.localPosition = home + new Vector3(0, -.1f, .2f);
        view.transform.localRotation = Quaternion.Euler(65, 0, 0);
        hud.StoryMessage = "그냥 조용히 지내고 싶을 뿐이야.";
        yield return new WaitForSeconds(2);
        enemy.transform.position = player.transform.position + new Vector3(1.5f, 0, .2f);
        enemy.SetFacing(Vector3.left);
        hud.StoryMessage = "야, 전학생. 여기서 자냐?";
        yield return new WaitForSeconds(.8f);
        Collider deskCollider = desk.GetComponent<Collider>(); deskCollider.enabled = false;
        Rigidbody deskBody = desk.AddComponent<Rigidbody>(); deskBody.mass = 2;
        deskBody.AddForce(new Vector3(-3, 2, 2), ForceMode.Impulse);
        deskBody.AddTorque(new Vector3(2, 0, 3), ForceMode.Impulse);
        hud.StoryMessage = "책상이 걷어차였다.";
        yield return new WaitForSeconds(.7f);
        view.transform.localPosition = home;
        view.transform.localRotation = Quaternion.LookRotation(Vector3.right);
        hud.StoryMessage = "…그만해.";
        float started = Time.time;
        while (Time.time - started < .5f)
        {
            view.SetForcedKick(Mathf.Sin((Time.time - started) / .5f * Mathf.PI));
            yield return null;
        }
        view.SetForcedKick(0); view.CinematicPose = false;
        enemy.transform.position = enemyHome; Destroy(desk);
        hud.StoryMessage = ""; moveStart = player.transform.position;
        player.GameplayEnabled = true; enemy.GameplayEnabled = true;
        input.GameplayEnabled = true; motor.GameplayEnabled = true; Progress.FinishOpening();
    }
    private void OnDisable()
    {
        if (player == null) return;
        StopAllCoroutines();
        if (openingDesk != null) Destroy(openingDesk);
        if (openingView != null)
        { openingView.transform.localPosition = openingVisualHome; openingView.SetForcedKick(0); }
        player.GameplayEnabled = true;
        if (enemy != null) enemy.GameplayEnabled = true;
        player.GetComponent<CombatInput>().GameplayEnabled = true; motor.GameplayEnabled = true;
        PrototypeCharacterView view = player.GetComponentInChildren<PrototypeCharacterView>();
        if (view != null) view.CinematicPose = false;
    }
}
