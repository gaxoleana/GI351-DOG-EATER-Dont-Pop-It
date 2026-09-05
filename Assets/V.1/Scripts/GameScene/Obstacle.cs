using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float speed = 12f;
    public Vector2 moveDirection = Vector2.left;
    public float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GumController gum = other.GetComponent<GumController>();
        PlayerController player = other.GetComponent<PlayerController>();

        // Case A: ชนโดนตัวหมากฝรั่งโดยตรง (Hit Gum) -> หมากฝรั่งแตก แต่ฟื้นตัวเร็ว!
        if (gum != null && gum.currentState == GumController.GumState.Normal)
        {
            Debug.Log("💥 Hit Gum -> Fast Recovery!");
            gum.ForcePop(gum.fastStunDuration); // ฟื้นตัวเร็ว (1.5 วิ)
            Destroy(gameObject);
            return;
        }

        // Case B: ชนโดนตัวผู้เล่นโดยตรง (Hit Player) -> หมากฝรั่งแตก ฟื้นตัวช้า!
        if (player != null)
        {
            Debug.Log("💥 Hit Player Body -> Normal (Slow) Recovery!");
            GumController playerGum = player.GetComponentInChildren<GumController>();
            if (playerGum != null && playerGum.currentState == GumController.GumState.Normal)
            {
                playerGum.ForcePop(playerGum.normalStunDuration); // ฟื้นตัวปกติ (2.5 วิ)
            }
            Destroy(gameObject);
            return;
        }
    }
}