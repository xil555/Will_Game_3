using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class PoliceCarFollow : MonoBehaviour
{

    [Header("Waypoints")]
    // holds the waypoints for the police car to follow
    [SerializeField] private Transform[] waypoints;
    // keeps track of the current waypoint index
    private int currentWaypointIndex = 0;   

    // speed at which the police car moves
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 5f;

    void Update()
    {
        
        if (waypoints.Length == 0) { return; }
        
        Vector3 targetDirection = waypoints[currentWaypointIndex].position - transform.position;

        //handles rotation
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, 
            targetRotation, 
            rotationSpeed * Time.deltaTime);
        }

        //handles movement
        transform.position = Vector3.MoveTowards(
            transform.position, 
            waypoints[currentWaypointIndex].position, 
            speed * Time.deltaTime
        );

        //waypoint logic 
        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < 0.1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        } 
    }
}
