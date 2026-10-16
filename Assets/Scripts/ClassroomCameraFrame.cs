using UnityEngine;

// Close combat framing: stable in the centre, bounded adjustment at room edges.
public sealed class ClassroomCameraFrame : MonoBehaviour
{
    private Camera view;
    private CombatActor player;
    private Vector3 velocity;
    public void Initialize(Camera camera,CombatActor actor=null)
    {
        view=camera;player=actor;
        view.nearClipPlane=.3f;view.farClipPlane=50f;
        transform.position=new Vector3(0,3.2f,0);
        ApplyFrame();
    }
    private void LateUpdate()
    {
        ApplyFrame();
        if(player==null||Time.timeScale==0)return;
        Vector3 point=player.transform.position;
        // No facing or jump offsets. Only reframe when approaching the visible edges.
        float x=Mathf.Sign(point.x)*Mathf.Max(0,Mathf.Abs(point.x)-6.5f);
        float z=Mathf.Sign(point.z)*Mathf.Max(0,Mathf.Abs(point.z)-3.5f);
        Vector3 target=new Vector3(Mathf.Clamp(x,-4,4),3.2f,Mathf.Clamp(z,-2,2));
        transform.position=Vector3.SmoothDamp(transform.position,target,ref velocity,.18f,Mathf.Infinity,Time.deltaTime);
    }
    private void ApplyFrame()
    {
        if(view==null)return;
        view.orthographicSize=Mathf.Max(7.5f,12.5f/Mathf.Max(.1f,view.aspect));
    }
}
