using System.Collections;
using UnityEngine;

public class TextJitterEffect : MonoBehaviour
{
    [SerializeField] private float positionJitter = 3f;
    [SerializeField] private float scaleJitter = 0.03f;
    [SerializeField] private float fps = 12f;

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        StartCoroutine(JitterRoutine());
    }

    private IEnumerator JitterRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1f / fps);

        while (true)
        {
            Vector2 randomOffset = Random.insideUnitCircle * positionJitter;
            rectTransform.anchoredPosition = originalPosition + (Vector3)randomOffset;

            float randomScaleX = 1f + Random.Range(-scaleJitter, scaleJitter);
            float randomScaleY = 1f + Random.Range(-scaleJitter, scaleJitter);
            rectTransform.localScale = new Vector3(originalScale.x * randomScaleX, originalScale.y * randomScaleY, 1f);

            yield return wait;
        }
    }
}