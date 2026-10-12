using System;
using UnityEngine;

// Imported scenery only. Existing arena colliders and actor logic remain authoritative.
public sealed class ClassroomAssetBuilder : MonoBehaviour
{
    private readonly System.Collections.Generic.List<Material> materials = new System.Collections.Generic.List<Material>();

    public static void Build(Transform parent)
    {
        ClassroomAssetBuilder kit = parent.gameObject.AddComponent<ClassroomAssetBuilder>();
        GameObject floor = kit.Load("floor");
        for (int x = 0; x < 14; x++) for (int z = 0; z < 9; z++)
            kit.Place(floor, "Floor tile", new Vector3(-13 + x * 2, -.04f, -8 + z * 2), new Vector3(2, .04f, 2), 0);
        GameObject wall = kit.Load("wall");
        for (int x = 0; x < 4; x++)
            kit.Place(wall, "Classroom wall", new Vector3(-10.5f + x * 7, 0, 8.65f), new Vector3(7, 4, .22f), 180);
        GameObject board = kit.Load("board");
        kit.Place(board, "Chalkboard asset", new Vector3(0, 1.5f, 8.35f), new Vector3(.12f, 1.8f, 5.5f), 90);
        GameObject window = kit.Load("window");
        for (int x = 0; x < 2; x++)
            kit.Place(window, "Window asset", new Vector3(-9.5f + x * 19, 1.5f, 8.3f), new Vector3(3.5f, 1.7f, .15f), 180);
        GameObject furniture = kit.Load("furniture");
        for (int row = 0; row < 2; row++) for (int col = 0; col < 4; col++)
            kit.Place(furniture, "Desk and chair", new Vector3(-6 + col * 4, 0, 1.3f + row * 2.3f), new Vector3(1.1f, .85f, 1.05f), 0);
        Debug.Log("CLASSROOM_ASSETS_READY count=5 floorTiles=126 furniture=8 physics=preserved");
    }

    private GameObject Load(string kind)
    {
        GameObject model = Resources.Load<GameObject>("Classroom/" + kind + "/" + kind + "Model");
        Texture2D texture = Resources.Load<Texture2D>("Classroom/" + kind + "/BaseColor");
        Shader shader = Resources.Load<Shader>("Shaders/SchoolCharacter");
        if (model == null || texture == null || shader == null)
            throw new InvalidOperationException("Classroom resource missing: " + kind);
        Material material = new Material(shader) { mainTexture = texture };
        material.name = "Classroom " + kind;
        materials.Add(material);
        // Each prototype is inactive and shares mesh/material resources with all copies.
        GameObject prototype = Instantiate(model, transform, false);
        prototype.name = kind + " source";
        foreach (Renderer renderer in prototype.GetComponentsInChildren<Renderer>())
        {
            Material[] slots = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < slots.Length; i++) slots[i] = material;
            renderer.sharedMaterials = slots;
        }
        prototype.SetActive(false);
        return prototype;
    }

    private void Place(GameObject prototype, string label, Vector3 bottom, Vector3 size, float yaw)
    {
        GameObject instance = Instantiate(prototype, transform, false);
        instance.name = label;
        instance.SetActive(true);
        Bounds bounds = new Bounds(); bool initialized = false;
        foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
        {
            Bounds mesh = filter.sharedMesh.bounds;
            for (int n = 0; n < 8; n++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((n & 1) == 0 ? -1 : 1, (n & 2) == 0 ? -1 : 1, (n & 4) == 0 ? -1 : 1));
                Vector3 local = instance.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!initialized) { bounds = new Bounds(local, Vector3.zero); initialized = true; }
                else bounds.Encapsulate(local);
            }
        }
        if (!initialized || bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0)
            throw new InvalidOperationException("Classroom model has invalid bounds: " + label);
        Vector3 scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        Quaternion rotation = Quaternion.Euler(0, yaw, 0);
        Vector3 pivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        instance.transform.localScale = scale;
        instance.transform.localRotation = rotation;
        instance.transform.localPosition = bottom - rotation * Vector3.Scale(pivot, scale);
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) if (material != null) Destroy(material);
    }
}
