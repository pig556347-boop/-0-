using UnityEngine;
using UnityEngine.UI;

// Read-only UI renderer. No input remapping, gameplay state changes or camera changes.
public sealed class CombatHUDView : MonoBehaviour
{
    private RectTransform canvas;
    private Font font;
    private Sprite solid;
    private HUDHealthMeter playerMeter, enemyMeter;
    private Text playerName, enemyName, enemyRole, quest, combo, dialogue, modalTitle, modalBody;
    private Image gauge;
    private readonly Image[] orbs = new Image[3];
    private GameObject dialoguePanel, modal, comboPanel;
    public HUDHealthMeter PlayerMeter => playerMeter;
    public HUDHealthMeter EnemyMeter => enemyMeter;
    public string ComboText => combo.text;
    public string EnemyNameText => enemyName.text;
    public string EnemyRoleText => enemyRole.text;
    public string ModalText => modal.activeSelf ? modalTitle.text : "";
    public void Initialize()
    {
        font = Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        solid = Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f));
        GameObject root = new GameObject("Combat HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        root.transform.SetParent(transform,false); canvas = (RectTransform)root.transform;
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        RectTransform p = Panel("Player panel",canvas,new Vector2(.02f,.815f),new Vector2(.34f,.98f));
        playerName = Label("Player name",p,new Vector2(.04f,.72f),new Vector2(.94f,.94f),28,TextAnchor.MiddleLeft);
        playerMeter = Meter("Player HP",p,.4f,.62f);
        Label("Special label",p,new Vector2(.04f,.05f),new Vector2(.22f,.24f),18,TextAnchor.MiddleLeft).text="특수 게이지";
        Image track = MakeImage("Ultimate track",p,new Vector2(.24f,.11f),new Vector2(.75f,.17f),new Color(1,1,1,.12f));
        gauge = MakeImage("Ultimate fill",track.rectTransform,Vector2.zero,Vector2.one,new Color(.72f,.73f,.7f));
        gauge.type=Image.Type.Filled; gauge.fillMethod=Image.FillMethod.Horizontal;
        for(int i=0;i<3;i++) orbs[i]=MakeImage("Ultimate charge "+(i+1),p,
            new Vector2(.8f+i*.05f,.09f),new Vector2(.83f+i*.05f,.19f),new Color(1,1,1,.2f));
        RectTransform e = Panel("Enemy panel",canvas,new Vector2(.365f,.85f),new Vector2(.655f,.98f));
        enemyRole = Label("Enemy role",e,new Vector2(.04f,.73f),new Vector2(.96f,.96f),17,TextAnchor.MiddleLeft);
        enemyName = Label("Enemy name",e,new Vector2(.04f,.46f),new Vector2(.96f,.75f),28,TextAnchor.MiddleLeft);
        enemyMeter = Meter("Enemy HP",e,.13f,.36f);
        RectTransform q=Panel("Objective panel",canvas,new Vector2(.68f,.785f),new Vector2(.98f,.98f));
        quest=Label("Objective",q,new Vector2(.05f,.06f),new Vector2(.95f,.95f),22,TextAnchor.UpperLeft);
        RectTransform c=Panel("Combo panel",canvas,new Vector2(.02f,.105f),new Vector2(.19f,.20f));
        comboPanel=c.gameObject;
        combo=Label("Combo",c,new Vector2(.07f,.12f),new Vector2(.96f,.92f),36,TextAnchor.MiddleLeft);
        RectTransform keys=Panel("Key guide",canvas,new Vector2(.02f,.02f),new Vector2(.98f,.09f));
        Label("Current controls",keys,new Vector2(.015f,.1f),new Vector2(.985f,.9f),22,TextAnchor.MiddleCenter).text=
            "WASD 이동   Space 점프   C 앉기   Shift+방향 회피\nJ 기본공격   K 강공격   L 가드/타이밍 방어   I 잡기   Q 궁극기   Esc 일시정지";
        RectTransform d=Panel("Dialogue panel",canvas,new Vector2(.23f,.135f),new Vector2(.77f,.29f));
        dialoguePanel=d.gameObject;
        dialogue=Label("Dialogue",d,new Vector2(.035f,.10f),new Vector2(.965f,.90f),28,TextAnchor.MiddleCenter);
        RectTransform m=Panel("State overlay",canvas,new Vector2(.285f,.35f),new Vector2(.715f,.65f));
        modal=m.gameObject;
        modalTitle=Label("State title",m,new Vector2(.07f,.5f),new Vector2(.93f,.86f),42,TextAnchor.MiddleCenter);
        modalBody=Label("State instructions",m,new Vector2(.07f,.1f),new Vector2(.93f,.46f),24,TextAnchor.MiddleCenter);
        dialoguePanel.SetActive(false); modal.SetActive(false); comboPanel.SetActive(false);
    }
    public void Present(CombatActor player,CombatActor enemy,string story,string tutorial,string encounter,bool paused)
    {
        if(player==null) return;
        playerName.text=player.name;
        playerMeter.Bind(player); enemyMeter.Bind(enemy);
        bool boss=enemy!=null && enemy.BossUltimateEnabled;
        enemyRole.text=enemy==null ? "대상 없음" : boss ? "BOSS · 간부 대결" : "TARGET";
        enemyRole.color=boss ? new Color(.96f,.62f,.59f) : new Color(.72f,.74f,.75f);
        enemyName.text=enemy==null ? "—" : enemy.name;
        gauge.fillAmount=Mathf.Clamp01(player.Progress.Gauge/100);
        for(int i=0;i<3;i++) orbs[i].color=player.Progress.UltimateTier>i ? new Color(.9f,.9f,.86f) : new Color(1,1,1,.18f);
        quest.text=encounter+(string.IsNullOrEmpty(tutorial)?"":"\n\n"+tutorial);
        int hits=player.Progress.Combo;
        comboPanel.SetActive(hits>0); combo.text=hits>0 ? hits+" HIT"+(hits>=6?"  GOOD!":"") : "";
        bool dead=!player.Health.Alive;
        bool complete=story.Contains("1학년 완료");
        bool clear=complete || story.Contains("클리어!") || story.Contains("라운드 정리 완료!");
        modal.SetActive(paused||dead||clear);
        modalTitle.text=paused?"일시정지":dead?"쓰러졌다":complete?"1학년 완료":clear?"전투 완료":"";
        modalTitle.color=dead ? new Color(.94f,.38f,.36f) : Color.white;
        modalBody.text=paused?"Esc · 계속하기":dead?"R · 현재 라운드 다시 도전":clear?story:"";
        dialoguePanel.SetActive(!paused&&!dead&&!clear&&!string.IsNullOrEmpty(story)); dialogue.text=story;
    }
    private HUDHealthMeter Meter(string name,RectTransform parent,float bottom,float top)
    {
        RectTransform panel=Rect(name,parent,new Vector2(.04f,bottom),new Vector2(.96f,top));
        Image border=MakeImage(name+" outline",panel,Vector2.zero,Vector2.one,new Color(1,1,1,.19f));
        Image bg=MakeImage(name+" track",panel,new Vector2(.006f,.13f),new Vector2(.69f,.87f),new Color(.12f,.13f,.15f,.96f));
        Image fill=MakeImage(name+" fill",bg.rectTransform,new Vector2(.008f,.2f),new Vector2(.992f,.8f),Color.white);
        fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;
        Text number=Label(name+" value",panel,new Vector2(.70f,0),new Vector2(.99f,1),21,TextAnchor.MiddleRight);
        HUDHealthMeter meter=panel.gameObject.AddComponent<HUDHealthMeter>();meter.Initialize(fill,border,number);return meter;
    }
    private RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max)
    {
        RectTransform r=Rect(name,parent,min,max);
        MakeImage(name+" edge",r,Vector2.zero,Vector2.one,new Color(1,1,1,.16f));
        RectTransform inset=Rect(name+" surface",r,Vector2.zero,Vector2.one);
        inset.offsetMin=new Vector2(1,1);inset.offsetMax=new Vector2(-1,-1);
        Image surface=inset.gameObject.AddComponent<Image>();surface.color=new Color(.045f,.05f,.06f,.72f);surface.raycastTarget=false;
        return r;
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {
        GameObject go=new GameObject(name,typeof(RectTransform));RectTransform r=(RectTransform)go.transform;
        r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;
    }
    private Image MakeImage(string name,Transform parent,Vector2 min,Vector2 max,Color color)
    {
        RectTransform r=Rect(name,parent,min,max);Image im=r.gameObject.AddComponent<Image>();
        im.sprite=solid;im.color=color;im.raycastTarget=false;return im;
    }
    private Text Label(string name,Transform parent,Vector2 min,Vector2 max,int size,TextAnchor align)
    {
        RectTransform r=Rect(name,parent,min,max);Text t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;
        t.resizeTextForBestFit=true;t.resizeTextMinSize=14;t.resizeTextMaxSize=size;
        t.alignment=align;t.color=new Color(.93f,.94f,.94f);t.raycastTarget=false;return t;
    }
    private void OnDestroy(){Destroy(solid);}
}
