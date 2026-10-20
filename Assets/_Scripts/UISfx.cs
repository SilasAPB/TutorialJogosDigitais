using System.Collections.Generic;
using UnityEngine;

public enum UiSound
{
    Hover,
    Click,
    Confirm,
    Open,
    Back,
    ToggleOn,
    ToggleOff,
    SliderTick
}

// Sons de interface. Vive entre as cenas (a fonte de áudio é criada sozinha) e respeita o volume global do jogo.
// Os arquivos ficam em Assets/Resources/UISfx.
public static class UISfx
{
    struct Entry
    {
        public string file;
        public float volume;
    }

    static readonly Dictionary<UiSound, Entry> Table = new Dictionary<UiSound, Entry>
    {
        { UiSound.Hover,      new Entry { file = "ui_hover_01",    volume = 0.45f } },
        { UiSound.Click,      new Entry { file = "ui_click_01",    volume = 0.9f } },
        { UiSound.Confirm,    new Entry { file = "ui_confirm_02",  volume = 0.9f } },
        { UiSound.Open,       new Entry { file = "ui_popup_open",  volume = 0.8f } },
        { UiSound.Back,       new Entry { file = "ui_popup_close", volume = 0.8f } },
        { UiSound.ToggleOn,   new Entry { file = "ui_toggle_on",   volume = 0.85f } },
        { UiSound.ToggleOff,  new Entry { file = "ui_toggle_off",  volume = 0.85f } },
        { UiSound.SliderTick, new Entry { file = "ui_slider_tick", volume = 0.6f } },
    };

    static readonly Dictionary<UiSound, AudioClip> Clips = new Dictionary<UiSound, AudioClip>();
    static AudioSource source;

    public static void Play(UiSound sound)
    {
        EnsureSource();

        AudioClip clip;
        if (!Clips.TryGetValue(sound, out clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>("UISfx/" + Table[sound].file);
            Clips[sound] = clip;
        }
        if (clip == null) return;

        source.PlayOneShot(clip, Table[sound].volume);
    }

    static void EnsureSource()
    {
        if (source != null) return;

        var go = new GameObject("UISfx");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }
}
