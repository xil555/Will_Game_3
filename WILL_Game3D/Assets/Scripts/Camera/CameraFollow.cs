using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float followSpeed = 10f;
    public Vector3 offset;

    void LateUpdate()
    {
        // Follow position smoothly
        Vector3 desiredPosition = player.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        // Match player rotation smoothly
        Quaternion desiredRotation = Quaternion.Euler(0, player.eulerAngles.y, 0);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            followSpeed * Time.deltaTime
        );
    }
}
