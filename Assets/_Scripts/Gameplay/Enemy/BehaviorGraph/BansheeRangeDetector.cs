using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "BansheeRangeDetector",
    story: "Update Range [Detector] and assign [Target]",
    category: "Action",
    id: "855e881edfc164e003958540ff48d612"
)]
public partial class BansheeRangeDetectorAction : Action
{
    [SerializeReference]
    public BlackboardVariable<BansheeRangeDetector> Detector;

    [SerializeReference]
    public BlackboardVariable<GameObject> Target;

    protected override Status OnUpdate()
    {
        Target.Value = Detector.Value.UpdateDetector();
        return Target.Value == null ? Status.Failure : Status.Success;
    }
}
