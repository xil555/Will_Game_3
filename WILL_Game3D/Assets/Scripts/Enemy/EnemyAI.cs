using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public Transform[] waypoints;
    
    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float killDistance = 1.5f;
    
    private int currentWaypointIndex = 0;

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }

        // Check for kill condition
        if (distanceToPlayer <= killDistance)
        {
            KillPlayer();
        }
    }

    void Patrol()
    {
        if (waypoints.Length == 0) return;

        agent.SetDestination(waypoints[currentWaypointIndex].position);

        // Check if agent reached the waypoint
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    void KillPlayer()
    {
        Debug.Log("Player is dead!");
        //TODO: Implement player death scene
        
    }
}
