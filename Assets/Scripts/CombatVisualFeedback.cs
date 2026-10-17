using UnityEngine;
using SchoolRuleZero;

// Actor-local presentation only; effects use the existing authoritative damage outcomes.
[DefaultExecutionOrder(9000)]
public sealed class CombatVisualFeedback : MonoBehaviour
{
    private CombatActor actor;
    private Transform contact;
    private Material contactMaterial;
    private MaterialPropertyBlock block;
    private Renderer[] flashRenderers;
    private float flashUntil;
    private void Awake()
    {
        actor=GetComponent<CombatActor>();
        if(actor==null){enabled=false;return;}
        block=new MaterialPropertyBlock();
        Shader shader=Resources.Load<Shader>("Shaders/ContactShadow");
        if(shader!=null)
        {
            contactMaterial=new Material(shader);
            GameObject shadow=GameObject.CreatePrimitive(PrimitiveType.Quad);shadow.name="Soft contact shadow";
            Collider collider=shadow.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            contact=shadow.transform;contact.SetParent(transform,false);
            MeshRenderer renderer=shadow.GetComponent<MeshRenderer>();renderer.sharedMaterial=contactMaterial;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        actor.Damaged+=Damage;actor.GuardBroken+=GuardBroken;actor.Dodged+=Dodge;
    }
    private void Damage(DamageResult result)
    {
        if(result.Kind==DamageKind.Parried||result.Kind==DamageKind.Guarded)
        {
            if(CombatEffects.Instance!=null)CombatEffects.Instance.Defense(actor,result.Kind==DamageKind.Parried);
        }
        else if(result.Kind==DamageKind.Hit)
        {
            flashRenderers=GetComponentsInChildren<Renderer>();flashUntil=Time.time+.11f;
        }
    }
    private void Dodge()
    {if(CombatEffects.Instance!=null)CombatEffects.Instance.DodgeTrail(actor);}
    private void GuardBroken()
    {
        if(CombatEffects.Instance!=null)CombatEffects.Instance.GuardBreak(actor);
    }
    private void LateUpdate()
    {
        if(actor==null||actor.Health==null)return;
        if(contact!=null)
        {
            float lift=Mathf.Max(0,transform.position.y-1.05f);
            bool alive=actor.Health.Alive;
            Vector3 point=transform.position+(alive?Vector3.zero:actor.Facing*.35f);point.y=.055f;
            contact.position=point;
            contact.rotation=Quaternion.LookRotation(Vector3.up,actor.Facing);
            float spread=1+Mathf.Min(1,lift)*.30f;
            contact.localScale=new Vector3((alive?1.05f:1.6f)*spread,(alive?.72f:.80f)*spread,1);
            contactMaterial.SetColor("_Color",new Color(.055f,.045f,.04f,(alive?.24f:.15f)/(1+lift*1.8f)));
        }
        if(flashRenderers==null)return;
        float flash=Mathf.Clamp01((flashUntil-Time.time)/.11f)*.20f;
        foreach(Renderer renderer in flashRenderers)
        {
            if(renderer==null||renderer.sharedMaterial==null||!renderer.sharedMaterial.HasProperty("_HitFlash"))continue;
            renderer.GetPropertyBlock(block);block.SetFloat("_HitFlash",flash);renderer.SetPropertyBlock(block);
        }
        if(flash==0)flashRenderers=null;
    }
    private void OnDestroy()
    {
        if(actor!=null){actor.Damaged-=Damage;actor.GuardBroken-=GuardBroken;actor.Dodged-=Dodge;}
        if(contactMaterial!=null)Destroy(contactMaterial);
    }
}
