using UnityEngine;

public sealed class HitImpactManager : MonoBehaviour
{
    public static HitImpactManager Instance { get; private set; }
    [SerializeField, Range(0, .2f)] private float hitStopSeconds = .055f;
    [SerializeField, Range(0, .5f)] private float shakeStrength = .08f;
    [SerializeField, Range(0, 1)] private float shakeDuration = .15f;
    [SerializeField, Range(0, 3)] private float knockbackMultiplier = 1;
    public bool Paused { get; private set; }
    public float KnockbackMultiplier => knockbackMultiplier;
    private Camera targetCamera;
    private Vector3 cameraHome;
    private float freezeUntil;
    private float shakeUntil;
    private float originalTimeScale;

    private void Awake()
    {
        Instance = this; originalTimeScale = Time.timeScale;
    }
    public void Initialize(Camera camera)
    { targetCamera = camera; cameraHome = camera.transform.localPosition; }
    public void OnHit(CombatActor source, CombatActor target, Vector3 point)
    {
        float stop = source.Team == 0 && source.State == CombatActor.ActionState.ULTIMATE && !source.IsUltimateFinisher
            ? Mathf.Min(hitStopSeconds, .018f) : hitStopSeconds;
        freezeUntil = Mathf.Max(freezeUntil, Time.unscaledTime + stop);
        shakeUntil = Time.unscaledTime + shakeDuration;
    }
    public void SetPaused(bool pause) { Paused = pause; UpdateClock(); }
    private void Update()
    {
        if (Application.isFocused && GameKeys.Down(KeyCode.Escape)) SetPaused(!Paused);
        UpdateClock();
    }
    private void UpdateClock() => Time.timeScale =
        Paused || Time.unscaledTime < freezeUntil ? 0 : originalTimeScale;
    private void LateUpdate()
    {
        if (targetCamera == null) return;
        targetCamera.transform.localPosition = cameraHome + (!Paused && Time.unscaledTime < shakeUntil
            ? Random.insideUnitSphere * shakeStrength : Vector3.zero);
    }
    private void OnDisable()
    {
        Time.timeScale = originalTimeScale;
        if (targetCamera != null) targetCamera.transform.localPosition = cameraHome;
        if (Instance == this) Instance = null;
    }
}
