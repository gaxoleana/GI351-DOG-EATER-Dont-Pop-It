using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine;

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
        // Finish the current event before taking exclusive control of the player.
        foreach (var panic in FindObjectsByType<PanicEventManagerVariantNoStop>(FindObjectsSortMode.None))
        {
            if (panic.enabled) panic.SendMessage("EndEvent", false);
            panic.StopAllCoroutines(); panic.enabled = false;
        }
        foreach (var panic in FindObjectsByType<PanicEventManagerVariantTimeStop>(FindObjectsSortMode.None))
        {
            if (panic.enabled) panic.SendMessage("EndEvent", false);
            panic.StopAllCoroutines(); panic.enabled = false;
        }
        foreach (var spawner in FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None))
        { spawner.StopAllCoroutines(); spawner.enabled = false; }
        foreach (var obstacle in FindObjectsByType<Obstacle>(FindObjectsSortMode.None)) Destroy(obstacle.gameObject);
        foreach (var rocket in FindObjectsByType<RocketPickup>(FindObjectsSortMode.None)) Destroy(rocket.gameObject);
        foreach (var ui in FindObjectsByType<RocketBoostUI>(FindObjectsSortMode.None))
        { ui.enabled = false; if (ui.container != null) ui.container.SetActive(false); else if (ui.boostText != null) ui.boostText.gameObject.SetActive(false); }
        foreach (var ui in FindObjectsByType<GumRecoveryUI>(FindObjectsSortMode.None))
        { ui.enabled = false; if (ui.container != null) ui.container.SetActive(false); else if (ui.recoveryText != null) ui.recoveryText.gameObject.SetActive(false); }
        if (player == null) yield break;
        player.StopAllCoroutines();
        player.SetBlowInput(false); player.SetInputLocked(true); player.enabled = false;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.linearVelocity = Vector2.zero; body.simulated = false; }
        Vector3 start = player.transform.position;
        start.y = (altitudeUI != null ? altitudeUI.maxAltitude + altitudeUI.startYOffset : 3000f);
        player.transform.position = start;
        var groundAnimation = player.GetComponent<PlayerGroundAnimator>();
        if (groundAnimation != null) groundAnimation.enabled = false;
        var animator = player.GetComponent<Animator>(); if (animator != null) animator.speed = 1f;
        if (player.gum != null)
        {
            player.gum.enabled = false;
            if (player.gum.ringRenderer != null) player.gum.ringRenderer.enabled = false;
        }
        foreach (var placeholder in player.GetComponentsInChildren<DeadZonePlaceholder>()) placeholder.enabled = false;
        var camera = Camera.main;
        var brain = camera != null ? camera.GetComponent<CinemachineBrain>() : null;
        if (brain != null) brain.enabled = false;
        var bosses = FindObjectsByType<AlienBoss>(FindObjectsSortMode.None);
        var bossStarts = new Vector3[bosses.Length];
        for (int i = 0; i < bosses.Length; i++)
        { bossStarts[i] = bosses[i].transform.position; bosses[i].BeginEndingExit(); }
        float elapsed = 0;
        const float escapeDuration = .18f;
        while (elapsed < escapeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / escapeDuration);
            for (int i = 0; i < bosses.Length; i++) if (bosses[i] != null)
                bosses[i].transform.position = bossStarts[i] + new Vector3(45f, 12f, 0) * t * t;
            yield return null;
        }
        foreach (var boss in bosses) if (boss != null) Destroy(boss.gameObject);
        float distance = camera != null && camera.orthographic ? camera.orthographicSize * 3f : 30f;
        Vector3 target = start + new Vector3(1f, distance, 0);
        elapsed = 0;
        const float flightDuration = 2.6f;
        while (elapsed < flightDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);
            player.transform.position = Vector3.LerpUnclamped(start, target, t * t);
            FlightRewardPresentation.SetEndingFade(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, 1f, t)));
            yield return null;
        }
        player.transform.position = target;
        Time.timeScale = 0f;
        if (winPanel != null) winPanel.SetActive(true);
        if (totalTimeText != null)
        {
            int minutes = Mathf.FloorToInt(timer / 60f), seconds = Mathf.FloorToInt(timer % 60f);
            totalTimeText.text = $"Total time : {minutes:00}:{seconds:00}";
        }
        if (inGameTimeText != null) inGameTimeText.gameObject.SetActive(false);
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