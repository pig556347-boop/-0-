using UnityEngine;

// Damage volume is independent of the wider movement/grounding capsule.
// Ignore outstretched hands, clothing and effects when deciding whether a body was hit.
public sealed class CombatHurtbox : MonoBehaviour
{
    private CombatActor actor;
    private PlayerController motor;
    public float Width { get; private set; } = .50f;
    public float Depth { get; private set; } = .34f;
    public float Height { get; private set; } = 1.80f;
    private void Awake(){actor=GetComponent<CombatActor>();motor=GetComponent<PlayerController>();}
    public void SetBuild(float width,float depth,float height)
    {Width=width;Depth=depth;Height=height;}
    public bool Intersects(Vector3 attackCenter,Quaternion attackRotation,Vector3 attackHalf)
    {
        if(actor==null||!actor.Health.Alive)return false;
        float height=(actor.State==CombatActor.ActionState.CROUCH||motor!=null&&motor.IsCrouching)?Height*.62f:Height;
        Vector3 center=transform.position+Vector3.up*(-.95f+.08f+height*.5f);
        if(Mathf.Abs(center.y-attackCenter.y)>height*.5f+attackHalf.y)return false;
        Vector3 right=Vector3.Cross(Vector3.up,actor.Facing).normalized;
        Vector3 forward=actor.Facing;
        Vector3 ar=attackRotation*Vector3.right,af=attackRotation*Vector3.forward;
        Vector3 delta=center-attackCenter;delta.y=0;
        // Separating-axis test for the two oriented horizontal rectangles.
        return OnAxis(right,delta,right,forward,ar,af,attackHalf)&&
            OnAxis(forward,delta,right,forward,ar,af,attackHalf)&&
            OnAxis(ar,delta,right,forward,ar,af,attackHalf)&&
            OnAxis(af,delta,right,forward,ar,af,attackHalf);
    }
    private bool OnAxis(Vector3 axis,Vector3 delta,Vector3 right,Vector3 forward,Vector3 ar,Vector3 af,Vector3 half)
    {
        float body=Width*.5f*Mathf.Abs(Vector3.Dot(axis,right))+Depth*.5f*Mathf.Abs(Vector3.Dot(axis,forward));
        float attack=half.x*Mathf.Abs(Vector3.Dot(axis,ar))+half.z*Mathf.Abs(Vector3.Dot(axis,af));
        return Mathf.Abs(Vector3.Dot(delta,axis))<=body+attack;
    }
}
