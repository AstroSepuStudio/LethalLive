using UnityEngine;
using static FootstepSurfacesSO;

public class PlayerFootstepHandler : MonoBehaviour
{
    [SerializeField] FootstepSurfacesSO surfaces;

    string currentSurfaceTag = "";
    int footstepIndex = 0;

    public void SetSurface(string tag) => currentSurfaceTag = tag ?? "";

    public AudioSFX GetNextClip(FootstepClipType type = FootstepClipType.Walk)
    {
        return surfaces != null ? surfaces.GetRandom(currentSurfaceTag, type) : null;
    }
}
