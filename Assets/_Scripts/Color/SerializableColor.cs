using UnityEngine;

[System.Serializable]
public struct SerializableColor
{
    public float r, g, b, a;

    public SerializableColor(Color c)
    {
        r = c.r; g = c.g; b = c.b; a = c.a;
    }

    public Color ToColor() => new Color(r, g, b, a);
}
