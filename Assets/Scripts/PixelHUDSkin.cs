using System.Collections.Generic;
using UnityEngine;

// Source-pack sprites only: point filtering and four-screen-pixel corners keep the
// arcade silhouettes intact when the HUD is resized. No gameplay dependencies.
public sealed class PixelHUDSkin : MonoBehaviour
{
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    public Sprite Load(string part, float border = 0)
    {
        if (sprites.TryGetValue(part, out Sprite cached)) return cached;
        Texture2D texture = Resources.Load<Texture2D>("UI/Pixel/" + part);
        if (texture == null) { Debug.LogError("Pixel HUD missing resource: " + part); return null; }
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect,
            new Vector4(Mathf.Min(border, texture.width / 2f - 1), Mathf.Min(border, texture.height / 2f - 1),
                Mathf.Min(border, texture.width / 2f - 1), Mathf.Min(border, texture.height / 2f - 1)));
        sprite.name = "Pixel " + part; sprites.Add(part, sprite); return sprite;
    }
    private void OnDestroy()
    {
        foreach (Sprite sprite in sprites.Values) Destroy(sprite);
    }
}
