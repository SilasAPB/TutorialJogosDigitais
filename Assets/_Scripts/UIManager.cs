using TMPro;
using UnityEngine;

// Painel de fim de jogo (derrota ou vitória) com o tempo total da partida.
public class UIManager : MonoBehaviour
{
    public GameObject endGamePanel;
    [Tooltip("Mensagem de conclusão: aparece antes do painel de fim de jogo, só quando o jogador vence.")]
    public GameObject conclusionPanel;
    [Tooltip("Mensagem de derrota: aparece antes do painel de fim de jogo, quando o jogador perde.")]
    public GameObject defeatPanel;
    public TMP_Text titleText;
    public TMP_Text timeText;

    bool shown;
    bool conclusionShown;
    bool conclusionDone;

    // Botão "Continuar" da mensagem de conclusão.
    public void ContinuaDaConclusao()
    {
        conclusionDone = true;
        if (conclusionPanel != null) conclusionPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    void Update()
    {
        if (!GameController.gameOver || shown) return;

        GameObject story = GameController.won ? conclusionPanel : defeatPanel;
        if (story != null && !conclusionDone)
        {
            if (!conclusionShown)
            {
                conclusionShown = true;
                story.SetActive(true);
            }
            return;
        }

        shown = true;
        if (titleText != null)
        {
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 28f;
            titleText.fontSizeMax = 56f;
            titleText.text = TitleFor(GameController.won, GameController.deathCause);
        }
        if (timeText != null)
        {
            timeText.text = GameSettings.Tr("Tempo total: ", "Total time: ") + GameController.FormatTime(GameController.totalTime);
        }
        endGamePanel.SetActive(true);
    }

    static string TitleFor(bool won, DeathCause cause)
    {
        if (won) return GameSettings.Tr("VITÓRIA!", "VICTORY!");

        switch (cause)
        {
            case DeathCause.MowerBroke:
                return GameSettings.Tr("O CORTADOR PAROU DE FUNCIONAR!", "THE MOWER BROKE DOWN!");
            case DeathCause.Drowned:
                return GameSettings.Tr("VOCÊ SE AFOGOU!", "YOU DROWNED!");
            default:
                return GameSettings.Tr("FIM DE JOGO", "GAME OVER");
        }
    }
}
