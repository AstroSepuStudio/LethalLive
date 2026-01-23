using UnityEngine;

public class OnTopFollow : MonoBehaviour
{
    private void LateUpdate()
    {
        if (GameManager.Instance.playMod.LocalPlayer == null) return;

        transform.rotation = GameManager.Instance.playMod.LocalPlayer.PlayerCamera.transform.rotation;
    }
}
