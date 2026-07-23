using System;

[Serializable]
public class PlayerProfilePackage
{
    public string nickname;
    public int avatarId;
    public bool isReady;
    public int wins;
    
    public string colorHex;
    
    public PlayerProfilePackage()
    {
        nickname = "";
        avatarId = 0;
        isReady = false;
        wins = 0;
        colorHex = "#FFFFFF";
    }

    public PlayerProfilePackage(string nickname, int avatarId, bool isReady, int wins, string colorHex)
    {
        this.nickname = nickname;
        this.avatarId = avatarId;
        this.isReady = isReady;
        this.wins = wins;
        this.colorHex = colorHex;
    }
}