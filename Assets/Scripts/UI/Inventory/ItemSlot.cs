using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// One icon is in the item tray. Owns its own visuals only and the tray decides when slots exist and what counts they show.
/// </summary>
[DisallowMultipleComponent]
public class ItemSlot : MonoBehaviour
{
    [SerializeField] Image rarityOutline;       // might change this to a shader at some point.
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI countText; // e.g "x#"

    [Header("Count Punch")]
    [SerializeField] float punchScale = 0.2f;
    [SerializeField] float punchDuration = 0.25f;

    private Tween punchTween;

    public ItemDefinition Definition { get; private set; }

    public void Bind(ItemDefinition itemDefinition, Color rarityColor)
    {
        Definition = itemDefinition;
        icon.sprite = itemDefinition.Icon;
        rarityOutline.color = rarityColor;
        SetCount(0, animate: false);
    }

    public void SetCount(int count, bool animate = true)
    {
        // hide "x1" and only show the label when we have stacking
        countText.gameObject.SetActive(count > 1);
        if (count > 1)
        {
            countText.text = $"x{count}";
        }

        if (animate)
        {
            punchTween?.Kill(complete: true);
            transform.localScale = Vector3.one;     // snap to known state b4 tweening
            punchTween = transform
                .DOPunchScale(Vector3.one * punchScale, punchDuration, vibrato: 1)
                .SetUpdate(true);
        }
    }

    private void OnDisable()
    {
        // kill and don't complete the punch on disable
        punchTween?.Kill();
        transform.localScale = Vector3.one;
    }
}
