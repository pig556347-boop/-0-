using System.Collections.Generic;
using UnityEngine;

// Presentation resources only. Keeps the supplied exports intact and caches neutral
// alpha masks so the original pink bars/icons can follow the game's warning palette.
public sealed class SilentHUDSkin : MonoBehaviour
{
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private readonly List<Texture2D> masks = new List<Texture2D>();
    public Sprite Load(string part, bool neutral = false, float border = 0)
    {
        string key = part + (neutral ? " neutral" : "");
        if (sprites.TryGetValue(key, out Sprite cached)) return cached;
        Texture2D source = Resources.Load<Texture2D>("UI/Silent/" + part);
        if (source == null) { Debug.LogError("Silent HUD missing resource: " + part); return null; }
        Texture2D texture = source;
        if (neutral)
        {
            Color32[] pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                // Preserve the black exported drop shadow while neutralizing colored artwork.
                byte brightness = (byte)Mathf.Max(pixels[i].r, Mathf.Max(pixels[i].g, pixels[i].b));
                pixels[i] = new Color32(brightness, brightness, brightness, pixels[i].a);
            }
            texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            texture.name = "Silent " + key; texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels); texture.Apply(false, true); masks.Add(texture);
        }
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        sprite.name = "Silent " + key; sprites.Add(key, sprite); return sprite;
    }
    private void OnDestroy()
    {
        foreach (Sprite sprite in sprites.Values) Destroy(sprite);
        foreach (Texture2D mask in masks) Destroy(mask);
    }
}
