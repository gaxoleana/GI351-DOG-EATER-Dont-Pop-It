using UnityEngine;

/// <summary>
/// ควบคุมการเคลื่อนที่และการชนของ Obstacle
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 8f;
    public Vector2 moveDirection = Vector2.right;
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
        // ถ้าชน GumController ให้บังคับหมากฝรั่งแตกทันที
        GumController gum = other.GetComponent<GumController>();
        if (gum == null) gum = other.GetComponentInParent<GumController>();

        if (gum != null && gum.currentState == GumController.GumState.Normal)
        {
            gum.ForcePop();
            Destroy(gameObject);
        }
    }
}