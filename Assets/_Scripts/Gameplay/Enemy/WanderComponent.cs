using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

public class WanderComponent : NetworkBehaviour
{
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    public bool isWandering = false;

    private NavMeshAgent agent;
    private float timer;

    private void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
    }

    private void Update()
    {
        if (!IsServerInitialized || !isWandering)
            return;
        timer += Time.deltaTime;

        if (timer >= wanderTimer)
        {
            Vector3 newPos = GetRandomPointOnNavMesh();

            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(newPos, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(newPos);
                timer = 0;
            }
        }
    }

    private Vector3 GetRandomPointOnNavMesh()
    {
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        int vertexIndex = Random.Range(0, navMeshData.vertices.Length);
        Vector3 randomPoint = navMeshData.vertices[vertexIndex];

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 2.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
    }
}
