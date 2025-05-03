using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Wandering",
    story: "[Self] is [WanderingBehavior]",
    category: "Action",
    id: "8c55a9fb9c324205774e215f488e1f36"
)]
public partial class WaneringAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Self;

    [SerializeReference]
    public BlackboardVariable<WanderComponent> WanderingBehavior;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd() { }
}
