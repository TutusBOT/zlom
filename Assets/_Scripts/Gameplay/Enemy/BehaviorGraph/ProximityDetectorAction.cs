using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "ProximityDetector",
    story: "Check using [Detector] and assign [Target]",
    category: "Action",
    id: "7a9fc354eb8d49e8b1c5e0cb49f7526d"
)]
public partial class ProximityDetectorAction : Action
{
    [SerializeReference]
    public BlackboardVariable<ProximityDetector> Detector;

    [SerializeReference]
    public BlackboardVariable<GameObject> Target;

    protected override Status OnUpdate()
    {
        Target.Value = Detector.Value.CheckProximity();
        return Target.Value == null ? Status.Failure : Status.Success;
    }
}
