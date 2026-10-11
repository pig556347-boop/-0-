using UnityEngine;
public sealed class OwnedMaterial : MonoBehaviour
{
    public Material Material { get; set; }
    private void OnDestroy() { if (Material != null) Destroy(Material); }
}
