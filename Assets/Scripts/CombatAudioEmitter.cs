using UnityEngine;
using SchoolRuleZero;

// Events originate in the existing combat model; no input, health or timing changes.
public sealed class CombatAudioEmitter : MonoBehaviour
{
    private CombatActor actor;
    private void OnEnable()
    {
        actor=GetComponent<CombatActor>();
        if(actor==null)return;
        actor.StrikeStarted+=Strike;actor.Damaged+=Damage;actor.Dodged+=Strike;
    }
    private static void Sound(string id){if(GameAudioChannels.Instance!=null)GameAudioChannels.Instance.Play(id);}
    private void Strike(){Sound("Whoosh");}
    private void Damage(DamageResult result)
    {
        if(result.Kind==DamageKind.Parried)Sound("Parry");
        else if(result.Kind==DamageKind.Guarded)Sound("Block");
        else if(result.Kind==DamageKind.Hit)Sound(actor.Health.Alive?"Hit":"Down");
    }
    private void OnDisable()
    {
        if(actor==null)return;
        actor.StrikeStarted-=Strike;actor.Damaged-=Damage;actor.Dodged-=Strike;
    }
}
