using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/StoreItem")]
public class StoreItemSO : ScriptableObject
{
    public ItemSO item;

    [Header("Pricing")]
    [Min(0)] public int minPrice = 10;
    [Min(0)] public int maxPrice = 50;

    [Tooltip("If true the price is fixed to minPrice and maxPrice is ignored.")]
    public bool fixedPrice = false;

    public int RolledPrice => fixedPrice
        ? minPrice
        : Random.Range(minPrice, maxPrice + 1);
}
