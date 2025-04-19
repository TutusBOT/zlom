using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class WanderingAI : MonoBehaviour {

    public float wanderRadius;
    public float wanderTimer;

    private Transform target;
    private NavMeshAgent agent;
    private float timer;

    // Use this for initialization
    void OnEnable () {
        agent = GetComponent<NavMeshAgent> ();
        timer = wanderTimer;
    }
  
    // Update is called once per frame
    void Update () {
        timer += Time.deltaTime;

        if (timer >= wanderTimer) {
            Vector3 newPos = GetRandomPointOnNavMesh();

            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(newPos, path) && path.status == NavMeshPathStatus.PathComplete) {
                agent.SetDestination(newPos);
                timer = 0;
            }
        }
    }


    public static Vector3 GetRandomPointOnNavMesh()
    {
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        int vertexIndex = Random.Range(0, navMeshData.vertices.Length);
        Vector3 randomPoint = navMeshData.vertices[vertexIndex];

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 2.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return GetRandomPointOnNavMesh();
    }
}