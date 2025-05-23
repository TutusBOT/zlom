using UnityEngine;

public class Shadow : Enemy, IFlashlightDetectable
{
    private float _lightExposureTime = 0f;
    private float _reactionThreshold = 1f;

    public void OnFlashlightHit(
        FlashlightController flashlight,
        Vector3 hitPoint,
        Vector3 hitNormal,
        float intensityFactor
    )
    {
        if (intensityFactor < 0.1f)
            return;

        _lightExposureTime += Time.deltaTime;

        if (_lightExposureTime < _reactionThreshold)
            return;

        Die();
    }

    protected override void ResetBehaviorGraph()
    {
        base.ResetBehaviorGraph();
        behaviorGraph.SetVariableValue("MovementState", MovementState.Idle);
    }

    public override void Respawn()
    {
        base.Respawn();
        _lightExposureTime = 0f;
    }
}
