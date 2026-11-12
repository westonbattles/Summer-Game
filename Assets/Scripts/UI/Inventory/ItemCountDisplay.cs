using System;
using TMPro;
using UnityEngine;

public class ItemCountDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;


    void Start()
    {
        label.text = $"Item Count: {Player.Instance.Inventory.TotalItemCount}";
        Player.Instance.Inventory.OnItemCountChanged += UpdateText;
    }

    void OnDestroy()
    {
        if (Player.Instance.Inventory != false) Player.Instance.Inventory.OnItemCountChanged -= UpdateText;
    }

    void UpdateText(ItemDefinition item, int count)
    {
        label.text = $"Item Count: {Player.Instance.Inventory.TotalItemCount}";
    }
}
