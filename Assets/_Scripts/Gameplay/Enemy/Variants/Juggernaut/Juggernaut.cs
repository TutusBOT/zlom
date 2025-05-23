using System.Collections;
using UnityEngine;

public class Juggernaut : Enemy, IFlashlightDetectable
{
    [Header("Light Reaction Settings")]
    [SerializeField]
    private float _reactionThreshold = 1f;

    [SerializeField]
    private float _lightMemoryDuration = 3f; // How long before light exposure resets

    private Vector3 _lastLightSourcePosition;
    private float _lightExposureTime = 0f;
    private float _lastLightHitTime;
    private GameObject _currentLightSource;
    private bool _isReacting = false;
    private Coroutine _resetCoroutine;

    protected override void Update()
    {
        base.Update();

        // Reset light exposure if we haven't been hit in a while
        if (
            !_isReacting
            && _lightExposureTime > 0
            && Time.time - _lastLightHitTime > _lightMemoryDuration
        )
        {
            _lightExposureTime = 0f;
            _currentLightSource = null;
            Debug.Log("Juggernaut light exposure reset due to timeout");
        }
    }

    public void OnFlashlightHit(
        FlashlightController flashlight,
        Vector3 hitPoint,
        Vector3 hitNormal,
        float intensityFactor
    )
    {
        if (intensityFactor < 0.1f || _isReacting)
            return;

        // Update the last hit time
        _lastLightHitTime = Time.time;

        // Store both the GameObject reference AND the position
        if (_lightExposureTime == 0f && flashlight != null)
        {
            _currentLightSource = flashlight.gameObject;
            // Store the hit point position - this is where the flashlight hit the Juggernaut
            _lastLightSourcePosition = flashlight.transform.position;
            Debug.Log(
                $"Juggernaut noticed light source: {_currentLightSource.name} at position {_lastLightSourcePosition}"
            );
        }

        // Accumulate exposure time
        _lightExposureTime += Time.deltaTime;

        // Check if we've reached the reaction threshold
        if (_lightExposureTime >= _reactionThreshold)
        {
            TriggerLightReaction();
        }
    }

    private void TriggerLightReaction()
    {
        if (_isReacting || _currentLightSource == null)
            return;

        _isReacting = true;

        // Create a position target instead of using the GameObject directly
        GameObject positionTarget = new GameObject("LightReactionTarget");
        positionTarget.transform.position = _lastLightSourcePosition;

        // Set behavior variables with the position target rather than the player
        behaviorGraph.SetVariableValue("ActionState", ActionState.Special);
        behaviorGraph.SetVariableValue("ShouldPerformAction", true);
        behaviorGraph.SetVariableValue("MovementState", MovementState.Idle);
        behaviorGraph.SetVariableValue("Target", positionTarget); // Use the position marker
        behaviorGraph.SetVariableValue("IsBlinded", true);

        Debug.Log($"Juggernaut reacting to light at position: {_lastLightSourcePosition}");

        // Start reset coroutine
        if (_resetCoroutine != null)
            StopCoroutine(_resetCoroutine);

        _resetCoroutine = StartCoroutine(ResetAfterReaction(positionTarget));
    }

    private IEnumerator ResetAfterReaction(GameObject targetToDestroy = null)
    {
        // Wait for the special action to complete
        yield return new WaitForSeconds(5f);

        // Reset reaction state
        _isReacting = false;
        _lightExposureTime = 0f;
        _currentLightSource = null;

        // Reset behavior
        behaviorGraph.SetVariableValue("IsBlinded", false);

        // Clean up the temporary target object
        if (targetToDestroy != null)
        {
            Destroy(targetToDestroy);
        }

        Debug.Log("Juggernaut light reaction complete");
    }

    protected override void ResetBehaviorGraph()
    {
        base.ResetBehaviorGraph();
        behaviorGraph.SetVariableValue("MovementState", MovementState.Idle);

        // Also reset light-related variables
        _lightExposureTime = 0f;
        _isReacting = false;
        _currentLightSource = null;

        if (_resetCoroutine != null)
        {
            StopCoroutine(_resetCoroutine);
            _resetCoroutine = null;
        }
    }

    public override void Respawn()
    {
        base.Respawn();
        _lightExposureTime = 0f;
        _isReacting = false;
        _currentLightSource = null;

        if (_resetCoroutine != null)
        {
            StopCoroutine(_resetCoroutine);
            _resetCoroutine = null;
        }
    }
}
