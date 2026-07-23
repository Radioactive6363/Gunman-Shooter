using UnityEngine;
using Photon.Pun;

public class PlayerAvatarSetup : MonoBehaviourPun
{
    [Header("Setup References")]
    [SerializeField] private AvatarDatabase avatarDatabase;
    [SerializeField] private Transform characterBaseParent; // Asigna aquí el objeto "CharacterBase"
    [SerializeField] private Animator rootAnimator; // El Animator en la raíz de tu PlayerPrefab

    private GameObject _currentModelInstance;

    private void Start()
    {
        ApplyAvatarVisuals();
    }

    private void ApplyAvatarVisuals()
    {
        if (photonView.Owner.CustomProperties.TryGetValue("AvatarID", out object avatarIdValue))
        {
            int avatarId = (int)avatarIdValue;
            
            if (avatarDatabase != null && avatarId >= 0 && avatarId < avatarDatabase.availableAvatars.Length)
            {
                AvatarData data = avatarDatabase.availableAvatars[avatarId];
                
                if (_currentModelInstance != null)
                {
                    Destroy(_currentModelInstance);
                }
                
                _currentModelInstance = Instantiate(data.modelPrefab, characterBaseParent);
                
                if (rootAnimator != null && data.characterAvatar != null)
                {
                    rootAnimator.avatar = data.characterAvatar;
                    rootAnimator.Rebind(); 
                }
            }
        }
    }
}