using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Scream",
    story: "[Agent] is screaming, causing stress to nearby players",
    category: "Action",
    id: "d4e91b7c3fd7124a9b582e9c4a6b7df7"
)]
public partial class ScreamAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<ScreamComponent> ScreamComponent;

    private ScreamComponent screamComp;

    protected override Status OnStart()
    {
        if (Agent.Value == null)
        {
            Debug.LogWarning("ScreamAction: Missing Agent reference");
            return Status.Failure;
        }

        // Get or find the ScreamComponent
        screamComp = ScreamComponent.Value;
        if (screamComp == null)
        {
            screamComp = Agent.Value.GetComponent<ScreamComponent>();

            if (screamComp != null && ScreamComponent.Value == null)
                ScreamComponent.Value = screamComp;
        }

        if (screamComp == null)
        {
            Debug.LogWarning("ScreamAction: No ScreamComponent found on Agent");
            return Status.Failure;
        }

        // Start the scream
        screamComp.StartScreaming();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (screamComp == null)
            return Status.Failure;

        // Check if scream is complete
        if (screamComp.IsScreamComplete())
            return Status.Success;

        // Still screaming
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (screamComp != null)
            screamComp.StopScreaming();
    }
}
