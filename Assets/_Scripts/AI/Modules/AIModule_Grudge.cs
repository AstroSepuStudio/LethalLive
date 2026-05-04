using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AIModule_Grudge : AIModule
{
    readonly HashSet<PlayerData> blacklist = new();
    readonly HashSet<ItemBase> stolenItems = new();

    public bool HasGrudge(PlayerData player) => blacklist.Contains(player);
    public IReadOnlyCollection<PlayerData> Blacklist => blacklist;
    public IReadOnlyCollection<ItemBase> StolenItems => stolenItems;

    readonly Dictionary<PlayerData, float> timedGrudges = new();
    readonly HashSet<PlayerData> permanentPlayers = new();

    public void AddGrudge(PlayerData player, ItemBase item)
    {
        if (player != null) blacklist.Add(player);
        if (item != null) stolenItems.Add(item);
    }

    public void AddTimedGrudge(PlayerData player, float duration)
    {
        if (player == null) return;
        if (blacklist.Contains(player) && !timedGrudges.ContainsKey(player)) return;
        timedGrudges[player] = duration;
        blacklist.Add(player);
    }

    public override void OnModuleTick(AIBrain brain)
    {
        var expired = new List<PlayerData>();
        var keys = timedGrudges.Keys.ToList();
        foreach (var p in keys)
        {
            timedGrudges[p] -= GameTick.TickRate;
            if (timedGrudges[p] <= 0f)
                expired.Add(p);
        }
        foreach (var p in expired)
        {
            timedGrudges.Remove(p);
            if (!stolenItems.Any(i => i != null))
                blacklist.Remove(p);
        }

        PruneStale();
    }

    public void RemoveGrudgeForItem(ItemBase item)
    {
        if (item == null) return;
        stolenItems.Remove(item);

        if (stolenItems.Count == 0 && timedGrudges.Count == 0 && permanentPlayers.Count == 0)
            blacklist.Clear();
    }

    public void MergeFrom(AIModule_Grudge other)
    {
        foreach (var p in other.blacklist) blacklist.Add(p);
        foreach (var i in other.stolenItems) stolenItems.Add(i);
    }

    public void PruneStale()
    {
        stolenItems.RemoveWhere(i => i == null);
        if (stolenItems.Count == 0 && timedGrudges.Count == 0 && permanentPlayers.Count == 0)
            blacklist.Clear();
    }

    public override void OnModuleInit(AIBrain brain) { }
}
