using UnityEngine;

public static class AvatarUtils
{
    public static Sprite ByteArrayToSprite(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            Debug.Log("byte array is empty or null");
            return null;
        }

        Texture2D src = new(2, 2, TextureFormat.RGBA32, false);
        if (!src.LoadImage(imageData))
        {
            Debug.Log("Could NOT load the image form the image data.");
            return null;
        }

        RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height);
        Graphics.Blit(src, rt, new Vector2(1, -1), new Vector2(0, 1));

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D dst = new(src.width, src.height);
        dst.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        dst.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return Sprite.Create(dst, new Rect(0, 0, dst.width, dst.height), new Vector2(0.5f, 0.5f));
    }
}
