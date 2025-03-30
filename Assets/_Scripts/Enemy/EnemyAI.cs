using UnityEngine;
using Unity.Behavior;

public class EnemyAI : MonoBehaviour
{
    public GameObject player;  // Gracz, za którym podąża przeciwnik
    private BehaviorGraphAgent behaviorGraph; // Komponent BehaviorGraph przypisany do obiektu
    


    void Update()
    {
        // Szukamy gracza w scenie
        player = GameObject.FindWithTag("Player");
        behaviorGraph = GetComponent<BehaviorGraphAgent>();

        if (behaviorGraph != null && player != null)
        {
            behaviorGraph.SetVariableValue("Target", player);
        }
    }
}
