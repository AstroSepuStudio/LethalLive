using UnityEngine;

public class OnTopFollow : MonoBehaviour
{
    private void LateUpdate()
    {
        if (GameManager.Instance.LocalPlayer == null) return;

        transform.rotation = GameManager.Instance.LocalPlayer.PlayerCamera.transform.rotation;
    }
}
