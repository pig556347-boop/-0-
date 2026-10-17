using UnityEngine;

public sealed class HitImpactManager : MonoBehaviour
{
    public static HitImpactManager Instance { get; private set; }
    [SerializeField, Range(0, .2f)] private float hitStopSeconds = .045f;
    [SerializeField, Range(0, .5f)] private float shakeStrength = .055f;
    [SerializeField, Range(0, 1)] private float shakeDuration = .13f;
    [SerializeField, Range(0, 3)] private float knockbackMultiplier = 1;
    public bool Paused { get; private set; }
    public float KnockbackMultiplier => knockbackMultiplier;
    private Camera targetCamera;
    private Vector3 cameraHome,shakeDirection;
    private float freezeUntil,shakeUntil,shakeStarted,shakeAmplitude,originalTimeScale;
    private void Awake(){Instance=this;originalTimeScale=Time.timeScale>0?Time.timeScale:1;}
    public void Initialize(Camera camera)
    {targetCamera=camera;cameraHome=camera.transform.localPosition;}
    public void OnHit(CombatActor source,CombatActor target,Vector3 point)
    {
        if(source==null||Paused)return;
        bool ultimate=source.State==CombatActor.ActionState.ULTIMATE;
        bool finisher=ultimate&&source.IsUltimateFinisher;
        float weight=finisher?1.5f:ultimate?.32f:source.HeavyStrike?1.15f:source.PunchStrike?.60f:.85f;
        if(source.Team!=0)weight*=.80f;
        float stop=ultimate&&!finisher?.012f:Mathf.Min(.085f,hitStopSeconds*weight);
        Impact(source.Facing,stop,weight);
    }
    public void OnParry(CombatActor defender)
    {if(defender!=null&&!Paused)Impact(defender.Facing,.060f,1.05f);}
    private void Impact(Vector3 direction,float stop,float weight)
    {
        freezeUntil=Mathf.Max(freezeUntil,Time.unscaledTime+stop);
        shakeStarted=Time.unscaledTime;shakeUntil=shakeStarted+shakeDuration;
        shakeAmplitude=Mathf.Min(1.5f,weight)*shakeStrength;
        if(targetCamera!=null)
        {
            Vector3 local=targetCamera.transform.InverseTransformDirection(direction);
            // Screen-plane movement preserves the fixed classroom framing and zoom.
            shakeDirection=targetCamera.transform.localRotation*new Vector3(local.x,local.y*.65f,0).normalized;
        }
    }
    public void SetPaused(bool pause)
    {
        Paused=pause;
        if(pause){freezeUntil=0;shakeUntil=0;}
        UpdateClock();
    }
    private void Update()
    {
        if(Application.isFocused&&GameKeys.Down(KeyCode.Escape))SetPaused(!Paused);
        UpdateClock();
    }
    private void UpdateClock()=>Time.timeScale=Paused||Time.unscaledTime<freezeUntil?0:originalTimeScale;
    private void LateUpdate()
    {
        if(targetCamera==null)return;
        float progress=Mathf.Clamp01((Time.unscaledTime-shakeStarted)/Mathf.Max(.01f,shakeDuration));
        float pulse=!Paused&&Time.unscaledTime<shakeUntil?Mathf.Sin(progress*Mathf.PI*4)*(1-progress)*(1-progress):0;
        targetCamera.transform.localPosition=cameraHome+shakeDirection*(pulse*shakeAmplitude);
    }
    private void OnDisable()
    {
        Time.timeScale=originalTimeScale;
        if(targetCamera!=null)targetCamera.transform.localPosition=cameraHome;
        if(Instance==this)Instance=null;
    }
}
