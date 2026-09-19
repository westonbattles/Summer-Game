using UnityEngine;

public abstract class GroundMover : MonoBehaviour
{
    // TODO: at some point, we need to store this in some data struct or script such as an enemy stats script.
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] protected float turnSpeed = 360f;

    protected NavMeshPathProvider Path { get; private set; }

    private int movementLockCount;
    public bool IsMovementLocked => movementLockCount > 0;

    protected virtual void Awake() => Path = GetComponent<NavMeshPathProvider>();

    protected virtual void OnEnable() => movementLockCount = 0;
    protected virtual void OnDisable() => movementLockCount = 0;

    protected void MoveAlongPath()
    {
        transform.position += Path.DesiredDirection * moveSpeed * Time.deltaTime;
    }

    protected void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    public void AddMovementLock() => movementLockCount++;

    public void RemoveMovementLock()
    {
        movementLockCount = Mathf.Max(0, movementLockCount - 1);
        if (movementLockCount == 0) OnMovementUnlocked();
    }

    // Called when the last lock is released. (e.g. retarget player after root ends)
    protected virtual void OnMovementUnlocked() { }
}
