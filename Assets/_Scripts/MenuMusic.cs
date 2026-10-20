using UnityEngine;

// Música da tela de entrada: toca em loop com entrada suave. Quando a introdução (lore) abre, faz crossfade para a
// música da história. Para sozinha quando a cena é trocada (ex.: ao pular a introdução e começar o jogo).
// O volume final segue o volume global (AudioListener.volume), ajustado nas Opções.
public class MenuMusic : MonoBehaviour
{
    public AudioClip clip;
    [Tooltip("Música da introdução (lore).")]
    public AudioClip storyClip;
    public GameObject introPanel;
    [Range(0f, 1f)] public float volume = 0.55f;
    [Range(0f, 1f)] public float storyVolume = 0.55f;
    public float fadeInSeconds = 1.5f;
    public float crossfadeSeconds = 1.2f;

    AudioSource menu;
    AudioSource story;
    float elapsed;

    void Awake()
    {
        menu = gameObject.AddComponent<AudioSource>();
        Setup(menu, clip, fadeInSeconds > 0f ? 0f : volume);
        story = gameObject.AddComponent<AudioSource>();
        Setup(story, storyClip, 0f);
    }

    static void Setup(AudioSource source, AudioClip clip, float startVolume)
    {
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = startVolume;
    }

    void Start()
    {
        if (clip != null) menu.Play();
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        bool intro = introPanel != null && introPanel.activeInHierarchy;

        float menuTarget = intro ? 0f : volume;
        float storyTarget = intro && storyClip != null ? storyVolume : 0f;

        // a entrada inicial do menu usa o fade-in; depois, os crossfades
        float menuStep = elapsed < fadeInSeconds ? volume / Mathf.Max(0.01f, fadeInSeconds) : Mathf.Max(volume, storyVolume, 0.01f) / Mathf.Max(0.05f, crossfadeSeconds);
        float storyStep = Mathf.Max(volume, storyVolume, 0.01f) / Mathf.Max(0.05f, crossfadeSeconds);

        Fade(menu, menuTarget, menuStep * Time.unscaledDeltaTime);
        Fade(story, storyTarget, storyStep * Time.unscaledDeltaTime);
    }

    static void Fade(AudioSource source, float target, float step)
    {
        if (source.clip == null) return;

        if (target > 0f && !source.isPlaying) source.Play();   // retoma de onde parou
        source.volume = Mathf.MoveTowards(source.volume, target, step);
        if (target <= 0f && source.volume <= 0f && source.isPlaying) source.Pause();
    }
}
