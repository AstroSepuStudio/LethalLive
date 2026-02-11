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

        Texture2D tex = new(2, 2);
        bool isLoaded = tex.LoadImage(imageData);

        if (!isLoaded)
            return null;

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}
