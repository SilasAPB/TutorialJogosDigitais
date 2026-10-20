using System;
using UnityEngine;

public enum GameLanguage
{
    Portuguese = 0,
    English = 1
}

// Configurações globais (idioma e volume). Persistem entre cenas e entre execuções via PlayerPrefs.
public static class GameSettings
{
    const string LanguageKey = "settings.language";
    const string VolumeKey = "settings.volume";

    public static event Action LanguageChanged;

    public static GameLanguage Language { get; private set; } = GameLanguage.Portuguese;
    public static float Volume { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        Language = (GameLanguage)PlayerPrefs.GetInt(LanguageKey, (int)GameLanguage.Portuguese);
        Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        AudioListener.volume = Volume;
    }

    public static void SetLanguage(GameLanguage language)
    {
        if (Language == language) return;

        Language = language;
        PlayerPrefs.SetInt(LanguageKey, (int)language);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    // Atalho para textos montados em código: Tr("Olá", "Hello").
    public static string Tr(string portuguese, string english)
    {
        return Language == GameLanguage.English ? english : portuguese;
    }

    public static void ToggleLanguage()
    {
        SetLanguage(Language == GameLanguage.Portuguese ? GameLanguage.English : GameLanguage.Portuguese);
    }

    public static void SetVolume(float volume)
    {
        Volume = Mathf.Clamp01(volume);
        AudioListener.volume = Volume;
        PlayerPrefs.SetFloat(VolumeKey, Volume);
    }
}
