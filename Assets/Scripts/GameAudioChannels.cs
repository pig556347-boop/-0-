using UnityEngine;

// Optional supplied clips: no downloaded or unlicensed audio is bundled.
public sealed class GameAudioChannels : MonoBehaviour
{
    private AudioSource music,effects;
    private AudioClip lobby,battle,hit;
    private void Awake()
    {
        music=gameObject.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;
        effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;
        lobby=Resources.Load<AudioClip>("Audio/LobbyMusic");battle=Resources.Load<AudioClip>("Audio/BattleMusic");hit=Resources.Load<AudioClip>("Audio/Hit");
        SetBattle(false);
    }
    public void SetBattle(bool active)
    {music.Stop();music.clip=active?battle:lobby;if(music.clip!=null)music.Play();}
    public void OnHit(CombatActor source,CombatActor target,Vector3 point)
    {if(hit!=null)effects.PlayOneShot(hit);}
    private void Update()
    {music.volume=PlayerPrefs.GetFloat("KickBack.Music",1);effects.volume=PlayerPrefs.GetFloat("KickBack.Effects",1);}
}
