using UnityEngine;

// Visual-only meshes have no primitive collider dependency in stripped WebGL players.
public static class PresentationGeometry
{
    public static GameObject Create(string label,Mesh mesh,Material material,Transform parent)
    {
        GameObject item=new GameObject(label);item.transform.SetParent(parent,false);
        item.AddComponent<MeshFilter>().sharedMesh=mesh;
        MeshRenderer renderer=item.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        return item;
    }
    public static Mesh Quad()
    {
        Mesh mesh=new Mesh{name="Presentation quad"};
        mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
        mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};
        mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};
        mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();return mesh;
    }
}
