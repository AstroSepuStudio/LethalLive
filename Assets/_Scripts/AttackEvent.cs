using UnityEngine;

public struct AttackEvent
{
    public EntityStats SourceStats;
    public EntityStats TargetStats;
    public AttackStat AttackStat_;

    public Vector3 Position;
    public int TeamID; // -1 = no team

    public static AttackEvent From(EntityStats source, EntityStats target, AttackStat attack, int teamID = -1)
        => new() { 
            SourceStats = source, 
            TargetStats = target, 
            Position = target.transform.position, 
            AttackStat_ = attack, 
            TeamID = teamID };

    public static AttackEvent From(PlayerData pData, EntityStats target, AttackStat attack)
        => new() { 
            SourceStats = pData != null ? pData.Player_Stats : null, 
            TargetStats = target, 
            Position = pData != null ? pData.transform.position : Vector3.zero, 
            AttackStat_ = attack, 
            TeamID = (int)pData.Team };

    public static AttackEvent From(Vector3 position, EntityStats target, AttackStat attack)
        => new()
        {
            SourceStats = null,
            TargetStats = target,
            Position = position,
            AttackStat_ = attack,
            TeamID = -1
        };

    public static AttackEvent None => new() {
        SourceStats = null,
        TargetStats = null,
        Position = Vector3.zero, 
        AttackStat_ = null,
        TeamID = -1 };
}
