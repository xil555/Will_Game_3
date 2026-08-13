using UnityEngine;

public class Flashlight : MonoBehaviour
{
    public GameObject ON;
    public GameObject OFF;
    bool isON;

    public bool IsOn => isON;

    void Awake()
    {
        FixInvalidColliders();
    }

    private void Start()
    {
        FixInvalidColliders();

        if (ON != null) ON.SetActive(false);
        if (OFF != null) OFF.SetActive(true);
        isON = false;
    }

    public void Update()
    {
        if (PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isON)
            {
                if (ON != null) ON.SetActive(false);
                if (OFF != null) OFF.SetActive(true);
            }
            else
            {
                if (ON != null) ON.SetActive(true);
                if (OFF != null) OFF.SetActive(false);
            }

            isON = !isON;
        }
    }

    void FixInvalidColliders()
    {
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>(true);
        for (int i = 0; i < meshColliders.Length; i++)
        {
            MeshCollider meshCol = meshColliders[i];
            Rigidbody rb = meshCol.GetComponent<Rigidbody>();
            if (rb == null)
                rb = meshCol.GetComponentInParent<Rigidbody>();

            if (rb != null && !rb.isKinematic && !meshCol.convex)
            {
                meshCol.enabled = false;
                Destroy(meshCol);
            }
        }
    }
}