using UnityEngine;

// The arena is framed once; actor movement/facing never moves this camera rig.
public sealed class ClassroomCameraFrame : MonoBehaviour
{
    private Camera view;
    public void Initialize(Camera camera)
    {
        view = camera;
        transform.position = new Vector3(0, 3.2f, 0);
        ApplyFrame();
    }
    private void LateUpdate() { ApplyFrame(); }
    private void ApplyFrame()
    {
        if (view == null) return;
        // Include the 28m arena plus side padding, rear wall, and HUD clearance.
        view.orthographicSize = Mathf.Max(9.4f, 15f / Mathf.Max(.1f, view.aspect));
    }
}
