using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ButtonClickSound
{
    Click,
    Confirm,
    Open,
    Back,
    None
}

// Som de passar o mouse e de clicar para um botão. 'None' deixa o clique mudo (quem o aciona toca o próprio som).
[RequireComponent(typeof(Button))]
public class ButtonSfx : MonoBehaviour, IPointerEnterHandler
{
    public ButtonClickSound clickSound = ButtonClickSound.Click;
    public bool hover = true;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hover && button.interactable) UISfx.Play(UiSound.Hover);
    }

    void OnClick()
    {
        switch (clickSound)
        {
            case ButtonClickSound.Click: UISfx.Play(UiSound.Click); break;
            case ButtonClickSound.Confirm: UISfx.Play(UiSound.Confirm); break;
            case ButtonClickSound.Open: UISfx.Play(UiSound.Open); break;
            case ButtonClickSound.Back: UISfx.Play(UiSound.Back); break;
        }
    }
}
