using UnityEngine;

public class PlayerGroundAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Ground Check")]
    [Tooltip("ลาก GroundCheck (child object ที่วางไว้ตรงเท้า) มาใส่")]
    [SerializeField] private Transform groundCheckPoint;

    [Tooltip("รัศมีวงกลมเช็คพื้น")]
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Tooltip("เลือก Layer ที่นับเป็นพื้น")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Gum Reference")]
    [SerializeField] private GumController gum;

    [Header("Airborne Animation")]
    [Range(0.1f, 1f)] public float airborneAnimationSpeed = 0.6f;
    private bool grounded;
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    void Start()
    {
        if (gum == null) gum = FindAnyObjectByType<GumController>();
    }

    void Update()
    {
        bool isGrounded = groundCheckPoint != null && Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
        grounded = isGrounded;
        if (animator != null) animator.SetBool(IsGroundedHash, isGrounded);

        if (gum != null)
        {
            gum.SetGroundedHidden(isGrounded);
        }
    }

    void LateUpdate()
    {
        if (animator != null)
            animator.speed = Mathf.MoveTowards(animator.speed, grounded ? 1f : airborneAnimationSpeed, Time.deltaTime * 3f);
    }

    // เผื่อ debug ดูรัศมีเช็คพื้นใน Scene view
    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}