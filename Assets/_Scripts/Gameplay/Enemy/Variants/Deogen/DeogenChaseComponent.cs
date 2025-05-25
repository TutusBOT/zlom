using UnityEngine;

public class DeogenChaseComponent : ChaseComponent
{
    [SerializeField] private float minDynamicSpeed = 1f;
    [SerializeField] private float maxDynamicSpeed = 5f;
    [SerializeField] private float slowestAtDistance = 2f;
    [SerializeField] private float fastestAtDistance = 20f;

    protected override void Update()
    {
        base.Update(); // Keep existing chase logic

        if (!IsServerInitialized || !_isActive || _target == null || _agent == null)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, _target.transform.position);
        _agent.speed = CalculateDynamicSpeed(distanceToTarget);
    }

    private float CalculateDynamicSpeed(float distance)
    {
        if (distance <= slowestAtDistance) return minDynamicSpeed;
        if (distance >= fastestAtDistance) return maxDynamicSpeed;

        float t = (distance - slowestAtDistance) / (fastestAtDistance - slowestAtDistance);
        return Mathf.Lerp(minDynamicSpeed, maxDynamicSpeed, t);
    }
}
