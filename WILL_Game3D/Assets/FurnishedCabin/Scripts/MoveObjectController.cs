using System.Collections;
using UnityEngine;

public class MoveObjectController : MonoBehaviour
{
    public float reachRange = 1.8f;

    private Animator anim;
    private Camera fpsCam;
    private GameObject player;

    private const string animBoolName = "isOpen_Obj_";

    private bool playerEntered;
    private bool showInteractMsg;
    private GUIStyle guiStyle;
    private GUIStyle dotStyle;
    private string msg;

    private int rayLayerMask;

    public static int NearInteractableCount;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        fpsCam = Camera.main;
        if (fpsCam == null)
            Debug.LogError("A camera tagged 'MainCamera' is missing.");

        anim = GetComponent<Animator>();
        if (anim != null)
            anim.enabled = false;

        int layer = LayerMask.NameToLayer("InteractRaycast");
        if (layer >= 0)
            rayLayerMask = 1 << layer;
        else
            rayLayerMask = Physics.DefaultRaycastLayers;

        setupGui();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        if (!playerEntered)
            NearInteractableCount++;

        playerEntered = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        if (playerEntered && NearInteractableCount > 0)
            NearInteractableCount--;

        playerEntered = false;
        showInteractMsg = false;
    }

    void OnDisable()
    {
        if (playerEntered && NearInteractableCount > 0)
            NearInteractableCount--;

        playerEntered = false;
    }

    void Update()
    {
        if (!playerEntered || fpsCam == null || anim == null)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Ray ray = fpsCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, reachRange, rayLayerMask, QueryTriggerInteraction.Ignore))
        {
            MoveableObject moveableObject = null;
            if (!isEqualToParent(hit.collider, out moveableObject))
            {
                showInteractMsg = false;
                return;
            }

            if (moveableObject != null)
            {
                showInteractMsg = true;
                string animBoolNameNum = animBoolName + moveableObject.objectNumber.ToString();

                bool isOpen = anim.GetBool(animBoolNameNum);
                msg = getGuiMsg(isOpen);

                if (Input.GetMouseButtonDown(0) || Input.GetButtonDown("Fire1"))
                {
                    anim.enabled = true;
                    anim.SetBool(animBoolNameNum, !isOpen);
                    msg = getGuiMsg(!isOpen);
                }
            }
        }
        else
        {
            showInteractMsg = false;
        }
    }

    bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        if (player == null)
            return false;

        return other.gameObject == player || other.transform.IsChildOf(player.transform);
    }

    private bool isEqualToParent(Collider other, out MoveableObject draw)
    {
        draw = null;
        bool rtnVal = false;
        try
        {
            int maxWalk = 6;
            draw = other.GetComponent<MoveableObject>();

            GameObject currentGO = other.gameObject;
            for (int i = 0; i < maxWalk; i++)
            {
                if (currentGO.Equals(this.gameObject))
                {
                    rtnVal = true;
                    if (draw == null)
                        draw = currentGO.GetComponentInParent<MoveableObject>();
                    break;
                }

                if (currentGO.transform.parent != null)
                    currentGO = currentGO.transform.parent.gameObject;
            }
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }

        return rtnVal;
    }

    #region GUI Config

    private void setupGui()
    {
        guiStyle = new GUIStyle();
        guiStyle.fontSize = 16;
        guiStyle.fontStyle = FontStyle.Bold;
        guiStyle.normal.textColor = Color.white;
        msg = "Press LeftClick to Open";

        dotStyle = new GUIStyle();
        dotStyle.alignment = TextAnchor.MiddleCenter;
        dotStyle.fontSize = 18;
        dotStyle.fontStyle = FontStyle.Bold;
        dotStyle.normal.textColor = Color.white;
    }

    private string getGuiMsg(bool isOpen)
    {
        if (isOpen)
            return "Press Left Click to Close";

        return "Press Left Click to Open";
    }

    void OnGUI()
    {
        if (showInteractMsg)
            GUI.Label(new Rect(50, Screen.height - 50, 240, 50), msg, guiStyle);

        if (playerEntered)
        {
            float x = Input.mousePosition.x;
            float y = Screen.height - Input.mousePosition.y;
            dotStyle.normal.textColor = showInteractMsg ? Color.green : Color.white;
            GUI.Label(new Rect(x - 10f, y - 10f, 20f, 20f), "+", dotStyle);
        }
    }

    #endregion
}
