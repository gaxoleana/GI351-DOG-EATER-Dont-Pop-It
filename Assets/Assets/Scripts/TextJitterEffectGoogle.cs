using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class OmoriTextJitter : MonoBehaviour
{
    private TMP_Text textComponent;
    private bool hasTextChanged;

    [Header("Jitter Settings")]
    [Range(0f, 10f)] public float shakeStrength = 2.0f;
    [Range(0.01f, 0.2f)] public float changeInterval = 0.05f;

    private float timer;
    private Vector3[] randomOffsets;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
    }

    void ON_TEXT_CHANGED(Object obj)
    {
        if (obj == textComponent)
            hasTextChanged = true;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (hasTextChanged)
        {
            textComponent.ForceMeshUpdate();
            hasTextChanged = false;
        }

        TMP_TextInfo textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0) return;

        if (randomOffsets == null || randomOffsets.Length < characterCount)
        {
            randomOffsets = new Vector3[characterCount * 4];
        }

        if (timer >= changeInterval)
        {
            for (int i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized * shakeStrength;

                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                randomOffsets[vertexIndex + 0] = randomOffset;
                randomOffsets[vertexIndex + 1] = randomOffset;
                randomOffsets[vertexIndex + 2] = randomOffset;
                randomOffsets[vertexIndex + 3] = randomOffset;
            }
            timer = 0;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            System.Array.Copy(meshInfo.vertices, meshInfo.colors32, 0);
        }

        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

            sourceVertices[vertexIndex + 0] += randomOffsets[vertexIndex + 0];
            sourceVertices[vertexIndex + 1] += randomOffsets[vertexIndex + 1];
            sourceVertices[vertexIndex + 2] += randomOffsets[vertexIndex + 2];
            sourceVertices[vertexIndex + 3] += randomOffsets[vertexIndex + 3];
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
