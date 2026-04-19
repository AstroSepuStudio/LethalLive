using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class VortexPack : NetworkBehaviour
{
    public static VortexPack Instance { get; private set; }

    [Header("Home Assignment")]
    [SerializeField] float homeDistanceFraction = 0.5f;
    [SerializeField] float homeDistanceTolerance = 0.2f;

    [Header("Scout Groups")]
    [SerializeField] int minGroupSize = 2;
    [SerializeField] int maxGroupSize = 3;
    [SerializeField] float minExploreDuration = 30f;
    [SerializeField] float maxExploreDuration = 120f;

    public RoomData HomeRoom { get; private set; }

    readonly List<VortexAI> members = new();
    readonly HashSet<ItemBase> homeItems = new();
    readonly List<VortexScoutGroup> activeGroups = new();

    VortexAI currentAlpha = null;

    public IReadOnlyList<VortexAI> Members => members;
    public VortexAI CurrentAlpha => currentAlpha;
    public bool IsAlpha(VortexAI v) => v != null && v == currentAlpha;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
    }

    void Update()
    {
        if (!isServer) return;

        for (int i = activeGroups.Count - 1; i >= 0; i--)
        {
            activeGroups[i].Tick(Time.deltaTime);
            if (!activeGroups[i].IsActive)
                activeGroups.RemoveAt(i);
        }
    }

    public void OnDungeonGenerated() => AssignHomeRoom();

    [Server]
    void AssignHomeRoom()
    {
        var gen = DungeonGenerator.Instance;
        if (gen == null || gen.SpawnedRooms == null) return;
        System.Random rng = gen.RNG;

        float maxDist = gen.MaxDistance;
        float prefDist = maxDist * homeDistanceFraction;
        float minBand = prefDist - maxDist * homeDistanceTolerance;
        float maxBand = prefDist + maxDist * homeDistanceTolerance;

        Vector3 startPos = gen.StartRoomPos;
        List<RoomData> candidates = new();
        List<RoomData> fallback = new();

        foreach (var kvp in gen.SpawnedRooms)
        {
            RoomData rd = kvp.Value;
            if (rd == null) continue;
            if (rd.Data.biome == Biome.Hallway || !rd.Data.ValidVortexHome) continue;

            float dist = Vector3.Distance(startPos, rd.transform.position);
            if (dist >= minBand && dist <= maxBand) 
                candidates.Add(rd);
            else 
                fallback.Add(rd);
        }

        if (candidates.Count > 0)
        {
            int index = rng.Next(candidates.Count);
            HomeRoom = candidates[index];
        }
        else if (fallback.Count > 0)
        {
            int index = rng.Next(fallback.Count);
            HomeRoom = fallback[index];
        }
        else HomeRoom = null;

        //Debug.Log($"[VortexPack] Home: {(HomeRoom != null ? HomeRoom.name : "none")}");
    }

    public bool IsItemAtHome(ItemBase item)
    {
        if (HomeRoom == null || item == null) return false;
        return Vector3.Distance(item.transform.position, HomeRoom.transform.position)
               <= DungeonGenerator.Instance.CellSize;
    }

    public bool IsVortexAtHome(VortexAI vortex)
    {
        if (HomeRoom == null || vortex == null) return false;
        var gen = DungeonGenerator.Instance;
        if (gen == null) return false;

        RoomData currentRoom = gen.GetRoomDataAtPosition(vortex.transform.position);
        bool atHome = currentRoom == HomeRoom;

        if (!atHome)
        {
            float distance = Vector3.Distance(vortex.transform.position, HomeRoom.transform.position);
            atHome = distance < gen.CellSize * 2;
        }

        return atHome;
    }

    public void ScanHomeItems()
    {
        homeItems.Clear();
        if (HomeRoom == null) return;
        Collider[] hits = Physics.OverlapSphere(
            HomeRoom.transform.position, DungeonGenerator.Instance.CellSize);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Item")) continue;
            var item = hit.GetComponent<ItemBase>();
            if (item != null && !item.HasOwner) homeItems.Add(item);
        }
    }

    public void CheckStolenItems(AIBrain brain)
    {
        if (homeItems.Count == 0) return;
        homeItems.RemoveWhere(item => item == null);
        foreach (var item in homeItems)
        {
            if (!item.HasOwner) continue;
            homeItems.Remove(item);
            brain.GetModule<AIModule_Patience>()?.DrainOnItemStolen();
            break;
        }
    }

    public void Register(VortexAI vortex)
    {
        if (vortex == null || members.Contains(vortex)) return;
        members.Add(vortex);
        ElectAlpha();

        if (HomeRoom != null && !IsVortexAtHome(vortex))
            vortex.TriggerReturnHome();
    }

    public void Unregister(VortexAI vortex)
    {
        if (!members.Remove(vortex)) return;

        foreach (var g in activeGroups)
            g.NotifyMemberDied(vortex);

        if (currentAlpha == vortex) { currentAlpha = null; ElectAlpha(); }
    }

    [Server]
    void ElectAlpha()
    {
        VortexAI best = null;
        float bestVal = -1f;
        foreach (var m in members)
        {
            if (m == null) continue;
            if (m.Alpha > bestVal) { bestVal = m.Alpha; best = m; }
        }

        if (best == currentAlpha) return;

        VortexAI previous = currentAlpha;
        currentAlpha = best;

        previous?.OnAlphaRoleRevoked();
        currentAlpha?.OnAlphaRoleGranted();

        //Debug.Log($"[VortexPack] Alpha -> {(currentAlpha != null ? currentAlpha.name : "none")} (a {bestVal:F0})");
    }

    [Server]
    public void DispatchScouts()
    {
        List<VortexAI> available = GetHomeScouts();

        if (available.Count < minGroupSize) return;

        Shuffle(available);

        int i = 0;
        while (i + minGroupSize <= available.Count)
        {
            int size = Mathf.Min(maxGroupSize, available.Count - i);
            if (size < minGroupSize) break;

            var slice = available.GetRange(i, size);
            var group = new VortexScoutGroup(slice, minExploreDuration, maxExploreDuration);
            foreach (var m in slice) m.SetGroup(group);

            activeGroups.Add(group);
            group.Dispatch();

            i += size;
        }

        //Debug.Log($"[VortexPack] Dispatched {activeGroups.Count} group(s) from {available.Count} scouts.");
    }

    public void OnGroupDisbanded(VortexScoutGroup group)
    {
        activeGroups.Remove(group);
        foreach (var m in group.AllMembers)
            m?.TriggerReturnHome();
    }

    List<VortexAI> GetHomeScouts()
    {
        var result = new List<VortexAI>();
        foreach (var m in members)
        {
            if (m == null || IsAlpha(m)) continue;
            if (IsInActiveGroup(m)) continue;
            if (!IsVortexAtHome(m)) continue;
            result.Add(m);
        }
        return result;
    }

    bool IsInActiveGroup(VortexAI v)
    {
        foreach (var g in activeGroups)
        {
            if (!g.IsActive) continue;
            foreach (var m in g.AllMembers)
                if (m == v) return true;
        }
        return false;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public class VortexScoutGroup
{
    public VortexAI Leader { get; private set; }

    readonly List<VortexAI> allMembers = new();
    public IReadOnlyList<VortexAI> AllMembers => allMembers;

    public bool IsActive { get; private set; }
    public bool CalledHome { get; private set; }

    int returnedCount = 0;
    float exploreTimer;

    public VortexScoutGroup(List<VortexAI> members, float minDuration, float maxDuration)
    {
        Leader = members[0];
        allMembers.AddRange(members);
        IsActive = true;
        exploreTimer = Random.Range(minDuration, maxDuration);
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;

        exploreTimer -= deltaTime;
        if (exploreTimer <= 0f)
            CallHome();
    }

    public void Dispatch()
    {
        Leader?.BeginGroupWander();
        Leader?.SetGroup(this);
            
        foreach (var m in allMembers)
        {
            if (m == null || m == Leader) continue;
            m.TriggerFollowLeader(Leader);
            m.SetGroup(this);
        }
    }

    public void NotifyMemberDied(VortexAI vortex)
    {
        for (int i = 0; i < allMembers.Count; i++)
        {
            if (allMembers[i] != vortex) continue;
            allMembers[i] = null;

            if (vortex == Leader)
            {
                Leader = null;
                CallHome();
            }
            return;
        }
    }

    public void NotifyMemberArrived(VortexAI vortex)
    {
        if (!IsActive) return;
        if (!allMembers.Contains(vortex)) return;
        returnedCount++;

        int aliveCount = 0;
        foreach (var m in allMembers)
            if (m != null) aliveCount++;

        Disband();
    }

    void CallHome()
    {
        if (!IsActive || CalledHome) return;

        CalledHome = true;
        foreach (var m in allMembers)
            m?.TriggerReturnHome();
    }

    void Disband()
    {
        IsActive = false;
        VortexPack.Instance?.OnGroupDisbanded(this);
    }
}
