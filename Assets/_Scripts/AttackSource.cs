using UnityEngine;

public struct AttackSource
{
    public EntityStats Stats;
    public Vector3 Position;
    public int TeamID; // -1 = no team

    public static AttackSource From(EntityStats stats, int teamID = -1)
        => new() { Stats = stats, Position = stats.transform.position, TeamID = teamID };

    public static AttackSource From(PlayerData pData)
        => new() { Stats = pData.Player_Stats, Position = pData.transform.position, TeamID = (int)pData.Team };

    public static AttackSource None => new() { Stats = null, Position = Vector3.zero, TeamID = -1 };
}
