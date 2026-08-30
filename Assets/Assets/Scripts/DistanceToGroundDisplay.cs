using UnityEngine;
using TMPro;

public class DistanceToGroundDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform groundPoint;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Distance Settings")]
    [SerializeField] private float startDistanceMeters = 4500f;
    [SerializeField] private string suffix = " m";
    [SerializeField] private int decimalPlaces = 0;

    private float groundY;
    private float initialYDistance;

    private void Start()
    {
        if (player == null || groundPoint == null)
        {
            Debug.LogWarning("DistanceToGroundDisplay: Missing Player or Ground Point reference.");
            enabled = false;
            return;
        }

        groundY = groundPoint.position.y;

        // Calculate the initial height gap between player and ground
        initialYDistance = Mathf.Max(0.0001f, player.position.y - groundY);
    }

    private void Update()
    {
        if (distanceText == null) return;

        // Remaining Y distance above ground
        float currentYDistance = Mathf.Max(0f, player.position.y - groundY);

        // Convert current Y distance so it starts at 4500m and scales down to 0m at groundY
        float remainingMeters = (currentYDistance / initialYDistance) * startDistanceMeters;

        distanceText.text = remainingMeters.ToString("F" + decimalPlaces) + suffix;
    }
}