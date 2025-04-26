using UnityEngine;
using Unity.Behavior;

public class EnemyAI : MonoBehaviour
{
    public GameObject player;
    private BehaviorGraphAgent behaviorGraph;
    


    void Update()
    {
        player = GameObject.FindWithTag("Player");
        behaviorGraph = GetComponent<BehaviorGraphAgent>();

        if (behaviorGraph != null && player != null)
        {
            behaviorGraph.SetVariableValue("Target", player);
        }
    }
}
