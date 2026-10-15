using SchoolRuleZero;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialCoach : MonoBehaviour
{
    private TutorialDirector director;
    private CombatActor player;
    private GameObject panel;
    private Text title,body,keys,hint;
    private Image accent;
    private TutorialStep previous=TutorialStep.Opening;
    private float successUntil,blockedUntil,completeUntil;
    private static readonly string[] KeyLabels={"","W A S D","SPACE","C","WASD + J","WASD + K","L","L","SHIFT\n+ WASD","WASD + I","J / K","WASD + Q",""};
    private static readonly string[] Instructions={"",
        "WASD로 교실 바닥을 따라 이동해보세요.\n조금만 걸어가면 성공이에요.",
        "스페이스바를 눌러 점프해보세요!\n착지하면 다음 연습으로 넘어가요.",
        "C를 0.6초 동안 누르고 있어보세요.\n키를 놓으면 다시 일어나요.",
        "가까이 다가가 J를 연속으로 눌러보세요.\n공격이 끝나기 전에 이어 눌러 3타를 맞혀요.",
        "WASD로 다가가 K로 강공격을 맞혀보세요.\n준비 동작이 길지만 강하게 밀어낼 수 있어요.",
        "상대를 바라보며 L을 누르고 있어보세요.\n정면 공격을 한 번 막으면 성공이에요.",
        "공격이 닿기 직전에 L을 눌러보세요.\n미리 누르지 말고 공격 예고를 보고 눌러요.",
        "Shift와 WASD를 함께 눌러 피해보세요.\n방향을 누른 채 Shift를 눌러도 돼요.",
        "상대 가까이에서 I를 눌러 잡아보세요.\n거리가 멀다면 WASD로 더 다가가세요.",
        "J 또는 K로 공격해 구슬 3개를 채워보세요.\nWASD로 거리를 맞추고 공격을 적중시켜요.",
        "상대 가까이에서 Q를 눌러보세요.\n구슬 3개로 6연타 궁극기를 적중시키세요.\n빗나가면 게이지를 다시 채워드려요.",
        "모든 조작을 배웠어요. 자유롭게 연습해보세요!"};
    private static readonly KeyCode[] CheckKeys={KeyCode.W,KeyCode.A,KeyCode.S,KeyCode.D,KeyCode.Space,KeyCode.C,
        KeyCode.J,KeyCode.K,KeyCode.L,KeyCode.I,KeyCode.Q,KeyCode.LeftShift,KeyCode.RightShift};
    public void Initialize(TutorialDirector lesson,CombatActor actor)
    {
        director=lesson;player=actor;
        var root=new GameObject("Tutorial Coach",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(transform,false);
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=10;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        panel=GameLobby.Rect("Current lesson",root.transform,new Vector2(.60f,.48f),new Vector2(.98f,.77f)).gameObject;
        panel.AddComponent<Image>().color=new Color(.035f,.035f,.04f,.92f);
        var edge=panel.AddComponent<Outline>();edge.effectColor=new Color(.7f,.68f,.6f,.6f);edge.effectDistance=new Vector2(1,-1);
        accent=GameLobby.Rect("Lesson accent",panel.transform,Vector2.zero,new Vector2(.007f,1)).gameObject.AddComponent<Image>();
        title=Text("Lesson",new Vector2(.04f,.76f),new Vector2(.75f,.97f),22);
        body=Text("Instructions",new Vector2(.04f,.28f),new Vector2(.75f,.76f),25);
        keys=Text("Allowed keys",new Vector2(.77f,.28f),new Vector2(.98f,.85f),32);keys.alignment=TextAnchor.MiddleCenter;
        hint=Text("Feedback",new Vector2(.04f,.035f),new Vector2(.98f,.25f),17);
    }
    private Text Text(string name,Vector2 min,Vector2 max,int size)
    {
        var text=GameLobby.Rect(name,panel.transform,min,max).gameObject.AddComponent<Text>();
        text.font=Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular");
        if(text.font==null)text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize=size;text.color=new Color(.94f,.93f,.89f);text.alignment=TextAnchor.MiddleLeft;
        text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=size;text.raycastTarget=false;return text;
    }
    private void LateUpdate()
    {
        if(director==null||!director.enabled){panel.SetActive(false);return;}var step=director.Progress.Step;
        if(previous!=step)
        {
            if(previous!=TutorialStep.Opening)
            {
                successUntil=Time.time+.65f;
                if(GameAudioChannels.Instance!=null)GameAudioChannels.Instance.Play("Success");
            }
            if(step==TutorialStep.Complete)completeUntil=Time.time+4;
            previous=step;
        }
        panel.SetActive(step!=TutorialStep.Opening&&(step!=TutorialStep.Complete||Time.time<completeUntil));
        if(!panel.activeSelf)return;
        foreach(var key in CheckKeys)
            if(GameKeys.Down(key)&&!TutorialInputGate.Allows(step,key))blockedUntil=Time.time+1.2f;
        title.text=step==TutorialStep.Complete?"튜토리얼 완료":$"조작 연습  {(int)step:00} / 12";
        body.text=Instructions[(int)step];keys.text=KeyLabels[(int)step];
        hint.text=Time.time<blockedUntil?"지금은 표시된 키로 먼저 연습해보세요.":
            Time.time<successUntil?"잘했어요! 다음 연습으로 넘어갈게요.":
            step==TutorialStep.Basic?$"연속 적중 {director.BasicHits}/3":
            step==TutorialStep.Crouch?$"C  {director.CrouchPracticeTime:0.0} / 0.6 s":
            step==TutorialStep.Charge?$"Q  {player.Progress.UltimateTier} / 3":"현재 연습에 필요한 키만 사용할 수 있어요.";
        CombatActor enemy=director.TrainingOpponent;
        if(step==TutorialStep.Parry&&enemy!=null&&enemy.WarningUntil>Time.time)
            hint.text=enemy.WarningUntil-Time.time<.13f ? "지금 L!" : "공격 예고를 보고 기다렸다가 L을 눌러요.";
        accent.color=Time.time<blockedUntil?new Color(.95f,.65f,.25f):Time.time<successUntil?new Color(.45f,.8f,.6f):new Color(.8f,.76f,.65f);
    }
}
