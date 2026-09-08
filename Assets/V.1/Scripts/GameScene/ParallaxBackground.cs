using UnityEngine;

/// <summary>
/// Moves a background sprite at a fraction of the player's movement.
/// This component is independent from GroundToSpaceBackground.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player used as the parallax reference. If empty, PlayerController is found automatically.")]
    public Transform playerTransform;

    [Header("Parallax")]
    [Range(0f, 1f)]
    [Tooltip("0 = background stays still, 1 = background follows the player fully.")]
    public float parallaxX;

    [Range(0f, 1f)]
    [Tooltip("0 = background stays still, 1 = background follows the player fully.")]
    public float parallaxY = 0.35f;

    [Tooltip("Keeps the original background position as the parallax origin.")]
    public bool preserveInitialPosition = true;

    [Header("Altitude Visibility")]
    [Tooltip("Enable altitude-based visibility for this background.")]
    public bool limitByAltitude;

    [Tooltip("Altitude at which this background starts being visible, in world units.")]
    public float visibleFromAltitude;

    [Tooltip("Altitude at which this background stops being visible, in world units.")]
    public float visibleUntilAltitude = 500f;

    private Vector3 initialPosition;
    private Vector3 initialPlayerPosition;
    private bool hasInitialized;

    private void Awake()
    {
        initialPosition = transform.position;
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("[ParallaxBackground] Player Transform is not assigned.", this);
            return;
        }

        initialPlayerPosition = playerTransform.position;
        hasInitialized = true;
    }

    private void LateUpdate()
    {
        if (!hasInitialized || playerTransform == null) return;

        Vector3 playerDelta = playerTransform.position - initialPlayerPosition;
        Vector3 targetPosition = initialPosition;
        targetPosition.x += playerDelta.x * parallaxX;
        targetPosition.y += playerDelta.y * parallaxY;
        transform.position = targetPosition;

        if (limitByAltitude)
        {
            float altitude = Mathf.Max(0f, playerTransform.position.y - initialPlayerPosition.y);
            bool shouldBeVisible = altitude >= visibleFromAltitude && altitude < visibleUntilAltitude;
            if (gameObject.activeSelf != shouldBeVisible)
            {
                gameObject.SetActive(shouldBeVisible);
            }
        }
    }
}