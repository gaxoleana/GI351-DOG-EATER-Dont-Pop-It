using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("ถ้าไม่ผูกไว้ จะหาอัตโนมัติตอน Start")]
    [SerializeField] private PlayerController player;

    private bool isPaused = false;

    void Start()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    private void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;

        if (player != null)
        {
            player.SetInputLocked(true);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;

        if (player != null)
        {
            player.SetInputLocked(false);
        }
    }

    public void BackToMainMenu()
    {
        // สำคัญมาก: ต้องคืนค่าทั้งคู่ก่อนเปลี่ยนซีน ไม่งั้นซีนใหม่จะค้าง (timeScale=0)
        // หรือ player ใน MainMenu (ถ้ามี logic ที่ต้องอ่าน input) จะโดน lock ค้างข้ามซีนไปด้วย
        Time.timeScale = 1f;

        if (player != null)
        {
            player.SetInputLocked(false);
        }

        SceneManager.LoadScene("MainMenuV.1");
    }
}