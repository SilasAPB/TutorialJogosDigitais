using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuActions : MonoBehaviour
{
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public Slider volumeSlider;
    [Tooltip("Tela de introdução (lore). No menu, Jogar abre esta tela antes de começar; no jogo (Reiniciar) fica vazio.")]
    public GameObject introPanel;

    void Start()
    {
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        Voltar();
    }

    // Botão Jogar: mostra a introdução (se houver) e só depois começa. Reiniciar, no jogo, começa direto.
    public void IniciaJogo()
    {
        if (introPanel != null)
        {
            introPanel.SetActive(true);
            return;
        }
        ComecaJogo();
    }

    // Botão Pular da introdução (ou fim dela).
    public void ComecaJogo()
    {
        Time.timeScale = 1f;
        GameController.Init();
        SceneManager.LoadScene(1);
    }

    public void VoltaMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void AbreOpcoes()
    {
        optionsPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    public void AbreCreditos()
    {
        creditsPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    public void Voltar()
    {
        // Na SampleScene este componente só é usado para IniciaJogo/VoltaMenuPrincipal, sem painéis de menu.
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (introPanel != null) introPanel.SetActive(false);
    }

    public void TrocaIdioma()
    {
        GameSettings.ToggleLanguage();
        UISfx.Play(GameSettings.Language == GameLanguage.English ? UiSound.ToggleOn : UiSound.ToggleOff);
    }

    public void MudaVolume(float volume)
    {
        GameSettings.SetVolume(volume);
    }
}
