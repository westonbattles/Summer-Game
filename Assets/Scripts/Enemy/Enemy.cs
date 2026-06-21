using UnityEngine;

/// <summary>
/// Enemy is the super-class behind all enemies in the game (including bosses). We are trying to use composition except for this base level inheritance for object pooling and generalization purposes. 
/// This class is not meant to be used directly, but rather to be inherited by specific enemy types (e.g., Goblin, Orc, etc.) that will implement their unique behaviors and attributes.
/// </summary>
public class Enemy : MonoBehaviour
{
    [SerializeField] private int baseHP;
    [SerializeField] private float movementSpeed;

    [field: SerializeField] public int XPValue { get; private set; }
    [field: SerializeField] public bool AffixEligible { get; private set; }

    private int currentHP;

    private void Awake()
    {
        currentHP = baseHP;
    }
}
