using UnityEngine;

public static class Sniper_RuntimeSprites
{
    private static Sprite ringSprite;

    public static Sprite GetRingSprite()
    {
        if (ringSprite != null)
            return ringSprite;

        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[64 * 64];
        int w = 64;
        int h = 64;
        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        float rOuter = 26f;
        float rInner = 20f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                byte a = 0;
                if (d <= rOuter && d >= rInner)
                    a = 255;
                else if (d < rInner)
                    a = 40;

                pixels[y * w + x] = new Color32(255, 255, 255, a);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        ringSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        ringSprite.name = "Sniper_RuntimeRing";
        return ringSprite;
    }
}