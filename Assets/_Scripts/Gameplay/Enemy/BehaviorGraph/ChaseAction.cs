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
        if (Agent.Value == null || Target.Value == null)
        {
            Debug.LogWarning("ChaseAction: Missing Agent or Target reference");
            return Status.Failure;
        }

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

        chaseComp.StartChasing(Target.Value.transform);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (chaseComp == null || Target.Value == null)
            return Status.Failure;

        if (chaseComp.IsTargetLost())
            return Status.Failure;

        if (chaseComp.IsTargetReached())
            return Status.Success;

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (chaseComp != null)
            chaseComp.StopChasing();
    }
}
