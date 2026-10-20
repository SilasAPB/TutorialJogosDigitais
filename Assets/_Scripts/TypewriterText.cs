using TMPro;
using UnityEngine;

// Mostra o texto aos poucos (máquina de escrever). Usa tempo real, então funciona com o jogo pausado.
// Clicar/tecla Espaço ou Enter completa o texto na hora.
[RequireComponent(typeof(TMP_Text))]
public class TypewriterText : MonoBehaviour
{
    public float charsPerSecond = 40f;
    public float startDelay = 0.3f;

    TMP_Text label;
    float elapsed;

    public bool Finished
    {
        get { return label != null && label.maxVisibleCharacters >= label.textInfo.characterCount && label.textInfo.characterCount > 0; }
    }

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        elapsed = -startDelay;
        if (label == null) label = GetComponent<TMP_Text>();
        label.maxVisibleCharacters = 0;
        GameSettings.LanguageChanged += Restart;
    }

    void OnDisable()
    {
        GameSettings.LanguageChanged -= Restart;
        if (label != null) label.maxVisibleCharacters = 99999;
    }

    void Restart()
    {
        elapsed = -startDelay;
        label.maxVisibleCharacters = 0;
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        label.ForceMeshUpdate();
        int total = label.textInfo.characterCount;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            elapsed = Mathf.Max(elapsed, total / Mathf.Max(1f, charsPerSecond));

        label.maxVisibleCharacters = elapsed <= 0f ? 0 : Mathf.Min(total, Mathf.FloorToInt(elapsed * charsPerSecond));
    }
}
