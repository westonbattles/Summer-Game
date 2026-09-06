
using System;
using UnityEngine;


public interface IEnemyAttackWithIndicator : IEnemyAttack
{
    // GameObject is the indicator prefab
    public event Action<GameObject> showIndicator;
    public event Action hideIndicator;
}