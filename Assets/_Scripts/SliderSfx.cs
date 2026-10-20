using UnityEngine;
using UnityEngine.UI;

// Toca um "tick" curto enquanto o jogador arrasta o slider (com limite de frequência para não virar zumbido).
[RequireComponent(typeof(Slider))]
public class SliderSfx : MonoBehaviour
{
    public float minInterval = 0.06f;

    Slider slider;
    float lastTime = -10f;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnChanged);
    }

    void OnDestroy()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(OnChanged);
    }

    void OnChanged(float value)
    {
        if (Time.unscaledTime - lastTime < minInterval) return;

        lastTime = Time.unscaledTime;
        UISfx.Play(UiSound.SliderTick);
    }
}
