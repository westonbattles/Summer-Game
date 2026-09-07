using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ItemInfoTooltipHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private Image itemIcon;
    [SerializeField] private ItemInventory inventory;

    [Header("DOTween Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.05f;  // Super fast fade in
    [SerializeField] private float fadeOutDuration = 1.5f;   // Slow fade out
    [SerializeField] private float displayDelay = 1.0f;     // How long to stay visible before fading out

    private CanvasGroup canvasGroup;
    private Sequence fadeSequence;

    private void Awake()
    {
        // Get or add a CanvasGroup to control whole-HUD opacity
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnItemCountChanged += HandleItemCountChanged;
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnItemCountChanged -= HandleItemCountChanged;
        }

        // Kill any running animations if disabled to prevent leaks
        fadeSequence?.Kill();
    }

    private void HandleItemCountChanged(ItemDefinition definition, int count)
    {
        SetItemInfo(definition, count);
    }

    public void SetItemInfo(ItemDefinition definition, int count = 1)
    {
        if (definition == null) return;

        // Update Text and Icon content
        if (itemDescription != null)
        {
            string countText = count > 1 ? $" (x{count})" : string.Empty;
            itemDescription.text = $"<b>{definition.DisplayName}</b>{countText}\n{definition.ShortDescription}";
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = definition.Icon;
            itemIcon.enabled = (definition.Icon != null);
        }

        // Trigger DOTween Fade Sequence
        PlayFadeSequence();
    }

    private void PlayFadeSequence()
    {
        // Cancel previous animation if an item is picked up while already fading
        fadeSequence?.Kill();

        fadeSequence = DOTween.Sequence();

        fadeSequence
            // Step 1: Fade in SUPER fast to 100% opacity
            .Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad))
            // Step 2: Hold full opacity briefly so the player can read it
            .AppendInterval(displayDelay)
            // Step 3: Fade out slowly back to 0% opacity
            .Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
    }
}