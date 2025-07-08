using UnityEngine;

public class AnimatorEventHelper : MonoBehaviour
{
    [SerializeField] PlayerData playerData;

    public void PunchDetectionEvent()
    {
        playerData.Punch_Manager.PunchDetection();
    }
}
