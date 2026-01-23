using System.Collections.Generic;

[System.Serializable]
public class CharacterSkinSaveData
{
    public SerializableColor bodyMain;
    public SerializableColor bodySecond;
    public SerializableColor bodyThird;

    public SerializableColor facialMain;
    public SerializableColor facialSecond;
    public SerializableColor facialThird;
    public SerializableColor pupilColor;
    public float facialGlow;

    public List<AccessorySaveData> accessories = new();
}
