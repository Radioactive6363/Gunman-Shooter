using System;

[Serializable]
public class PlayerProfilePackage
{
    public string nickname;
    public int avatarId;
    public bool isReady;
    public int wins;
    
    public PlayerProfilePackage()
    {
        nickname = "";
        avatarId = 0;
        isReady = false;
        wins = 0;
    }

    public PlayerProfilePackage(string nickname, int avatarId, bool isReady, int wins)
    {
        this.nickname = nickname;
        this.avatarId = avatarId;
        this.isReady = isReady;
        this.wins = wins;
    }
}