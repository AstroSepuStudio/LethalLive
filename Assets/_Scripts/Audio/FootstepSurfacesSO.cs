using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/FootstepSurfaces")]
public class FootstepSurfacesSO : ScriptableObject
{
    public enum FootstepClipType { Walk, Sprint, Jump, Land }

    [System.Serializable]
    public struct SurfaceEntry
    {
        public string surfaceTag;
        public AudioSFX[] walkSFX;
        public AudioSFX[] sprintSFX;
        public AudioSFX[] jumpSFX;
        public AudioSFX[] landSFX;
    }

    [SerializeField] SurfaceEntry[] surfaces;
    [SerializeField] SurfaceEntry fallbackSFX;

    void OnEnable() => BuildLookup();

    Dictionary<string, SurfaceEntry> entryLookup;

    void BuildLookup()
    {
        entryLookup = new();
        if (surfaces == null) return;
        foreach (var entry in surfaces)
            if (!string.IsNullOrEmpty(entry.surfaceTag))
            {
                entryLookup[entry.surfaceTag] = entry;
            }
    }

    public AudioSFX GetRandom(string surfaceTag, FootstepClipType type = FootstepClipType.Walk)
    {
        if (entryLookup == null) BuildLookup();

        SurfaceEntry entry = fallbackSFX;

        if (!string.IsNullOrEmpty(surfaceTag))
        {
            for (int i = 0; i < surfaces.Length; i++)
            {
                if (surfaces[i].surfaceTag == surfaceTag)
                {
                    entry = surfaces[i];
                    break;
                }
            }
        }

        AudioSFX[] clips = type switch
        {
            FootstepClipType.Sprint => entry.sprintSFX?.Length > 0 ? entry.sprintSFX : entry.walkSFX,
            FootstepClipType.Jump => entry.jumpSFX?.Length > 0 ? entry.jumpSFX : entry.walkSFX,
            FootstepClipType.Land => entry.landSFX?.Length > 0 ? entry.landSFX : entry.walkSFX,
            _ => entry.walkSFX,
        };

        if (clips == null || clips.Length == 0) clips = fallbackSFX.walkSFX;
        if (clips == null || clips.Length == 0) return null;

        return clips[Random.Range(0, clips.Length)];
    }
}
