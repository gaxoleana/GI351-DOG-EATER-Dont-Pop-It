using UnityEngine;
using TMPro;

/// <summary>
/// แสดงเวลานับถอยหลังก่อนหมากฝรั่งเป่าได้ใหม่ ตอน currentState == Popped
/// </summary>
public class GumRecoveryUI : MonoBehaviour
{
    [Header("References")]
    public GumController gum;
    public GameObject container;
    public TextMeshProUGUI recoveryText;

    void Start()
    {
        if (gum == null) gum = FindAnyObjectByType<GumController>();
        SetVisible(false);
    }

    void Update()
    {
        if (gum == null) return;

        bool isRecovering = gum.currentState == GumController.GumState.Popped;
        SetVisible(isRecovering);

        if (isRecovering && recoveryText != null)
        {
            recoveryText.text = $"{gum.RecoveryTimeRemaining:0.0}s";
        }
    }

    private void SetVisible(bool visible)
    {
        if (container != null)
        {
            if (container.activeSelf != visible) container.SetActive(visible);
        }
        else if (recoveryText != null)
        {
            recoveryText.gameObject.SetActive(visible);
        }
    }
}
