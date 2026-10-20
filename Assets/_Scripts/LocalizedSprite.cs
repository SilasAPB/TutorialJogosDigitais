using UnityEngine;
using UnityEngine.UI;

// Troca o sprite do Image conforme o idioma escolhido em GameSettings.
[RequireComponent(typeof(Image))]
public class LocalizedSprite : MonoBehaviour
{
    public Sprite portuguese;
    public Sprite english;

    Image image;

    void Awake()
    {
        image = GetComponent<Image>();
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
        image.sprite = GameSettings.Language == GameLanguage.English ? english : portuguese;
    }
}
