using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SecretsManager : MonoBehaviour
{
    public static SecretsManager Instance { get; private set; }

    [SerializeField] Canvas canvas;
    
    void Awake()
    {
        Instance = this;
    }

    public void AwardSecret()
    {
        ShowDebugMessage("You found a secret!");
    }
    
    
    
    void ShowDebugMessage(string message, float duration = 1.5f)
    {
        // create a tmpro object
        GameObject textObject = new GameObject("Secret Message");
        textObject.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = message;
        text.fontSize = 36;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        // position
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(1000, 100);
        rect.anchoredPosition = new Vector2(0, -70); // a bit down from top middle of screen

        // fade and destroy
        StartCoroutine(FadeOut(text, duration));
    }

    IEnumerator FadeOut(TextMeshProUGUI text, float duration)
    {
        yield return new WaitForSeconds(duration);

        float fadeTime = 0.75f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            Color color = text.color;
            color.a = 1f - Mathf.Clamp01(elapsed / fadeTime);
            text.color = color;

            yield return null;
        }

        Destroy(text.gameObject);
    }
}
