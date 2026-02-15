using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBannerMicMod : PlayerBanner
{
    [SerializeField] Slider micSlider;

    PlayerData currentPlayer;

    public override void SetPlayer(GameManager.LobbyMemberData memberData)
    {
        base.SetPlayer(memberData);

        NetworkClient.spawned.TryGetValue(MemberData.netID, out NetworkIdentity identity);

        if (identity == null)
        {
            Debug.LogWarning("Target player not found");
            return;
        }

        currentPlayer = identity.GetComponent<PlayerData>();

        micSlider.SetValueWithoutNotify(currentPlayer.VoiceChatVolume);
    }

    public void VoiceChatVolumeChanged(float value)
    {
        currentPlayer.VoiceChatVolume = value;
    }
}
