using UnityEngine;

public interface IMapFollowTarget
{
    Transform FollowTransform { get; }
    bool IsAvailable { get; }
}