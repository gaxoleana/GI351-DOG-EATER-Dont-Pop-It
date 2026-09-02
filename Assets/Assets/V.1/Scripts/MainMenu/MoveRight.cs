using UnityEngine;

public class MoveRight : MonoBehaviour
{
    private float speed;
    private float despawnX;

    public void Init(float moveSpeed, float xLimit)
    {
        speed = moveSpeed;
        despawnX = xLimit;
    }

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);

        if (transform.position.x >= despawnX)
        {
            Destroy(gameObject);
        }
    }
}