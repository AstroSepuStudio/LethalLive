using UnityEngine;

public class WoodenCartEntity : FurnitureEntity
{
    protected override void HandleDeath(AttackEvent source)
    {
        if (_dying) return;
        _dying = true;

        OnDeath?.Invoke(source);

        if (!sfxMap.TryGetValue(SFXEvent.Died, out var group) || group.Clips.Length == 0) return;
        RpcFurnitureBreaks(Random.Range(0, group.Clips.Length));
        foreach (var item in dataSO.lootTable) TryDropItem(item);
    }
}
