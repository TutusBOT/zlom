using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Jumpscare",
    story: "[Agent] performs a jumpscare on [Target], causing stress",
    category: "Action",
    id: "c5f92a8b4e68125b8c693f5d7b8c6e98"
)]
public partial class JumpscareAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<GameObject> Target;

    [SerializeReference]
    public BlackboardVariable<JumpscareComponent> JumpscareComponent;

    private JumpscareComponent _jumpscareComp;

    protected override Status OnStart()
    {
        if (Agent.Value == null)
        {
            Debug.LogWarning("JumpscareAction: Missing Agent reference");
            return Status.Failure;
        }

        if (Target.Value == null)
        {
            Debug.LogWarning("JumpscareAction: Missing Target reference");
            return Status.Failure;
        }

        // Get or find the JumpscareComponent
        _jumpscareComp = JumpscareComponent.Value;
        if (_jumpscareComp == null)
        {
            _jumpscareComp = Agent.Value.GetComponent<JumpscareComponent>();

            if (_jumpscareComp != null && JumpscareComponent.Value == null)
                JumpscareComponent.Value = _jumpscareComp;
        }

        if (_jumpscareComp == null)
        {
            Debug.LogWarning("JumpscareAction: No JumpscareComponent found on Agent");
            return Status.Failure;
        }

        // Start the jumpscare
        Player targetPlayer = Target.Value.GetComponent<Player>();
        _jumpscareComp.StartJumpscare(targetPlayer);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_jumpscareComp == null)
            return Status.Failure;

        // Check if jumpscare is complete
        if (_jumpscareComp.IsJumpscareComplete())
            return Status.Success;

        // Check if target is still valid
        if (Target.Value == null)
        {
            _jumpscareComp.StopJumpscare();
            return Status.Failure;
        }

        // Still jumpscaring
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_jumpscareComp != null && _jumpscareComp.IsJumpscaring())
            _jumpscareComp.StopJumpscare();
    }
}
