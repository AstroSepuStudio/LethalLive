using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/Skin")]
public class SkinSO : ScriptableObject
{
    public string skinName;
    public GameObject modelPrefab;
    public Sprite icon;
}
