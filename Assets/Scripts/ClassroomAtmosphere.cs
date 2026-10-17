using System.Collections.Generic;
using UnityEngine;

// Quiet architectural detail stays on the room perimeter, outside the combat sightlines.
public sealed class ClassroomAtmosphere : MonoBehaviour
{
    private readonly List<Material> materials=new List<Material>();
    public static void Build(Transform parent)
    {
        var atmosphere=parent.gameObject.AddComponent<ClassroomAtmosphere>();
        Material trim=atmosphere.Surface(new Color(.23f,.26f,.27f));
        Material pale=atmosphere.Surface(new Color(.72f,.72f,.66f));
        Material metal=atmosphere.Surface(new Color(.31f,.37f,.39f));
        Material paper=atmosphere.Surface(new Color(.78f,.73f,.60f));
        atmosphere.Box("Wall skirting",new Vector3(0,.09f,8.43f),new Vector3(27,.17f,.08f),trim);
        atmosphere.Box("Wall top trim",new Vector3(0,3.92f,8.43f),new Vector3(27,.10f,.08f),metal);
        // Fluorescent fixtures are shallow wall-side geometry, never large floating foreground objects.
        for(int i=-1;i<=1;i++)
        {
            atmosphere.Box("Fixture housing",new Vector3(i*8,3.70f,8.36f),new Vector3(2.5f,.09f,.14f),trim);
            atmosphere.Box("Soft fixture diffuser",new Vector3(i*8,3.67f,8.25f),new Vector3(2.25f,.045f,.04f),pale);
        }
        atmosphere.Box("Notice panel frame",new Vector3(-5.25f,2.20f,8.30f),new Vector3(1.20f,1.02f,.065f),trim);
        atmosphere.Box("Notice cork",new Vector3(-5.25f,2.20f,8.25f),new Vector3(1.11f,.93f,.04f),paper);
        for(int i=0;i<3;i++)
            atmosphere.Box("Notice paper",new Vector3(-5.58f+i*.34f,2.23f+(i%2)*.12f,8.21f),new Vector3(.25f,.40f,.012f),pale);
        atmosphere.Clock(new Vector3(4.72f,2.85f,8.28f),trim,pale);
        Shader lightShader=Resources.Load<Shader>("Shaders/ClassroomLight");
        if(lightShader!=null)
        {
            var sunlight=new Material(lightShader);sunlight.color=new Color(1,.85f,.60f,.075f);atmosphere.materials.Add(sunlight);
            for(int side=-1;side<=1;side+=2)for(int pane=0;pane<3;pane++)
            {
                GameObject patch=GameObject.CreatePrimitive(PrimitiveType.Quad);patch.name="Window light on floor";
                patch.transform.SetParent(parent,false);patch.transform.localPosition=new Vector3(side*9.4f+pane*.68f,.012f,6.85f-pane*.13f);
                patch.transform.localRotation=Quaternion.Euler(90,0,-18);patch.transform.localScale=new Vector3(.57f,2.2f,1);
                var collider=patch.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                patch.GetComponent<Renderer>().sharedMaterial=sunlight;
            }
        }
    }
    private Material Surface(Color color)
    {
        var material=new Material(Resources.Load<Shader>("Shaders/SchoolToon")){color=color};
        materials.Add(material);return material;
    }
    private void Box(string label,Vector3 position,Vector3 scale,Material material)
    {
        GameObject box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=label;
        box.transform.SetParent(transform,false);box.transform.localPosition=position;box.transform.localScale=scale;
        Collider collider=box.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        box.GetComponent<Renderer>().sharedMaterial=material;
    }
    private void Clock(Vector3 position,Material frame,Material face)
    {
        GameObject clock=GameObject.CreatePrimitive(PrimitiveType.Cylinder);clock.name="Classroom clock frame";
        clock.transform.SetParent(transform,false);clock.transform.localPosition=position;
        clock.transform.localRotation=Quaternion.Euler(90,0,0);clock.transform.localScale=new Vector3(.52f,.045f,.52f);
        Collider shape=clock.GetComponent<Collider>();shape.enabled=false;Destroy(shape);clock.GetComponent<Renderer>().sharedMaterial=frame;
        GameObject dial=GameObject.CreatePrimitive(PrimitiveType.Cylinder);dial.name="Clock face";
        dial.transform.SetParent(transform,false);dial.transform.localPosition=position+Vector3.back*.055f;
        dial.transform.localRotation=Quaternion.Euler(90,0,0);dial.transform.localScale=new Vector3(.44f,.012f,.44f);
        shape=dial.GetComponent<Collider>();shape.enabled=false;Destroy(shape);dial.GetComponent<Renderer>().sharedMaterial=face;
        Box("Clock minute hand",position+new Vector3(0,.065f,-.073f),new Vector3(.016f,.15f,.015f),frame);
        Box("Clock hour hand",position+new Vector3(.045f,0,-.077f),new Vector3(.10f,.020f,.016f),frame);
    }
    private void OnDestroy(){foreach(Material material in materials)if(material!=null)Destroy(material);}
}
