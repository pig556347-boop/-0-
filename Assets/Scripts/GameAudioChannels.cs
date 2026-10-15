using UnityEngine;

// Original project audio. Keep presentation separate from combat decisions.
public sealed class GameAudioChannels : MonoBehaviour
{
    public static GameAudioChannels Instance { get; private set; }
    private AudioSource music,effects;
    private AudioClip lobby,battle;
    private readonly System.Collections.Generic.Dictionary<string,AudioClip> clips = new System.Collections.Generic.Dictionary<string,AudioClip>();
    private bool battleActive,verification;
    private float nextScan;
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
        if(music.clip!=null){music.Play();Trace(active?"BattleMusic":"LobbyMusic");}
    }
    public void Play(string id)
    {
        if(!clips.TryGetValue(id,out AudioClip clip)||clip==null)return;
        ApplyVolumes();effects.PlayOneShot(clip);Trace(id);
    }
    private void Trace(string id)
    {if(verification)Debug.Log("GAME_AUDIO "+id);}
    private void ApplyVolumes()
    {
        music.volume=.55f*Mathf.Clamp01(PlayerPrefs.GetFloat("KickBack.Music",1));
        effects.volume=.75f*Mathf.Clamp01(PlayerPrefs.GetFloat("KickBack.Effects",1));
    }
    private void Update()
    {
        ApplyVolumes();
        // Unity WebGL may suspend playback until the first browser gesture.
        if(!music.isPlaying&&music.clip!=null&&GameKeys.Down(KeyCode.Return))music.Play();
        if(!battleActive||Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+.25f;
        foreach(var actor in FindObjectsByType<CombatActor>(FindObjectsSortMode.None))
            if(actor.GetComponent<CombatAudioEmitter>()==null)actor.gameObject.AddComponent<CombatAudioEmitter>();
    }
    private void OnDestroy(){if(Instance==this)Instance=null;}
}
