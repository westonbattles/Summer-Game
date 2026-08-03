using UnityEngine;
using TMPro;

public class InteractionPromptHUD : MonoBehaviour
{
    [SerializeField] GameObject promptRoot;
    [SerializeField] TMP_Text promptText;

    public static InteractionPromptHUD Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        promptRoot.SetActive(false);
    }

    public void Show(string text)
    {
        promptText.text = text;
        promptRoot.SetActive(true);
    }

    public void Hide() => promptRoot.SetActive(false);
}
