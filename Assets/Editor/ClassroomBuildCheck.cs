#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
public static class ClassroomBuildCheck
{
    public static void Validate()
    {
        foreach (string kind in new[] { "floor", "wall", "board", "window", "furniture" })
        {
            GameObject model = Resources.Load<GameObject>("Classroom/" + kind + "/" + kind + "Model");
            Texture2D texture = Resources.Load<Texture2D>("Classroom/" + kind + "/BaseColor");
            if (model == null || texture == null) throw new InvalidOperationException("Missing classroom asset: " + kind);
            long triangles = 0;
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
                triangles += filter.sharedMesh.triangles.Length / 3;
            if (triangles <= 0 || triangles > 20000 || texture.width > 1024 || texture.height > 1024)
                throw new InvalidOperationException("Classroom asset exceeds web budget: " + kind);
            Debug.Log("CLASSROOM_BUILD_CHECK " + kind + " triangles=" + triangles + " texture=" + texture.width + "x" + texture.height);
        }
    }
}
#endif
