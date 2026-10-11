using UnityEngine;
using UnityEngine.UI;

public sealed class PrototypeHUD : MonoBehaviour
{
    public string StoryMessage { get; set; } = "";
    public string QuestText { get; set; } = "";
    public string EncounterText { get; set; } = "";
    public void SetEnemyTarget(CombatActor target) => enemy = target;
    private CombatActor player, enemy;
    private Image hp, gauge, enemyHP;
    private readonly Image[] orbs = new Image[3];
    private Text hpLabel, enemyLabel, combo, quest, story, pause;
    private Font font;
    private Sprite orbSprite;
    private Sprite barSprite;
    private Texture2D orbTexture;
    private Transform canvas;

    public void Initialize(CombatActor playerActor, CombatActor enemyActor)
    {
        player = playerActor; enemy = enemyActor;
        font = Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        GameObject root = new GameObject("Game HUD", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler)); root.transform.SetParent(transform, false);
        Canvas ui = root.GetComponent<Canvas>(); ui.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        canvas = root.transform;
        barSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f));
        Label("Title", "교칙 제0조  |  시안", new Vector2(32, -24), new Vector2(420, 40), 28);
        hp = Bar("HP", new Vector2(32, -75), new Vector2(360, 25), new Color(.8f, .15f, .18f));
        hpLabel = Label("HP value", "", new Vector2(32, -106), new Vector2(360, 32), 22);
        gauge = Bar("Ultimate gauge", new Vector2(32, -147), new Vector2(360, 18), new Color(.9f, .6f, .2f));
        MakeOrbSprite();
        for (int i = 0; i < 3; i++)
        {
            orbs[i] = ImageAt("Ultimate orb " + (i + 1), new Vector2(32 + i * 42, -181),
                new Vector2(30, 30), Color.gray, canvas);
            orbs[i].sprite = orbSprite;
        }
        enemyHP = Bar("Enemy HP", new Vector2(730, -35), new Vector2(360, 20), new Color(.65f, .65f, .65f));
        enemyLabel = Label("Enemy label", "", new Vector2(730, -65), new Vector2(500, 35), 24);
        quest = Label("Quest", "", new Vector2(1370, -35), new Vector2(520, 240), 25);
        combo = Label("Combo popup", "", new Vector2(32, -800), new Vector2(500, 110), 42);
        story = Label("Story", "", new Vector2(320, -740), new Vector2(1300, 170), 32);
        pause = Label("Pause", "", new Vector2(690, -400), new Vector2(700, 180), 40);
        Label("Controls", "WASD 이동 · Space 점프 · C 유지 앉기 · Shift+방향 회피\n" +
            "J 연속공격 · K 강공격 · L 가드/타이밍 방어 · I 잡기 · Q 궁극기 · Esc 일시정지",
            new Vector2(32, -978), new Vector2(1800, 90), 24);
    }
    private Image Bar(string name, Vector2 position, Vector2 size, Color color)
    {
        Image background = ImageAt(name + " background", position, size,
            new Color(.08f, .08f, .1f, .9f), canvas);
        Image fill = ImageAt(name + " fill", Vector2.zero, size, color, background.transform);
        fill.sprite = barSprite;
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0; return fill;
    }
    private static RectTransform Rect(GameObject go, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static Image ImageAt(string name, Vector2 position, Vector2 size, Color color, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Rect(go, parent, position, size); Image image = go.GetComponent<Image>();
        image.color = color; image.raycastTarget = false; return image;
    }
    private Text Label(string name, string content, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        Rect(go, canvas, position, size); Text text = go.GetComponent<Text>();
        text.font = font; text.fontSize = fontSize; text.color = Color.white;
        text.text = content; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; return text;
    }
    private void MakeOrbSprite()
    {
        orbTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            orbTexture.SetPixel(x, y, new Vector2(x - 15.5f, y - 15.5f).sqrMagnitude <= 225
                ? Color.white : Color.clear);
        orbTexture.Apply(); orbSprite = Sprite.Create(orbTexture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f));
    }
    private void Update()
    {
        if (player == null || hp == null) return;
        hp.fillAmount = player.Health.Current / player.Health.Maximum;
        gauge.fillAmount = player.Progress.Gauge / 100;
        hpLabel.text = $"HP {player.Health.Current:0} / {player.Health.Maximum:0}";
        for (int i = 0; i < 3; i++) orbs[i].color = player.Progress.UltimateTier > i
            ? new Color(1, .25f, .15f) : new Color(.3f, .3f, .3f);
        if (enemy != null)
        {
            enemyHP.fillAmount = enemy.Health.Current / enemy.Health.Maximum;
            enemyLabel.text = $"상대  {enemy.Health.Current:0} / {enemy.Health.Maximum:0}";
        }
        int hits = player.Progress.Combo;
        combo.text = hits > 0 ? $"{hits} HIT" + (hits >= 6 ? " / GOOD!" : "") : "";
        combo.color = new Color(1, .3f, .2f, Mathf.Clamp01(player.Progress.ComboExpires - Time.time));
        quest.text = (EncounterText.Length > 0 ? EncounterText + "\n\n" : "") + QuestText;
        story.text = StoryMessage;
        pause.text = HitImpactManager.Instance != null && HitImpactManager.Instance.Paused
            ? "일시정지\nEsc를 눌러 계속" : "";
    }
    private void OnDestroy() { Destroy(orbSprite); Destroy(orbTexture); Destroy(barSprite); }
}
