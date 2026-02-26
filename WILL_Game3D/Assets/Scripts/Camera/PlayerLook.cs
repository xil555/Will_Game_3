using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public float rotationSpeed = 8f;
    public LayerMask groundLayer;

    private Camera cam;
    private Rigidbody rb;

    private Quaternion targetRotation;
    private bool isRotating = false;

    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody>();
        targetRotation = rb.rotation;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            SetTargetRotation();
        }
    }

    void SetTargetRotation()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 direction = hit.point - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                targetRotation = Quaternion.LookRotation(direction);
                isRotating = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (!isRotating) return;

        Quaternion newRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(newRotation);

        if (Quaternion.Angle(rb.rotation, targetRotation) < 1f)
        {
            rb.MoveRotation(targetRotation);
            isRotating = false;
        }
    }
}
