using UnityEngine;

[CreateAssetMenu]
public class Emote : ScriptableObject
{
    public string emoteName;
    public string animatorTrigger;
    public bool loop;
    public bool dynamic;
}
