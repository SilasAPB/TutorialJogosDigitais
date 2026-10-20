using System.Collections;
using TMPro;
using UnityEngine;

// Poder da aliança. Ao pegá-la, o jogo pausa e mostra uma mensagem. Na luta contra o boss, ESPAÇO a usa:
// aparecem frases no topo da tela, uma por vez (fade de entrada e saída, 3 s cada). Enquanto duram,
// o herói atira o dobro de rápido, os tiros mudam de sprite e a bomba causa o dobro de dano.
public class RingPower : MonoBehaviour
{
    public static RingPower Instance { get; private set; }

    // Verdadeiro enquanto as frases estão na tela.
    public static bool Active { get; private set; }

    // Com o poder ativo, todo ataque do boss causa 1 ponto de dano a menos (nunca abaixo de zero).
    public static int ReduceBossDamage(int damage)
    {
        return Active ? Mathf.Max(0, damage - 1) : damage;
    }

    [Header("Mensagem ao encontrar a aliança")]
    public GameObject foundPanel;

    [Header("Legenda no topo da tela")]
    public CanvasGroup legendGroup;
    public TMP_Text legendText;
    public float secondsPerLine = 3f;
    public float fadeSeconds = 0.5f;

    [Header("Frases (edite aqui, uma por linha)")]
    [TextArea] public string[] linesPt =
    {
        "Eu sei que um dia você terá uma vida linda",
        "Eu sei que você será uma estrela no céu de outra pessoa",
        "Mas por que? Por que? Por que não pode ser ",
        "Ah, não pode ser...",
        "MIIIIIIIIIIIIIIINHA!"
    };
    [TextArea] public string[] linesEn =
    {
        "I know someday you'll have a beautiful life",
        "I know you'll be a star in somebody else's sky",
        "But why, why, why can't it be",
        "Oh, can't it be...",
        "MIIIIIIIIIIIIIIINE!"
    };

    Coroutine running;

    void Awake()
    {
        Instance = this;
        Active = false;
        if (legendGroup != null) legendGroup.alpha = 0f;
        if (foundPanel != null) foundPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Active = false;
    }

    [Header("Onde a aliança aparece")]
    [Tooltip("Chance (0 a 1) de achar a aliança a cada mato alto cortado, só na fase com piscina e uma única vez no jogo.")]
    [Range(0f, 0.2f)] public float findChance = 0.01f;

    // Chamado a cada mato alto cortado: com uma chance pequena, só na fase da piscina, o cortador acha a aliança.
    public void TryFindWhileCutting()
    {
        if (GameController.ringCollected || RoundManager.Instance == null) return;
        if (!RoundManager.Instance.CurrentStage.hasPool) return;
        if (Random.value >= findChance) return;

        GameController.ringCollected = true;
        GameController.ringCount = 1;
        ShowFoundMessage();
    }

    // Pausa o jogo e mostra a mensagem de que a aliança foi encontrada.
    public void ShowFoundMessage()
    {
        if (foundPanel == null) return;

        foundPanel.SetActive(true);
        GameController.paused = true;
        Time.timeScale = 0f;
    }

    // Botão "Entendi" da mensagem.
    public void CloseFoundMessage()
    {
        if (foundPanel != null) foundPanel.SetActive(false);
        GameController.paused = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        var round = RoundManager.Instance;
        if (round == null) return;

        // Sair da luta (ou perder) encerra o efeito.
        if (running != null && (round.CurrentPhase != Phase.Boss || GameController.gameOver))
        {
            StopCoroutine(running);
            running = null;
            Active = false;
            if (legendGroup != null) legendGroup.alpha = 0f;
        }

        if (running == null && GameController.ringCount > 0 && round.Playing
            && round.CurrentPhase == Phase.Boss && Input.GetKeyDown(KeyCode.Space))
        {
            GameController.ringCount--;
            running = StartCoroutine(Play());
        }
    }

    IEnumerator Play()
    {
        Active = true;
        string[] lines = GameSettings.Language == GameLanguage.English ? linesEn : linesPt;

        foreach (string line in lines)
        {
            legendText.text = line;
            float hold = Mathf.Max(0f, secondsPerLine - fadeSeconds * 2f);
            yield return Fade(0f, 1f, fadeSeconds);
            yield return new WaitForSeconds(hold);
            yield return Fade(1f, 0f, fadeSeconds);
        }

        Active = false;
        running = null;
    }

    IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            legendGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.01f, seconds)));
            yield return null;
        }
        legendGroup.alpha = to;
    }
}
