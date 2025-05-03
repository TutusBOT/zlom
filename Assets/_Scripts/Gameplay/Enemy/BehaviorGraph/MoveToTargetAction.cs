using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Move To Target",
    story: "[Agent] is moving to [Position]",
    category: "Action",
    id: "e8bc7532d91e48c9a23c7e16f5d34a0b"
)]
public partial class MoveToTargetAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<Vector3> Position;

    [SerializeReference]
    public BlackboardVariable<float> StoppingDistance = new BlackboardVariable<float>(1.0f);

    private NavMeshAgent _agent;
    private bool _destinationSet = false;

    protected override Status OnStart()
    {
        if (Agent.Value == null || Position.Value == null)
        {
            Debug.LogWarning("MoveToTargetAction: Missing Agent or Target reference");
            return Status.Failure;
        }

        _agent = Agent.Value.GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogWarning("MoveToTargetAction: No NavMeshAgent found on Agent");
            return Status.Failure;
        }

        _agent.stoppingDistance = StoppingDistance.Value;
        _agent.SetDestination(Position.Value);
        _destinationSet = true;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_agent == null || Position.Value == null)
            return Status.Failure;

        _agent.SetDestination(Position.Value);

        if (
            !_agent.pathPending
            && _agent.remainingDistance <= StoppingDistance.Value
            && (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.1f)
        )
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_agent != null && _destinationSet)
        {
            _agent.ResetPath();
            _destinationSet = false;
        }
    }
}
