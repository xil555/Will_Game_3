using TMPro;
using UnityEngine;

/// <summary>
/// Floating pickup cue. Add this to a child arrow, or let EnergyDrink / BatteryPickup
/// spawn one at runtime. Destroyed with the item when it is picked up.
/// </summary>
public class PickupIndicator : MonoBehaviour
{
    [Header("Motion")]
    public float bobSpeed = 2.4f;
    public float bobHeight = 0.18f;
    public float rotationSpeed = 40f;

    [Header("Proximity")]
    [Tooltip("Arrow is hidden until the player is this close (world units).")]
    public float detectionRadius = 40f;
    public bool faceCamera = true;

    [Header("Visual")]
    [Tooltip("If empty, a default down-arrow is created.")]
    public GameObject arrowVisual;
    public Color arrowColor = new Color(1f, 0.86f, 0.2f, 0.95f);

    Transform player;
    Camera cam;
    Vector3 restLocalPos;
    float playerRefreshTimer;
    Renderer[] renderers;
    TextMeshPro arrowText;
    bool visible = true;

    public static PickupIndicator Ensure(Transform host, Vector3 offset, Color color, GameObject arrowPrefab = null, float detectionRadius = 40f)
    {
        if (host == null)
            return null;

        PickupIndicator existing = host.GetComponentInChildren<PickupIndicator>(true);
        if (existing != null)
        {
            existing.arrowColor = color;
            existing.detectionRadius = detectionRadius;
            existing.ApplyColor();
            return existing;
        }

        GameObject root;
        if (arrowPrefab != null)
        {
            root = Instantiate(arrowPrefab, host);
            root.name = "PickupIndicator";
        }
        else
        {
            root = new GameObject("PickupIndicator");
            root.transform.SetParent(host, false);
        }

        Vector3 lossy = host.lossyScale;
        if (Mathf.Abs(lossy.x) > 0.0001f && Mathf.Abs(lossy.y) > 0.0001f && Mathf.Abs(lossy.z) > 0.0001f)
            root.transform.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);

        root.transform.position = host.position + offset;
        root.transform.rotation = Quaternion.identity;

        PickupIndicator indicator = root.GetComponent<PickupIndicator>();
        if (indicator == null)
            indicator = root.AddComponent<PickupIndicator>();

        indicator.arrowColor = color;
        indicator.detectionRadius = detectionRadius;
        indicator.CaptureRestPose();
        indicator.ApplyColor();
        return indicator;
    }

    public void CaptureRestPose()
    {
        restLocalPos = transform.localPosition;
    }

    public void ApplyColor()
    {
        if (arrowText != null)
            arrowText.color = arrowColor;
    }

    void Awake()
    {
        restLocalPos = transform.localPosition;
        ResolvePlayer();
        cam = Camera.main;

        if (arrowVisual == null)
            arrowVisual = CreateDefaultArrow();

        CacheVisuals();
        SetVisible(false);
    }

    void Update()
    {
        if (player == null)
        {
            playerRefreshTimer -= Time.deltaTime;
            if (playerRefreshTimer <= 0f)
            {
                playerRefreshTimer = 0.4f;
                ResolvePlayer();
            }
        }

        if (cam == null)
            cam = Camera.main;

        bool inRange = false;
        if (player != null)
            inRange = Vector3.Distance(player.position, transform.position) <= detectionRadius;

        SetVisible(inRange);
        if (!inRange)
            return;

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = restLocalPos + Vector3.up * bob;

        if (faceCamera && cam != null)
        {
            Vector3 look = transform.position - cam.transform.position;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(look);
        }
        else if (Mathf.Abs(rotationSpeed) > 0.01f)
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        }
    }

    public void Hide()
    {
        SetVisible(false);
        enabled = false;
    }

    void ResolvePlayer()
    {
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null)
            player = found.transform;
    }

    void CacheVisuals()
    {
        if (arrowVisual == null)
            return;

        renderers = arrowVisual.GetComponentsInChildren<Renderer>(true);
        arrowText = arrowVisual.GetComponentInChildren<TextMeshPro>(true);
        if (arrowText != null)
            arrowText.color = arrowColor;
    }

    void SetVisible(bool show)
    {
        if (visible == show)
            return;

        visible = show;

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = show;
            }
        }

        if (arrowText != null)
            arrowText.enabled = show;
    }

    GameObject CreateDefaultArrow()
    {
        GameObject go = new GameObject("Arrow");
        go.transform.SetParent(transform, false);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "▼";
        tmp.fontSize = 6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        tmp.sortingOrder = 20;
        tmp.color = arrowColor;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        tmp.rectTransform.sizeDelta = new Vector2(2f, 2f);
        return go;
    }
}
