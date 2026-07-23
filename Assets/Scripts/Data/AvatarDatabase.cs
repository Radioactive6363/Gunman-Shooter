using UnityEngine;

[CreateAssetMenu(fileName = "AvatarDatabase", menuName = "Data/Avatar Database")]
public class AvatarDatabase : ScriptableObject
{
    public AvatarData[] availableAvatars;
}
