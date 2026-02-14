using UnityEngine;

public static class PlayerTeamColorUtils
{
    public static Color GetPlayerTeamColor(PlayerTeam playerTeam)
    {
        return playerTeam switch
        {
            PlayerTeam.White => Color.white,
            PlayerTeam.Blue => Color.blue,
            PlayerTeam.Red => Color.red,
            PlayerTeam.Green => Color.green,
            PlayerTeam.Yellow => Color.yellow,
            PlayerTeam.Pink => Color.pink,
            _ => Color.white
        };
    }

    public static Color GetPlayerTeamTextColor(PlayerTeam playerTeam)
    {
        return playerTeam switch
        {
            PlayerTeam.White => Color.black,
            PlayerTeam.Blue => Color.white,
            PlayerTeam.Red => Color.white,
            PlayerTeam.Green => Color.black,
            PlayerTeam.Yellow => Color.black,
            PlayerTeam.Pink => Color.black,
            _ => Color.white
        };
    }
}
