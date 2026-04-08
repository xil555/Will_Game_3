using UnityEngine;

public class PlayerStats : MonoBehaviour
{   
    [SerializeField] public float health = 100f;
    [SerializeField] public int keys = 0;
    [SerializeField] public int battery = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("PlayerStats initialized."); 

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
}
