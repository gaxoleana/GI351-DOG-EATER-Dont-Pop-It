using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameTimerUI : MonoBehaviour
{
    [Header("References")]
    public AltitudeProgressBarUI altitudeUI;
    public PlayerController player;
    public GameObject winPanel;
    public TextMeshProUGUI totalTimeText;

    [Header("In-Game UI")]
    [Tooltip("ลาก UI Text ที่จะโชว์เวลาตอนเล่น (มุมขวาบน) มาใส่ตรงนี้")]
    public TextMeshProUGUI inGameTimeText; // 🔹 เพิ่มตัวแปรสำหรับเวลาหน้าจอหลัก

    [Header("Menu Settings")]
    [Tooltip("ใส่ชื่อ Scene ของหน้าเมนูหลักให้ตรงกันเป๊ะๆ (เช่น MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Win Slowdown")]
    [Tooltip("เวลาจริงที่ให้ Player ลอยต่อหลังถึงเส้นชัย")]
    public float winFloatDuration = 0.75f;
    [Tooltip("เวลาจริงที่ใช้ลด Time.timeScale ลงจนหยุด")]
    public float winSlowdownDuration = 0.75f;

    private float timer = 0f;
    private bool isFinished = false;

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
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

            // 🔹 อัปเดตเวลาบนหน้าจอตอนเล่นเกมทุกๆ เฟรม
            if (inGameTimeText != null)
            {
                int minutes = Mathf.FloorToInt(timer / 60f);
                int seconds = Mathf.FloorToInt(timer % 60f);
                inGameTimeText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }
    }

    private void HandleGameWin()
    {
        if (isFinished) return;
        isFinished = true;

        if (player != null)
        {
            player.SetInputLocked(true);
        }

        StartCoroutine(FinishGameRoutine());
    }

    private IEnumerator FinishGameRoutine()
    {
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(winFloatDuration);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, winSlowdownDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        Time.timeScale = 0f;

        if (winPanel != null) winPanel.SetActive(true);

        if (totalTimeText != null)
        {
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            totalTimeText.text = $"Total time : {minutes:00}:{seconds:00}";
        }

        // 🔹 ซ่อนเวลาที่มุมจอตอนจบเกม เพื่อไม่ให้รกจอตอนหน้าต่าง Win เด้ง
        if (inGameTimeText != null)
        {
            inGameTimeText.gameObject.SetActive(false);
        }
    }

    /// <summary>เรียกเมื่อกดปุ่ม "เล่นต่อ"</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f; // คืนค่าเวลาเกมให้กลับมาเดินปกติ
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // โหลด Scene เดิมซ้ำ
    }

    /// <summary>เรียกเมื่อกดปุ่ม "กลับเมนู"</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // คืนค่าเวลาเกมให้กลับมาเดินปกติ
        SceneManager.LoadScene(mainMenuSceneName); // โหลด Scene เมนู (อิงชื่อจาก PauseMenu ของคุณ)
    }
}