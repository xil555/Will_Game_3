using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public int batteryAmount = 30;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Picked up battery! +" + batteryAmount);

            // Later:
            // other.GetComponent<PlayerBattery>().AddBattery(batteryAmount);

            Destroy(gameObject); // remove battery after pickup
        }
    }
}
