using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/Item")]
public class ItemSO : ScriptableObject
{
    public GameObject itemPrefab;
    public string itemName;
    public Sprite icon;
    public bool isTwoHanded;
    public bool isSellable;
    public int minValue = 10;
    public int maxValue = 100;
    public bool hasPrimaryAction = false;
    public bool hasSecondaryAction = false;
    public bool pickable = true;
    public bool droppable = true;

    public Vector3 gOffset;
    public Vector3 gRotation;
}
