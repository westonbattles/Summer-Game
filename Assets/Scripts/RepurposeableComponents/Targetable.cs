using UnityEngine;
using System;

public class Targetable : MonoBehaviour
{
    [SerializeField, Tooltip("Optional override for highlight/aim position (e.g. chest pivot). Defaults to transform position.")]
    private Transform centerOverride;

    public event Action<bool> OnHighlightStateChanged;

    public bool IsHighlighted { get; private set; }

    public Vector3 TargetPosition => centerOverride != null ? centerOverride.position : transform.position;

    public void SetHighlighted(bool highlighted)
    {
        if (IsHighlighted == highlighted) return;

        IsHighlighted = highlighted;
        OnHighlightStateChanged?.Invoke(highlighted);
    }

    private void OnDisable()
    {
        if (IsHighlighted)
        {
            SetHighlighted(false);
        }
    }
}
