using UnityEngine;
using UnityEngine.UI;
using TMPro;

// each HUD slot gets one of these. Note that it polls the AbilityController each frame
// and it will mirror the current state.
public class AbilitySlotWidget : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] AbilitySlot slot;

    [Header("Source")]
    [SerializeField] AbilityController abilityController;

    [Header("UI References")]
    [SerializeField] Image iconImage;       // the ability's icon sprite
    [SerializeField] Image cooldownOverlay; // type: filled, radial 30, etc sits on top of the icon!
    [SerializeField] TextMeshProUGUI chargesText;  // only shown when MaxCharges > 1

    Sprite _lastIcon;

    private void Update()
    {
        AbilityInstance instance = abilityController != null
            ? abilityController.GetAbilityInstance(slot)
            : null;

        bool hasAbility = instance != null;

        // Empty slot
        if (iconImage != null)
        {
            iconImage.enabled = hasAbility;
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.enabled = hasAbility;
        }
        
        if (chargesText != null && !hasAbility)
        {
            chargesText.enabled = false;
        }

        if (!hasAbility) return;

        // Icon updates automatically when loot swaps the ability in this slot btw
        if (iconImage != null && instance.Icon != _lastIcon)
        {
            _lastIcon = instance.Icon;
            iconImage.sprite = _lastIcon;

            if (_lastIcon == null)
            {
                Debug.LogWarning($"Ability {instance.Ability.name} has no icon sprite assigned.", instance.Ability);
            }
        }

        // cooldown sweep (1 = just used, 0 = ready)
        float fraction = instance.CooldownFraction;
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = fraction;
        }

        // charges are hidden entirely for plain single-charge cooldown abilities.
        int maxCharges = instance.MaxCharges;
        if (chargesText != null)
        {
            bool showCharges = maxCharges > 1;
            chargesText.enabled = showCharges;
            if (showCharges)
            {
                chargesText.text = instance.CurrentCharges.ToString();
            }
        }
    }
}
