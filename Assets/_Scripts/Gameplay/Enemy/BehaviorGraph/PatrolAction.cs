using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Patrol Custom",
    story: "[Agent] is [PatrolComponent] along waypoints",
    category: "Action",
    id: "a5c62178f4e348e994edc3b1f21a7c9d"
)]
public partial class PatrolAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<GameObject> StartWaypoint;

    [SerializeReference]
    public BlackboardVariable<float> Speed = new BlackboardVariable<float>(3f);

    [SerializeReference]
    public BlackboardVariable<float> WaypointWaitTime = new BlackboardVariable<float>(1.0f);

    [SerializeReference]
    public BlackboardVariable<float> DistanceThreshold = new BlackboardVariable<float>(0.5f);

    [SerializeReference]
    public BlackboardVariable<PatrolComponent> PatrolComponent;

    private PatrolComponent patrolComp;

    protected override Status OnStart()
    {
        if (Agent.Value == null)
        {
            LogFailure("No agent assigned.");
            return Status.Failure;
        }

        patrolComp = PatrolComponent.Value;
        if (patrolComp == null)
        {
            patrolComp = Agent.Value.GetComponent<PatrolComponent>();

            if (patrolComp != null && PatrolComponent.Value == null)
                PatrolComponent.Value = patrolComp;
        }

        if (patrolComp == null)
        {
            LogFailure("No PatrolComponent found on agent.");
            return Status.Failure;
        }

        patrolComp.Initialize(Speed.Value);
        patrolComp.SetWaitTime(WaypointWaitTime.Value);
        patrolComp.SetDistanceThreshold(DistanceThreshold.Value);
        patrolComp.StartPatrolling(StartWaypoint.Value);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (patrolComp == null)
            return Status.Failure;

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (patrolComp != null)
            patrolComp.StopPatrolling();
    }
}
