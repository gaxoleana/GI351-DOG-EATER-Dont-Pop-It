using UnityEngine;

public class BirdCollectTrigger : MonoBehaviour
{
    public GameObject Off;   
    public GameObject On;    

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Off.SetActive(false);
        On.SetActive(true);

        gameObject.SetActive(false); 
    }
}