using UnityEngine;
using TMPro;

public class GameTimerUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก UI หลอดความสูงมาใส่ตรงนี้")]
    public AltitudeProgressBarUI altitudeUI;

    [Tooltip("ลาก UI Panel 'You Win!' มาใส่ตรงนี้")]
    public GameObject winPanel;

    [Tooltip("ลาก UI Text ที่จะให้แสดงเวลามาใส่ตรงนี้")]
    public TextMeshProUGUI totalTimeText;

    private float timer = 0f;
    private bool isFinished = false;

    void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);

        if (altitudeUI != null)
        {
            altitudeUI.OnReachFinishLine += HandleGameWin;
        }
    }

    void OnDestroy()
    {
        if (altitudeUI != null)
        {
            altitudeUI.OnReachFinishLine -= HandleGameWin;
        }
    }

    void Update()
    {
        if (!isFinished)
        {
            timer += Time.deltaTime;
        }
    }

    private void HandleGameWin()
    {
        if (isFinished) return;
        isFinished = true;

        Time.timeScale = 0f;

        if (winPanel != null) winPanel.SetActive(true);

        if (totalTimeText != null)
        {
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            totalTimeText.text = $"Total time : {minutes:00}:{seconds:00}";
        }
    }
}