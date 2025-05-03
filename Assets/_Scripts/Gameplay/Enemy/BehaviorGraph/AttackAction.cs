using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Attack",
    story: "[Agent] attacks [Target]",
    category: "Action",
    id: "b8c19a5b3fd7124a9b582e9c4a6b7df4"
)]
public partial class AttackAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<GameObject> Target;

    [SerializeReference]
    public BlackboardVariable<float> AttackCooldown;

    private AttackComponent _attackComponent;
    private bool _attackInitiated = false;

    protected override Status OnStart()
    {
        _attackInitiated = false;

        if (Agent.Value != null)
        {
            _attackComponent = Agent.Value.GetComponent<AttackComponent>();

            if (_attackComponent == null)
                return Status.Failure;

            // Update the cooldown value from blackboard if provided
            if (AttackCooldown.Value > 0)
                _attackComponent.cooldownDuration = AttackCooldown.Value;
        }
        else
        {
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || Target.Value == null || _attackComponent == null)
            return Status.Failure;

        // Update blackboard with current cooldown
        AttackCooldown.Value = _attackComponent.GetCooldownTime();

        if (!_attackInitiated)
        {
            _attackInitiated = _attackComponent.TryAttack(Target.Value);

            if (!_attackInitiated)
                return Status.Failure; // Couldn't attack (out of range or on cooldown)

            return Status.Running;
        }

        if (_attackComponent.IsAttackAnimationComplete())
        {
            _attackComponent.ResetAttackState();
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        // Reset animation when node completes
        if (_attackComponent != null)
            _attackComponent.StopAttack();
    }
}
