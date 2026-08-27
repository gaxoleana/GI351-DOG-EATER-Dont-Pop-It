using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Score Settings")]
    [SerializeField] private float scorePerSecond = 1f;
    [SerializeField] private float scoreMultiplier = 1f;

    private float currentScore = 0f;
    private int highScore = 0;
    private bool isPlayerAlive = true;

    private void Start()
    {
        highScore = PlayerPrefs.GetInt("Highest", 0);
        UpdateHighScoreUI();
    }

    private void Update()
    {
        if (!isPlayerAlive) return;

        currentScore += scorePerSecond * scoreMultiplier * Time.deltaTime;

        if (scoreText != null)
        {
            scoreText.text = $"{Mathf.FloorToInt(currentScore)} Score";
        }
    }

    public int OnPlayerDied()
    {
        isPlayerAlive = false;

        int finalScore = Mathf.FloorToInt(currentScore);
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt("Highest", highScore);
            PlayerPrefs.Save();
            UpdateHighScoreUI();
        }

        return finalScore;
    }

    public void SetMultiplier(float newMultiplier)
    {
        scoreMultiplier = newMultiplier;
    }

    private void UpdateHighScoreUI()
    {
        if (highScoreText != null)
        {
            highScoreText.text = $"Highest\n{highScore} Score";
        }
    }
}