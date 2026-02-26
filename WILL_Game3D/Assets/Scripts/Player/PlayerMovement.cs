using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 40f;

    [Header("Rotation")]
    public float rotationSpeed = 25f;
    public LayerMask groundMask;

    private Vector3 velocity;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandleMovement();
        RotateToMouse();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v).normalized;

        if (input.magnitude > 0.01f)
        {
            // Convert input to player-relative movement
            Vector3 moveDirection =
                transform.right * input.x +
                transform.forward * input.z;

            Vector3 targetVelocity = moveDirection * moveSpeed;

            velocity = Vector3.MoveTowards(
                velocity,
                targetVelocity,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            velocity = Vector3.zero;
        }

        transform.position += velocity * Time.deltaTime;
}

    void RotateToMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
        {
            Vector3 direction = hit.point - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }
}
