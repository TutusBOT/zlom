using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Charge",
    story: "[Agent] charges at [Target]",
    category: "Action",
    id: "a7c19b5e4fd9124a8c683e8d5b7c8ef6"
)]
public partial class ChargeAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<GameObject> Target;

    [SerializeReference]
    public BlackboardVariable<ChargeComponent> ChargeComponent;

    private ChargeComponent chargeComp;
    private bool chargeStarted = false;

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null)
        {
            Debug.LogWarning("ChargeAction: Missing Agent or Target reference");
            return Status.Failure;
        }

        // Get the charge component
        chargeComp = ChargeComponent.Value;
        if (chargeComp == null)
        {
            chargeComp = Agent.Value.GetComponent<ChargeComponent>();

            if (chargeComp != null && ChargeComponent.Value == null)
                ChargeComponent.Value = chargeComp;
        }

        if (chargeComp == null)
        {
            Debug.LogWarning("ChargeAction: No ChargeComponent found on Agent");
            return Status.Failure;
        }

        // Check if charge is available
        if (!chargeComp.CanCharge())
        {
            Debug.Log("ChargeAction: Charge not available yet");
            return Status.Failure;
        }

        // Start the charge
        chargeComp.StartCharge(Target.Value.transform);
        chargeStarted = true;

        Debug.Log("ChargeAction: Charge started");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!chargeStarted || chargeComp == null)
            return Status.Failure;

        // Check if charge is still in progress
        if (chargeComp.CanCharge())
        {
            // Charge has completed
            chargeStarted = false;
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (chargeComp != null && chargeStarted)
        {
            // If the behavior gets interrupted, cancel the charge
            chargeComp.CancelCharge();
            chargeStarted = false;
        }
    }
}
