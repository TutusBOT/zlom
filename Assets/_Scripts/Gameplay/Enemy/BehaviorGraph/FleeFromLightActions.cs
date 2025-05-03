using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(
    name: "Flee From Light",
    story: "[Agent] flees from light",
    category: "Actions/Movement",
    id: "46ba8c71d293e47f8b5a98762c3f4d1e"
)]
public partial class FleeFromLightAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<NavMeshAgent> NavAgent;

    [SerializeReference]
    public BlackboardVariable<LightDetector> LightDetector;

    public float fleeDistance = 10f;
    public float updatePathInterval = 0.5f;

    private float updateTimer;
    private Vector3 lastKnownLightPosition;
    private bool hasFleePosition = false;

    protected override Status OnStart()
    {
        updateTimer = 0;
        hasFleePosition = false;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || NavAgent.Value == null)
            return Status.Failure;

        // Get light detector
        LightDetector detector = LightDetector.Value;
        if (detector == null)
        {
            detector = Agent.Value.GetComponent<LightDetector>();
            if (detector != null)
                LightDetector.Value = detector;
        }

        if (detector == null)
            return Status.Failure;

        // If in light, store position for fleeing
        if (detector.IsInLight && detector.CurrentLightSource != null)
        {
            lastKnownLightPosition = detector.CurrentLightSource.transform.position;
            hasFleePosition = true;
        }

        // If we don't have a position to flee from, we're done
        if (!hasFleePosition)
            return Status.Success;

        // If we're no longer in light and not in memory duration
        if (!detector.IsInLight && !detector.WasRecentlyInLight)
        {
            // Stop fleeing once we're safely away
            return Status.Success;
        }

        // Update flee path on interval
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0)
        {
            // Calculate direction away from light
            Vector3 dirAwayFromLight = (
                Agent.Value.transform.position - lastKnownLightPosition
            ).normalized;

            Vector3 fleeTarget = Agent.Value.transform.position + dirAwayFromLight * fleeDistance;

            // Find valid position on NavMesh
            if (
                NavMesh.SamplePosition(
                    fleeTarget,
                    out NavMeshHit hit,
                    fleeDistance,
                    NavMesh.AllAreas
                )
            )
            {
                NavAgent.Value.SetDestination(hit.position);
            }

            updateTimer = updatePathInterval;
        }

        return Status.Running;
    }
}
