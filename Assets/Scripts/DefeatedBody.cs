using UnityEngine;

// Owns only the defeated body's presentation and collision lifecycle.
// Runs after the supplied pose samplers so ground contact uses the displayed mesh.
[DefaultExecutionOrder(10000)]
public sealed class DefeatedBody : MonoBehaviour
{
    private CombatActor actor;
    private Rigidbody body;
    private RigidbodyInterpolation initialInterpolation;
    private Transform visual;
    private Vector3 homePosition,homeScale,deathPosition,settledPosition;
    private Quaternion deathRotation;
    private Collider[] shapes;
    private bool[] shapeEnabled;
    private bool initialKinematic,initialGravity,defeated,motorEnabled;
    private PlayerController motor;
    private float started,floorY,initialBottom;
    private AuthoredPlayerView authored;
    private Skin[] skins;
    private MeshRenderer[] rigidMeshes;
    public bool Settled { get; private set; }
    private sealed class Skin
    {
        public SkinnedMeshRenderer Renderer;
        public Vector3[] Vertices;
        public BoneWeight[] Weights;
        public Matrix4x4[] Bind,Matrices;
    }
    public void Initialize(CombatActor owner,Transform view)
    {
        actor=owner;body=owner.GetComponent<Rigidbody>();visual=view;
        motor=owner.GetComponent<PlayerController>();
        homePosition=view.localPosition;homeScale=view.localScale;
        initialKinematic=body.isKinematic;initialGravity=body.useGravity;initialInterpolation=body.interpolation;
        shapes=owner.GetComponents<Collider>();shapeEnabled=new bool[shapes.Length];
        for(int i=0;i<shapes.Length;i++)shapeEnabled[i]=shapes[i].enabled;
    }
    private void Begin()
    {
        defeated=true;Settled=false;started=Time.time;deathPosition=body.position;
        deathRotation=Quaternion.LookRotation(actor.Facing);
        var floor=GameObject.Find("Classroom floor");
        var floorShape=floor==null?null:floor.GetComponent<Collider>();
        floorY=floorShape==null?0:floorShape.bounds.max.y;
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        body.isKinematic=true;body.interpolation=RigidbodyInterpolation.None;
        foreach(var shape in shapes)shape.enabled=false;
        if(motor!=null){motorEnabled=motor.enabled;motor.enabled=false;}
        authored=visual.GetComponent<AuthoredPlayerView>();
        var renderers=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
        skins=new Skin[renderers.Length];
        for(int i=0;i<renderers.Length;i++)
        {
            Mesh mesh=renderers[i].sharedMesh;
            skins[i]=new Skin{Renderer=renderers[i],Vertices=mesh.vertices,Weights=mesh.boneWeights,
                Bind=mesh.bindposes,Matrices=new Matrix4x4[mesh.bindposes.Length]};
        }
        rigidMeshes=visual.GetComponentsInChildren<MeshRenderer>();
        initialBottom=TryBounds(out Bounds initial)?initial.min.y:floorY;
    }
    private void LateUpdate()
    {
        if(actor==null||actor.Health==null)return;
        if(actor.Health.Alive){if(defeated)Restore();return;}
        if(!defeated)Begin();
        if(Settled)
        {
            visual.localPosition=settledPosition;visual.localScale=homeScale;
            if(authored==null||!authored.IsReady)visual.rotation=deathRotation*Quaternion.Euler(90,0,0);
            return;
        }
        float age=Time.time-started;
        float fall=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.45f));
        // Keep the gameplay root unchanged; only the visible body falls to the floor.
        body.position=deathPosition;transform.position=deathPosition;
        visual.localScale=homeScale;
        visual.localPosition=homePosition;
        bool supplied=authored!=null&&authored.IsReady;
        if(!supplied)visual.rotation=deathRotation*Quaternion.Euler(90*fall,0,0);
        // A fixed capsule centre or bind-pose bounds cannot ground a rotated corpse.
        // Skin the actual pose rather than relying on the renderer's culling bounds.
        if(TryBounds(out Bounds bounds))
            visual.position+=Vector3.up*(Mathf.Lerp(initialBottom,floorY+.015f,fall)-bounds.min.y);
        Settled=age>=(supplied?Mathf.Max(.45f,authored.DeathDuration+.1f):.45f);
        if(Settled)settledPosition=visual.localPosition;
    }
    public bool TryBounds(out Bounds bounds)
    {
        bounds=default;bool found=false;
        if(skins!=null)foreach(var skin in skins)
        {
            if(skin.Renderer==null||!skin.Renderer.enabled)continue;
            var bones=skin.Renderer.bones;
            for(int i=0;i<skin.Matrices.Length;i++)skin.Matrices[i]=bones[i].localToWorldMatrix*skin.Bind[i];
            for(int i=0;i<skin.Vertices.Length;i++)
            {
                BoneWeight w=skin.Weights[i];Vector3 v=skin.Vertices[i];
                Vector3 p=skin.Matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+
                    skin.Matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+
                    skin.Matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+
                    skin.Matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
            }
        }
        if(rigidMeshes!=null)foreach(var mesh in rigidMeshes)
            if(mesh!=null&&mesh.enabled){if(!found){bounds=mesh.bounds;found=true;}else bounds.Encapsulate(mesh.bounds);}
        return found;
    }
    public void Restore()
    {
        if(!defeated)return;
        defeated=false;Settled=false;
        visual.localPosition=homePosition;visual.localScale=homeScale;
        visual.rotation=Quaternion.LookRotation(actor.Facing);
        body.isKinematic=initialKinematic;body.useGravity=initialGravity;body.interpolation=initialInterpolation;
        for(int i=0;i<shapes.Length;i++)shapes[i].enabled=shapeEnabled[i];
        if(motor!=null)motor.enabled=motorEnabled;
        skins=null;rigidMeshes=null;
    }
}
