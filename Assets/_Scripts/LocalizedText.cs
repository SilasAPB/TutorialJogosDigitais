using TMPro;
using UnityEngine;

// Troca o texto do TMP conforme o idioma escolhido em GameSettings.
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [TextArea] public string portuguese;
    [TextArea] public string english;

    TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        GameSettings.LanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        GameSettings.LanguageChanged -= Refresh;
    }

    void Refresh()
    {
        label.text = GameSettings.Language == GameLanguage.English ? english : portuguese;
    }
}
