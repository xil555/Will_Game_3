using UnityEngine;

public class Flashlight : MonoBehaviour
{
    public GameObject ON;
    public GameObject OFF;
    bool isON;

    private void Start()
    {
        ON.SetActive(false);
        OFF.SetActive(true);
        isON = false;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isON)
            {
                ON.SetActive(false);
                OFF.SetActive(true);
            }
            if (!isON)
            {
                ON.SetActive(true);
                OFF.SetActive(false);
            }

            isON = !isON;
        }
    }
}