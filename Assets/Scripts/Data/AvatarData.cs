using UnityEngine;

[CreateAssetMenu(fileName = "NewAvatar", menuName = "Data/Avatar Data")]
public class AvatarData : ScriptableObject
{
    public string avatarName;
    public Sprite avatarIcon;
    
    [Header("3D Assets")]
    public GameObject modelPrefab;
    public Avatar characterAvatar;
}