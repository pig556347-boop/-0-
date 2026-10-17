using UnityEngine;

// Short, directional contacts never create colliders or alter combat random numbers.
public sealed class CombatEffects : MonoBehaviour
{
    public static CombatEffects Instance { get; private set; }
    private Material impactMaterial;
    private readonly System.Random variation = new System.Random(2741);
    private void Awake()
    {
        Instance = this;
        Shader shader = Resources.Load<Shader>("Shaders/Impact");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        impactMaterial = new Material(shader) { color = Color.white };
    }
    public void OnHit(CombatActor source, CombatActor target, Vector3 position)
    {
        if (source == null || target == null) return;
        bool ultimate = source.State == CombatActor.ActionState.ULTIMATE;
        bool final = ultimate && source.IsUltimateFinisher;
        bool heavy = !ultimate && source.HeavyStrike;
        float size = final ? .83f : heavy ? .59f : ultimate ? .31f : source.PunchStrike ? .29f : .40f;
        Color color = ultimate ? new Color(1,.69f,.23f) : heavy ? new Color(1,.40f,.22f) : new Color(1,.84f,.55f);
        Burst(position, source.Facing, color, size, final ? .24f : .16f, final ? 14 : heavy ? 10 : 7, false);
    }
    public void Defense(CombatActor target, bool parry)
    {
        if (target == null) return;
        Vector3 point = target.transform.position + target.Facing * .48f + Vector3.up * .25f;
        Burst(point, target.Facing, parry ? new Color(1,.91f,.43f) : new Color(.40f,.80f,1),
            parry ? .65f : .39f, parry ? .23f : .14f, parry ? 10 : 4, true);
        if (parry && HitImpactManager.Instance != null) HitImpactManager.Instance.OnParry(target);
    }
    public void GuardBreak(CombatActor target)
    {
        if(target==null)return;
        Burst(target.transform.position+target.Facing*.45f+Vector3.up*.25f,-target.Facing,
            new Color(.56f,.86f,1),.73f,.22f,12,false);
    }
    public void DodgeTrail(CombatActor source)
    {
        if (source == null) return;
        Burst(source.transform.position + Vector3.down * .72f, -source.Facing,
            new Color(.65f,.78f,.85f,.45f), .32f, .18f, 3, false);
    }
    private void Burst(Vector3 position, Vector3 direction, Color color, float size, float lifetime, int rays, bool ring)
    {
        Camera camera = Camera.main;
        Vector3 right = camera == null ? Vector3.right : camera.transform.right;
        Vector3 up = camera == null ? Vector3.up : camera.transform.up;
        Vector2 projected = new Vector2(Vector3.Dot(direction,right), Vector3.Dot(direction,up));
        float angle = Mathf.Atan2(projected.y,projected.x);
        Vector3[] vertices = new Vector3[(rays + (ring ? 24 : 0))*4];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[vertices.Length/4*6];
        int quad = 0;
        for (int i = 0; i < rays; i++)
        {
            float ray = angle + Mathf.PI*2*i/rays + RandomRange(-.20f,.20f);
            float length = size*RandomRange(.65f,1.25f);
            Vector2 axis = new Vector2(Mathf.Cos(ray),Mathf.Sin(ray));
            Vector2 side = new Vector2(-axis.y,axis.x)*size*(ring ? .021f : .037f);
            Vector2 near = axis*size*.10f, far = axis*length;
            AddQuad(vertices,colors,triangles,quad++,near-side,near+side,far+side*.12f,far-side*.12f,color);
        }
        if (ring) for (int i = 0; i < 24; i++)
        {
            float a = i*Mathf.PI/12, b = (i+1)*Mathf.PI/12;
            Vector2 start = new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            Vector2 end = new Vector2(Mathf.Cos(b),Mathf.Sin(b));
            AddQuad(vertices,colors,triangles,quad++,start*size*.70f,start*size*.77f,end*size*.77f,end*size*.70f,color);
        }
        GameObject effect = new GameObject(ring ? "Defense contact" : "Directional contact");
        effect.transform.position = position - (camera == null ? Vector3.zero : camera.transform.forward*.10f);
        effect.transform.SetParent(transform,true);
        if (camera != null) effect.transform.rotation = camera.transform.rotation;
        Mesh mesh = new Mesh { name = "Contact rays", vertices = vertices, colors = colors, triangles = triangles };
        mesh.RecalculateBounds();
        effect.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = effect.AddComponent<MeshRenderer>();renderer.sharedMaterial = impactMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows = false;
        effect.AddComponent<ContactBurst>().Initialize(mesh, colors, lifetime);
    }
    private float RandomRange(float min, float max) => Mathf.Lerp(min,max,(float)variation.NextDouble());
    private static void AddQuad(Vector3[] vertices,Color[] colors,int[] triangles,int quad,
        Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
    {
        int v=quad*4,t=quad*6;
        vertices[v]=a;vertices[v+1]=b;vertices[v+2]=c;vertices[v+3]=d;
        for(int n=0;n<4;n++)colors[v+n]=color;
        triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;
        triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;
    }
    private void OnDestroy()
    {
        if(Instance==this)Instance=null;
        if(impactMaterial!=null)Destroy(impactMaterial);
    }
}
