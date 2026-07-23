using UnityEngine;
using Photon.Pun;

public class PlayerAvatarSetup : MonoBehaviourPun
{
    [Header("Setup References")]
    [SerializeField] private AvatarDatabase avatarDatabase;
    [SerializeField] private Transform characterBaseParent;
    [SerializeField] private Animator rootAnimator;

    private GameObject _currentModelInstance;

    private void Start()
    {
        ApplyAvatarVisuals();
    }

    private void ApplyAvatarVisuals()
    {
        int avatarId = 0;
        string colorHex = "#FFFFFF";
        
        if (photonView.Owner.CustomProperties.TryGetValue("AvatarID", out object avatarIdValue))
            avatarId = (int)avatarIdValue;
        
        if (photonView.Owner.CustomProperties.TryGetValue("ColorHex", out object colorHexValue))
            colorHex = (string)colorHexValue;

        if (avatarDatabase != null && avatarId >= 0 && avatarId < avatarDatabase.availableAvatars.Length)
        {
            AvatarData data = avatarDatabase.availableAvatars[avatarId];
            
            if (_currentModelInstance != null)
                Destroy(_currentModelInstance);
            
            _currentModelInstance = Instantiate(data.modelPrefab, characterBaseParent);
            
            SkinnedMeshRenderer instancedRenderer = _currentModelInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            
            if (instancedRenderer != null && ColorUtility.TryParseHtmlString(colorHex, out Color parsedColor))
            {
                instancedRenderer.material.color = parsedColor;
            }
            
            if (rootAnimator != null && data.characterAvatar != null)
            {
                rootAnimator.avatar = data.characterAvatar;
                rootAnimator.Rebind(); 
            }
        }
    }
}