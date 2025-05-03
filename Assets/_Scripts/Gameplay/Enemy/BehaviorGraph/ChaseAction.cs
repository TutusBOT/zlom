using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Chase",
    story: "[Agent] is chasing [Target]",
    category: "Action",
    id: "e8c19a5b3fd7124a9b582e9c4a6b7df5"
)]
public partial class ChaseAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<GameObject> Target;

    [SerializeReference]
    public BlackboardVariable<ChaseComponent> ChaseComponent;

    private ChaseComponent chaseComp;

    protected override Status OnStart()
    {
        // Validate required references
        if (Agent.Value == null || Target.Value == null)
        {
            Debug.LogWarning("ChaseAction: Missing Agent or Target reference");
            return Status.Failure;
        }

        // Get or find chase component
        chaseComp = ChaseComponent.Value;
        if (chaseComp == null)
        {
            chaseComp = Agent.Value.GetComponent<ChaseComponent>();

            if (chaseComp != null && ChaseComponent.Value == null)
                ChaseComponent.Value = chaseComp;
        }

        if (chaseComp == null)
        {
            Debug.LogWarning("ChaseAction: No ChaseComponent found on Agent");
            return Status.Failure;
        }

        // Start chasing
        chaseComp.StartChasing(Target.Value.transform);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // Check if any references are invalid
        if (chaseComp == null || Target.Value == null)
            return Status.Failure;

        // Check if target is lost
        if (chaseComp.IsTargetLost())
            return Status.Failure;

        // Check if we've reached the target
        if (chaseComp.IsTargetReached())
            return Status.Success;

        // Still chasing
        return Status.Running;
    }

    protected override void OnEnd()
    {
        // Properly stop chasing when this node ends
        if (chaseComp != null)
            chaseComp.StopChasing();
    }
}
