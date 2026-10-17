using UnityEngine;
using SchoolRuleZero;

// A swing sounds on its real active frame. Contact sounds only after confirmed damage.
public sealed class CombatAudioEmitter : MonoBehaviour
{
    private CombatActor actor;
    private void OnEnable()
    {
        actor=GetComponent<CombatActor>();if(actor==null)return;
        actor.StrikeStarted+=Strike;actor.HitConfirmed+=Hit;actor.Damaged+=Damage;
        actor.Dodged+=Dodge;actor.UltimateStarted+=Ultimate;
    }
    private static void Sound(string id){if(GameAudioChannels.Instance!=null)GameAudioChannels.Instance.Play(id);}
    private void Strike()
    {
        if(actor.State==CombatActor.ActionState.ULTIMATE)
            Sound(actor.IsUltimateFinisher?"HeavyWhoosh":"KickWhoosh");
        else Sound(actor.PunchStrike?"Whoosh":actor.HeavyStrike?"HeavyWhoosh":"KickWhoosh");
    }
    private void Hit(CombatActor source,CombatActor target,Vector3 point)
    {
        if(!target.Health.Alive)return; // The target's Down replaces the body hit, avoiding a double transient.
        Sound(source.State==CombatActor.ActionState.ULTIMATE?"UltimateHit":source.HeavyStrike?"HeavyHit":source.PunchStrike?"PunchHit":"KickHit");
    }
    private void Damage(DamageResult result)
    {
        if(result.Kind==DamageKind.Parried)Sound("Parry");
        else if(result.Kind==DamageKind.Guarded)Sound("Block");
        else if(result.Kind==DamageKind.Hit&&!actor.Health.Alive)Sound("Down");
    }
    private void Dodge(){Sound("DodgeWhoosh");}
    private void Ultimate(int tier){Sound("UltimateRise");}
    private void OnDisable()
    {
        if(actor==null)return;
        actor.StrikeStarted-=Strike;actor.HitConfirmed-=Hit;actor.Damaged-=Damage;
        actor.Dodged-=Dodge;actor.UltimateStarted-=Ultimate;
    }
}
