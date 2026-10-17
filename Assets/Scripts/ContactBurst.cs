using UnityEngine;

// Scaled gameplay time freezes the contact image during hit stop and pause.
public sealed class ContactBurst : MonoBehaviour
{
    private Mesh mesh;
    private Color[] colors;
    private float age,lifetime,initialAlpha;
    public void Initialize(Mesh geometry,Color[] tint,float duration)
    {mesh=geometry;colors=tint;lifetime=duration;initialAlpha=tint.Length>0?tint[0].a:1;}
    private void Update()
    {
        age+=Time.deltaTime;
        float t=Mathf.Clamp01(age/lifetime);
        transform.localScale=Vector3.one*Mathf.Lerp(.82f,1.12f,t);
        float alpha=(1-t)*(1-t);
        for(int i=0;i<colors.Length;i++)colors[i].a=alpha*initialAlpha;
        mesh.colors=colors;
        if(age>=lifetime)Destroy(gameObject);
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
