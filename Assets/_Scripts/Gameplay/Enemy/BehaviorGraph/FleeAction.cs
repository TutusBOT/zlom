using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Flee", story: "[Agent] is [FleeBehavior] from [Target]", category: "Action", id: "f7aba44eefc0a45a554d2ab97fe368bc")]
public partial class FleeAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<FleeComponent> FleeBehavior;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    private FleeComponent fleeComponent;

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null)
        {
            Debug.LogWarning("FleeAction: Missing Agent or Target reference");
            return Status.Failure;
        }

        if (FleeBehavior.Value != null)
        {
            fleeComponent = FleeBehavior.Value;
        }
        else
        {
            fleeComponent = Agent.Value.GetComponent<FleeComponent>();

            // Cache it for future use
            if (fleeComponent != null && FleeBehavior.Value == null)
                FleeBehavior.Value = fleeComponent;
        }

        if (fleeComponent == null)
        {
            Debug.LogWarning("FleeAction: No FleeComponent found on Agent");
            return Status.Failure;
        }

        fleeComponent.StartFleeing(Target.Value.transform);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (fleeComponent == null || Target.Value == null)
            return Status.Failure;

        if (fleeComponent.IsFleeingComplete())
            return Status.Success;

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (fleeComponent != null)
        {
            fleeComponent.StopFleeing();
        }
    }
}
