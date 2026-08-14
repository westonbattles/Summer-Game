using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ItemChoiceCardView : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text displayName;
    [SerializeField] TMP_Text shortDescription;
    [SerializeField] Image rarityAccent;
    [SerializeField] Button button;

    ItemDefinition definition;
    Action<ItemDefinition> onChosen;

    public void Populate(ItemDefinition def, ItemRarityPalette palette, Action<ItemDefinition> onChosenCallback)
    {
        definition = def;
        onChosen = onChosenCallback;

        icon.sprite = def.Icon;
        displayName.text = def.DisplayName;
        shortDescription.text = def.ShortDescription;
        rarityAccent.color = palette.GetColor(def.Rarity);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onChosen?.Invoke(definition)); // invokes onChosen when clicked
    }
}
