using UnityEngine;
using UnityEngine.UI;
using SchoolRuleZero;

// Presentation-only health animation. Never writes actor health or transform.
public sealed class HUDHealthMeter : MonoBehaviour
{
    private Image fill, outline;
    private Text value;
    private CombatActor actor;
    private CombatHealth health;
    private float previous, from, target, elapsed = 1, pulse;
    public float DisplayedRatio => fill == null ? 0 : fill.fillAmount;
    public string ValueText => value == null ? "" : value.text;
    public void Initialize(Image bar, Image border, Text numbers)
    { fill = bar; outline = border; value = numbers; }
    public void Bind(CombatActor source)
    {
        CombatHealth next = source == null ? null : source.Health;
        if (actor == source && health == next) return;
        actor = source; health = next; pulse = 0; elapsed = 1;
        previous = health == null ? 0 : health.Current;
        target = health == null ? 0 : Mathf.Clamp01(health.Current / health.Maximum);
        from = target; fill.fillAmount = target;
        UpdateNumbers();
    }
    private void UpdateNumbers()
    { value.text = health == null ? "— / —" : $"{health.Current:0} / {health.Maximum:0}"; }
    private void Update()
    {
        if (fill == null) return;
        // Unity destroyed-object semantics: clear a departed opponent immediately.
        if (actor == null && health != null) { Bind(null); return; }
        if (health != null && !Mathf.Approximately(health.Current, previous))
        {
            if (health.Current < previous) pulse = .24f;
            from = fill.fillAmount; target = Mathf.Clamp01(health.Current / health.Maximum);
            elapsed = 0; previous = health.Current;
        }
        UpdateNumbers();
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / .24f);
        fill.fillAmount = Mathf.Lerp(from, target, 1 - (1 - t) * (1 - t));
        pulse = Mathf.Max(0, pulse - Time.unscaledDeltaTime);
        bool low = health != null && health.Alive && target <= .25f;
        Color normal = low ? new Color(.86f,.22f,.24f) : new Color(.87f,.89f,.87f);
        fill.color = Color.Lerp(normal, new Color(1,.34f,.32f), pulse / .24f);
        outline.color = pulse > 0 ? new Color(1,.35f,.32f,.85f) : new Color(1,1,1,.19f);
    }
}
