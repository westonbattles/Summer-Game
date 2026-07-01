using UnityEngine;

public interface IEnemyAttack
{
    bool TryExecute();
    virtual void Cancel() { }
    void SetTarget(Transform target);
}