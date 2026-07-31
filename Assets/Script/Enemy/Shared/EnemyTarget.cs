using UnityEngine;

public class EnemyTarget : MonoBehaviour
{
    [SerializeField] private Transform target;

    public Transform Target => target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
