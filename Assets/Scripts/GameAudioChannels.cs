using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Original audio only. Combat events choose sounds; audio never changes combat timing.
public sealed class GameAudioChannels : MonoBehaviour
{
    public static GameAudioChannels Instance { get; private set; }
    public static readonly string[] EffectNames={"Hit","Whoosh","Block","Parry","Down","Menu","Success",
        "PunchHit","KickHit","HeavyHit","UltimateHit","KickWhoosh","HeavyWhoosh","DodgeWhoosh","UltimateRise"};
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Music(int mode);
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Play(int id,float gain,float pitch,int priority);
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Volumes(float master,float music,float effects);
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Close();
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Pause(int paused);
#endif
    private const int VoiceCount=6;
    private AudioSource music;
    private readonly AudioSource[] voices=new AudioSource[VoiceCount];
    private readonly float[] voiceGains=new float[VoiceCount];
    private readonly int[] priorities=new int[VoiceCount];
    private readonly float[] voiceStarted=new float[VoiceCount];
    private AudioClip lobby,battle,boss;
    private readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
    private readonly Dictionary<string,float> nextAllowed=new Dictionary<string,float>();
    private bool battleActive,verification,musicPaused,switching;
    private float nextScan,nextMusicRetry,nextStatus,duckUntil,musicEnvelope=1;
    private int musicMode=-1,variation;
    private Coroutine transition;
    public string CurrentMusic=>musicMode==2?"BossMusic":musicMode==1?"BattleMusic":"LobbyMusic";
    private void Awake()
    {
        Instance=this;verification=Application.absoluteURL.Contains("motionVerification=1");
        music=gameObject.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;music.spatialBlend=0;
        for(int i=0;i<VoiceCount;i++)
        {voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;}
        lobby=Resources.Load<AudioClip>("Audio/LobbyMusic");battle=Resources.Load<AudioClip>("Audio/BattleMusic");
        boss=Resources.Load<AudioClip>("Audio/BossMusic");
        foreach(string id in EffectNames)clips[id]=Resources.Load<AudioClip>("Audio/"+id);
        ApplyVolumes();SetBattle(false);
    }
    public void SetBattle(bool active)
    {battleActive=active;SelectMusic(active?1:0);}
    private void SelectMusic(int mode)
    {
        if(musicMode==mode)return;
        musicMode=mode;
#if UNITY_WEBGL && !UNITY_EDITOR
        music.clip=mode==2?boss:mode==1?battle:lobby;
        KickBackAudio_Music(mode);
#else
        if(transition!=null)StopCoroutine(transition);
        transition=StartCoroutine(SwitchMusic(mode==2?boss:mode==1?battle:lobby));
#endif
        Trace(CurrentMusic);
        // Existing gameplay verification observes the broad battle channel as well.
        if(mode==2)Trace("BattleMusic");
    }
    private IEnumerator SwitchMusic(AudioClip clip)
    {
        switching=true;float from=musicEnvelope,started=Time.unscaledTime;
        while(Time.unscaledTime-started<.12f)
        {musicEnvelope=Mathf.Lerp(from,0,(Time.unscaledTime-started)/.12f);yield return null;}
        music.Stop();music.clip=clip;musicEnvelope=0;
        if(clip!=null)
        {
            clip.LoadAudioData();while(clip.loadState==AudioDataLoadState.Loading)yield return null;
            if(!musicPaused&&clip.loadState==AudioDataLoadState.Loaded)music.Play();
        }
        started=Time.unscaledTime;
        while(Time.unscaledTime-started<.22f)
        {musicEnvelope=Mathf.Clamp01((Time.unscaledTime-started)/.22f);yield return null;}
        musicEnvelope=1;switching=false;transition=null;
    }
    public void Play(string id)
    {
        if(musicPaused||!clips.TryGetValue(id,out AudioClip clip)||clip==null)return;
        float now=Time.unscaledTime;
        if(nextAllowed.TryGetValue(id,out float until)&&now<until)return;
        nextAllowed[id]=now+(id=="Down"?.10f:id=="Menu"?.04f:.035f);
        int priority=id=="Parry"||id=="Down"||id=="UltimateHit"?3:id=="HeavyHit"||id=="Block"?2:1;
        float gain=id=="UltimateRise"?.75f:1;
        // Small deterministic variation does not consume the combat AI random stream.
        float pitch=id=="Menu"||id=="Success"?1:1+((variation++%5)-2)*.012f;
        if(IsImpact(id))duckUntil=now+.16f;
        ApplyVolumes();
#if UNITY_WEBGL && !UNITY_EDITOR
        KickBackAudio_Play(System.Array.IndexOf(EffectNames,id),gain,pitch,priority);
#else
        int slot=-1;
        for(int i=0;i<VoiceCount;i++)if(!voices[i].isPlaying){slot=i;break;}
        if(slot<0)
        {
            for(int i=0;i<VoiceCount;i++)
                if(priorities[i]<=priority&&(slot<0||priorities[i]<priorities[slot]||priorities[i]==priorities[slot]&&voiceStarted[i]<voiceStarted[slot]))slot=i;
        }
        if(slot<0)return;
        voices[slot].Stop();voices[slot].clip=clip;voices[slot].pitch=pitch;
        voices[slot].volume=.64f*Mathf.Clamp01(PlayerPrefs.GetFloat("KickBack.Effects",1))*gain;
        voiceGains[slot]=gain;priorities[slot]=priority;voiceStarted[slot]=now;voices[slot].Play();ApplyVolumes();
#endif
        Trace(id);
        if(id.EndsWith("Hit")&&id!="Hit")Trace("Hit");
        if(id.EndsWith("Whoosh")&&id!="Whoosh")Trace("Whoosh");
    }
    private static bool IsImpact(string id)=>id=="Hit"||id.EndsWith("Hit")||id=="Parry"||id=="Down";
    private void Trace(string id){if(verification)Debug.Log("GAME_AUDIO "+id);}
    private void TryStartMusic()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        if(switching||musicPaused||music.clip==null||music.isPlaying||Time.unscaledTime<nextMusicRetry)return;
        nextMusicRetry=Time.unscaledTime+.5f;
        if(music.clip.loadState==AudioDataLoadState.Loaded)music.Play();
#endif
    }
    private void ApplyVolumes()
    {
        float musicLevel=.48f*Mathf.Clamp01(PlayerPrefs.GetFloat("KickBack.Music",1));
        float effectLevel=.64f*Mathf.Clamp01(PlayerPrefs.GetFloat("KickBack.Effects",1));
        float duck=Time.unscaledTime<duckUntil?.78f:1;
        music.volume=musicLevel*musicEnvelope*duck;
        int active=0;foreach(var voice in voices)if(voice!=null&&voice.isPlaying)active++;
        float headroom=1/Mathf.Max(1,active*.65f);
        for(int i=0;i<VoiceCount;i++)if(voices[i]!=null)voices[i].volume=effectLevel*headroom*voiceGains[i];
#if UNITY_WEBGL && !UNITY_EDITOR
        KickBackAudio_Volumes(AudioListener.volume,musicLevel,effectLevel);
#endif
    }
    private void Update()
    {
        ApplyVolumes();
        bool paused=HitImpactManager.Instance!=null&&HitImpactManager.Instance.Paused;
        if(paused!=musicPaused)
        {
            musicPaused=paused;
#if UNITY_WEBGL && !UNITY_EDITOR
            KickBackAudio_Pause(paused?1:0);
#else
            if(paused){music.Pause();foreach(var voice in voices)voice.Stop();}else music.UnPause();
#endif
            Trace(paused?"MusicPaused":"MusicResumed");
        }
        TryStartMusic();
        if(verification&&Time.unscaledTime>=nextStatus)
        {
            nextStatus=Time.unscaledTime+2;
            Debug.Log("GAME_AUDIO_STATE clip="+(music.clip==null?"missing":music.clip.name)+
                " loaded="+(music.clip==null?"none":music.clip.loadState.ToString())+
                " playing="+music.isPlaying+" virtual="+music.isVirtual+" mute="+music.mute+" time="+music.time+
                " paused="+AudioListener.pause+" master="+AudioListener.volume+" music="+music.volume);
        }
        if(!battleActive||Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+.25f;bool liveBoss=false;
        foreach(var actor in FindObjectsByType<CombatActor>(FindObjectsSortMode.None))
        {
            if(actor.GetComponent<CombatAudioEmitter>()==null)actor.gameObject.AddComponent<CombatAudioEmitter>();
            if(actor.GameplayEnabled&&actor.Health!=null&&actor.Health.Alive&&actor.GetComponent<DojaehakBrain>()!=null)liveBoss=true;
        }
        SelectMusic(liveBoss?2:1);
    }
    private void OnDestroy()
    {
        if(Instance!=this)return;
#if UNITY_WEBGL && !UNITY_EDITOR
        KickBackAudio_Close();
#endif
        Instance=null;
    }
}
