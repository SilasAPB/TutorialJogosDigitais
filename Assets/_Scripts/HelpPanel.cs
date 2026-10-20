using UnityEngine;

// Painel de ajuda (botão "?"): comandos e itens. No jogo ele pausa tudo enquanto está aberto.
public class HelpPanel : MonoBehaviour
{
    public GameObject root;
    public GameObject commandsPage;
    public GameObject itemsPage;

    bool open;
    float previousTimeScale = 1f;

    public void Open()
    {
        if (open) return;

        open = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        GameController.paused = true;
        root.SetActive(true);
        ShowCommands();
    }

    public void Close()
    {
        if (!open) return;

        open = false;
        root.SetActive(false);
        GameController.paused = false;
        Time.timeScale = previousTimeScale;
    }

    public void ShowCommands()
    {
        commandsPage.SetActive(true);
        itemsPage.SetActive(false);
    }

    public void ShowItems()
    {
        commandsPage.SetActive(false);
        itemsPage.SetActive(true);
    }

    void Update()
    {
        if (open && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void OnDestroy()
    {
        if (open)
        {
            GameController.paused = false;
            Time.timeScale = previousTimeScale;
        }
    }
}
