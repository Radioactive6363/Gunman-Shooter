using System;

[Serializable]
public class PlayerProfilePackage
{
    public string nickname;
    public int avatarId;
    public bool isReady;
    
    public PlayerProfilePackage()
    {
        nickname = "";
        avatarId = 0;
        isReady = false;
    }

    public PlayerProfilePackage(string nickname, int avatarId, bool isReady)
    {
        this.nickname = nickname;
        this.avatarId = avatarId;
        this.isReady = isReady;
    }
}