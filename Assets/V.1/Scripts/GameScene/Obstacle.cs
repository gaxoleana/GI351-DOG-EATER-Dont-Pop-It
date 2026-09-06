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
        // 1. ถ้าชนโดนหมากฝรั่ง (ต้องตั้ง Tag ชิ้นส่วนหมากฝรั่งเป็น "Gum")
        if (other.CompareTag("Gum"))
        {
            GumController gum = other.GetComponentInParent<GumController>();
            if (gum != null && gum.currentState == GumController.GumState.Normal)
            {
                Debug.Log("💥 โดนหมากฝรั่ง -> แตกแบบฟื้นตัวเร็ว!");
                gum.ForcePop(gum.fastStunDuration);
            }
            Destroy(gameObject);
        }
        // 2. ถ้าชนโดนผู้เล่น (ต้องตั้ง Tag ชิ้นส่วนคนเป็น "Player")
        else if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                Debug.Log("😵 โดนผู้เล่น -> มึน!");
                player.OnHitByObstacleBody(); // เรียกฟังก์ชันมึนที่คุณเตรียมไว้
            }
            Destroy(gameObject);
        }
    }
}