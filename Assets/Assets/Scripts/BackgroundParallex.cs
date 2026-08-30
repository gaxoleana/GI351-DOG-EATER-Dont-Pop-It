using UnityEngine;

public class BackgroundParallax : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private SpriteRenderer topCloud;
    [SerializeField] private SpriteRenderer bottomCloud;
    [Range(0f, 1f)][SerializeField] private float parallaxFactor = 0.2f;

    private float spriteHeight;
    private float previousCameraY;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            previousCameraY = cameraTransform.position.y;

        if (topCloud != null)
        {
            spriteHeight = topCloud.bounds.size.y;

            // Automatically position bottom cloud directly beneath top cloud
            if (bottomCloud != null)
            {
                bottomCloud.transform.localPosition = new Vector3(
                    topCloud.transform.localPosition.x,
                    topCloud.transform.localPosition.y - spriteHeight,
                    topCloud.transform.localPosition.z
                );
            }
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || topCloud == null || bottomCloud == null) return;

        // Apply Parallax translation to the Parent container
        float deltaY = cameraTransform.position.y - previousCameraY;
        transform.position += new Vector3(0f, deltaY * (1f - parallaxFactor), 0f);
        previousCameraY = cameraTransform.position.y;

        // Leapfrog top sprite down when camera falls past it
        if (cameraTransform.position.y < topCloud.transform.position.y - spriteHeight)
        {
            topCloud.transform.position = new Vector3(
                topCloud.transform.position.x,
                bottomCloud.transform.position.y - spriteHeight,
                topCloud.transform.position.z
            );

            // Swap references so they continuously take turns falling
            SpriteRenderer temp = topCloud;
            topCloud = bottomCloud;
            bottomCloud = temp;
        }
    }
}