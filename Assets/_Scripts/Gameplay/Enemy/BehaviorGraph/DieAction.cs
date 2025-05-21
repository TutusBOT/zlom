using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Die",
    story: "[Agent] dies",
    category: "Action",
    id: "7c34a2e895f64d9a9df162c3b1a5432d"
)]
public partial class DieAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        Debug.Log("DieAction: Agent is dying");

        if (Agent.Value == null)
        {
            Debug.LogWarning("DieAction: Missing Agent reference");
            return Status.Failure;
        }

        // Get the Enemy component
        Enemy enemy = Agent.Value.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogWarning("DieAction: Agent does not have an Enemy component");
            return Status.Failure;
        }

        enemy.Die();

        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }
}
