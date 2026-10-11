using UnityEngine;
using UnityEngine.UI;

// Small ratio-only overhead bar; follows the body without rotating with attacks.
public sealed class EnemyHealthBar : MonoBehaviour
{
    private CombatActor actor;
    private GameObject root;
    private Image fill;
    private Text warning;
    private Sprite sprite;
    private void Start()
    {
        actor = GetComponent<CombatActor>();
        root = new GameObject("Enemy health", typeof(RectTransform), typeof(Canvas));
        root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 10); rect.localScale = Vector3.one * .014f;
        sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), new Vector2(.5f,.5f));
        MakeImage("Background", root.transform, new Color(.05f,.05f,.07f,.9f));
        fill = MakeImage("Remaining", root.transform, new Color(.9f,.22f,.2f));
        fill.sprite = sprite; fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0;
        GameObject label = new GameObject("Attack warning", typeof(RectTransform), typeof(Text));
        RectTransform labelRect = label.GetComponent<RectTransform>(); labelRect.SetParent(root.transform, false);
        labelRect.sizeDelta = new Vector2(250, 35); labelRect.anchoredPosition = new Vector2(0, 25);
        warning = label.GetComponent<Text>(); warning.font = Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular");
        if (warning.font == null) warning.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        warning.fontSize = 23; warning.alignment = TextAnchor.MiddleCenter;
        warning.color = new Color(1, .8f, .1f); warning.raycastTarget = false;
    }
    private Image MakeImage(string label, Transform parent, Color color)
    {
        GameObject obj = new GameObject(label, typeof(RectTransform), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        return image;
    }
    private void LateUpdate()
    {
        if (root == null || actor == null) return;
        root.SetActive(actor.Health.Alive);
        root.transform.position = transform.position + Vector3.up * 1.5f;
        if (Camera.main != null) root.transform.rotation = Camera.main.transform.rotation;
        fill.fillAmount = actor.Health.Current / actor.Health.Maximum;
        warning.text = Time.time < actor.WarningUntil ? actor.WarningText : "";
    }
    private void OnDestroy() { Destroy(root); Destroy(sprite); }
}
