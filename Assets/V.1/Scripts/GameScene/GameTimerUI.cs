using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameTimerUI : MonoBehaviour
{
    [Header("References")]
    public AltitudeProgressBarUI altitudeUI;
    public GameObject winPanel;
    public TextMeshProUGUI totalTimeText;

    [Header("In-Game UI")]
    [Tooltip("ลาก UI Text ที่จะโชว์เวลาตอนเล่น (มุมขวาบน) มาใส่ตรงนี้")]
    public TextMeshProUGUI inGameTimeText; // 🔹 เพิ่มตัวแปรสำหรับเวลาหน้าจอหลัก

    [Header("Menu Settings")]
    [Tooltip("ใส่ชื่อ Scene ของหน้าเมนูหลักให้ตรงกันเป๊ะๆ (เช่น MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

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
        SceneManager.LoadScene("MainMenuV.1"); // โหลด Scene เมนู (อิงชื่อจาก PauseMenu ของคุณ)
    }
}