using UnityEngine;
using UnityEngine.UI;

public class UICrosshair : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image crosshairCenter;
    [SerializeField] private Image chargeRing;
    
    [Header("Vignette Effect")]
    [SerializeField] private Image vignetteImage;
    [SerializeField] private float maxVignetteAlpha = 0.7f;

    [Header("Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color maxChargeColor = Color.red;
    [SerializeField] private bool scaleWithCharge = true;
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 0.5f;

    private void OnEnable()
    {
        PlayerShooting.OnWeaponChargeChanged += UpdateCrosshair;
    }

    private void OnDisable()
    {
        PlayerShooting.OnWeaponChargeChanged -= UpdateCrosshair;
    }

    private void UpdateCrosshair(float chargePercentage)
    {
        if (chargeRing != null)
        {
            chargeRing.fillAmount = chargePercentage;
            chargeRing.color = Color.Lerp(normalColor, maxChargeColor, chargePercentage);
        }
        
        if (crosshairCenter != null)
        {
            if (scaleWithCharge)
            {
                float currentScale = Mathf.Lerp(minScale, maxScale, chargePercentage);
                crosshairCenter.transform.localScale = new Vector3(currentScale, currentScale, 1f);
            }
            
            crosshairCenter.color = Color.Lerp(normalColor, maxChargeColor, chargePercentage);
        }
        
        if (vignetteImage != null)
        {
            Color vColor = vignetteImage.color;
            // Interpola el alfa (transparencia) desde 0 (invisible) hasta tu máximo definido
            vColor.a = Mathf.Lerp(0f, maxVignetteAlpha, chargePercentage);
            vignetteImage.color = vColor;
        }
    }
}
