using UnityEngine;
using System.Collections;

public class LoadScene : MonoBehaviour
{
  
    public string levelToLoad;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
   
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            
            Application.LoadLevel(levelToLoad);
        }
    }
   

    // Update is called once per frame
    void Update()
    {
        
    }
}
