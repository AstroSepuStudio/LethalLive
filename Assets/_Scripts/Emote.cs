using UnityEngine;

[CreateAssetMenu]
public class Emote : ScriptableObject
{
    public string emoteName;
    public Sprite icon;
    public string animatorTrigger;
    public bool loop;
    public bool dynamic;
}
