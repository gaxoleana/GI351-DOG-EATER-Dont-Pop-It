using UnityEngine;

public class RocketPickup : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float rotationSpeedY = 180f;
    public float boostHeight = 100f;
    public float boostDuration = 2.5f;
    public float lifeTime = 8f;

    private Vector2 moveDirection;

    public void Initialize(Vector2 direction)
    {
        moveDirection = direction.normalized;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Rotate(0f, rotationSpeedY * Time.deltaTime, 0f, Space.Self);
        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        player.StartRocketBoost(boostHeight, boostDuration);
        Destroy(gameObject);
    }
}
