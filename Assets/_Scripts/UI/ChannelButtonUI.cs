using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChannelButtonUI : MonoBehaviour
{
    [SerializeField] PlayerChat_ChannelManager channelManager;

    [SerializeField] PlayerTeam playerTeam;
    [SerializeField] bool isGlobal;

    [SerializeField] TextMeshProUGUI labelTxt;
    [SerializeField] Image btnImage;

    bool initialized = false;

    int ChannelIndex => isGlobal ? 0 : playerTeam switch
    {
        PlayerTeam.White => 1,
        PlayerTeam.Red => 2,
        PlayerTeam.Green => 3,
        PlayerTeam.Blue => 4,
        PlayerTeam.Yellow => 5,
        PlayerTeam.Pink => 6,
        _ => 0
    };

    Color ChannelColor => isGlobal ? Color.white : PlayerTeamColorUtils.GetPlayerTeamColor(playerTeam);

    public void Initialize()
    {
        gameObject.SetActive(true);

        if (initialized) return;
        initialized = true;

        SetUpChannelButton();
        channelManager.OnSwitchChannel.AddListener(OnSwitchChannel);
    }

    public void HideChannel()
    {
        DeselectChannel();
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (initialized)
            channelManager.OnSwitchChannel.RemoveListener(OnSwitchChannel);
    }

    void OnSwitchChannel(int channelIndex)
    {
        if (channelIndex != ChannelIndex)
            DeselectChannel();
        else
            SelectChannel();
    }

    void SetUpChannelButton()
    {
        Color textColor = isGlobal ? Color.black : PlayerTeamColorUtils.GetPlayerTeamTextColor(playerTeam);

        btnImage.color = ChannelColor;
        labelTxt.color = textColor;
    }

    public void SwitchChannel() => channelManager.SwitchChannel(ChannelIndex);

    void DeselectChannel()
    {
        Color darkerClr = new (ChannelColor.r * 0.7f, ChannelColor.g * 0.7f, ChannelColor.b * 0.7f, ChannelColor.a);
        btnImage.color = darkerClr;
    }

    void SelectChannel()
    {
        btnImage.color = ChannelColor;
    }
}
