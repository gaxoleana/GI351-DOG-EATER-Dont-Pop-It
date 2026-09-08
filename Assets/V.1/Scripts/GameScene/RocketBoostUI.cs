using TMPro;
using UnityEngine;

public class RocketBoostUI : MonoBehaviour
{
    public PlayerController player;
    public GameObject container;
    public TextMeshProUGUI boostText;

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        SetVisible(false);
    }

    private void Update()
    {
        if (player == null || boostText == null) return;

        bool isBoosting = player.RocketBoostTimeRemaining > 0f;
        SetVisible(isBoosting);

        if (isBoosting && boostText != null)
        {
            boostText.text = $"Rocket Boost: {player.RocketBoostTimeRemaining:0.0}s";
        }
    }

    private void SetVisible(bool visible)
    {
        if (container != null)
        {
            if (container.activeSelf != visible) container.SetActive(visible);
        }
        else if (boostText != null)
        {
            boostText.gameObject.SetActive(visible);
        }
    }
}
