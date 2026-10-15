using UnityEngine;

// Original project audio. Keep presentation separate from combat decisions.
public sealed class GameAudioChannels : MonoBehaviour
{
    public static GameAudioChannels Instance { get; private set; }
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Music(int battle);
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Play(int id);
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Volumes(float master,float music,float effects);
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Close();
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KickBackAudio_Pause(int paused);
#endif
    private AudioSource music,effects;
    private AudioClip lobby,battle;
    private readonly System.Collections.Generic.Dictionary<string,AudioClip> clips = new System.Collections.Generic.Dictionary<string,AudioClip>();
    private bool battleActive,verification,musicPaused;
    private float nextScan,nextMusicRetry,nextStatus;
    private void Awake()
    {
        Instance=this;verification=Application.absoluteURL.Contains("motionVerification=1");
        music=gameObject.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;music.spatialBlend=0;
        effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;
        lobby=Resources.Load<AudioClip>("Audio/LobbyMusic");battle=Resources.Load<AudioClip>("Audio/BattleMusic");
        foreach(string id in new[]{"Hit","Whoosh","Block","Parry","Down","Menu","Success"})
            clips[id]=Resources.Load<AudioClip>("Audio/"+id);
        ApplyVolumes();SetBattle(false);
    }
    public void SetBattle(bool active)
    {
        battleActive=active;music.Stop();music.clip=active?battle:lobby;
        #if UNITY_WEBGL && !UNITY_EDITOR
        KickBackAudio_Music(active?1:0);
#else
        if(music.clip!=null){music.clip.LoadAudioData();TryStartMusic();}
#endif
        Trace(active?"BattleMusic":"LobbyMusic");
    }
    public void Play(string id)
    {
        if(!clips.TryGetValue(id,out AudioClip clip)||clip==null)return;
        ApplyVolumes();
#if UNITY_WEBGL && !UNITY_EDITOR
        KickBackAudio_Play(System.Array.IndexOf(new[]{"Hit","Whoosh","Block","Parry","Down","Menu","Success"},id));
#else
        effects.PlayOneShot(clip);
#endif
        Trace(id);
    }
    private void Trace(string id)
    {if(verification)Debug.Log("GAME_AUDIO "+id);}
    private void TryStartMusic()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        if(musicPaused||music.clip==null||music.isPlaying||Time.unscaledTime<nextMusicRetry)return;
        nextMusicRetry=Time.unscaledTime+.5f;
        if(music.clip.loadState==AudioDataLoadState.Loaded)music.Play();
#endif
    }
    private void ApplyVolumes()
    {
        music.volume=.55f*Mathf.Clamp01(PlayerPrefs.GetFloat("KickBack.Music",1));
        effects.volume=.75f*Mathf.Clamp01(PlayerPrefs.GetFloat("KickBack.Effects",1));
#if UNITY_WEBGL && !UNITY_EDITOR
        KickBackAudio_Volumes(AudioListener.volume,music.volume,effects.volume);
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
            if(paused)music.Pause();else music.UnPause();
#endif
            Trace(paused?"MusicPaused":"MusicResumed");
        }
        // WebGL decodes imported audio asynchronously; do not lose the initial loop
        // if the clip was still loading or playback was deferred by autoplay policy.
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
        nextScan=Time.unscaledTime+.25f;
        foreach(var actor in FindObjectsByType<CombatActor>(FindObjectsSortMode.None))
            if(actor.GetComponent<CombatAudioEmitter>()==null)actor.gameObject.AddComponent<CombatAudioEmitter>();
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
