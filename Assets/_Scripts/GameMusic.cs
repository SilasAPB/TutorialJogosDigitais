using UnityEngine;

// Música do jogo por fase. Cada faixa tem a sua própria fonte de áudio: ao trocar de fase uma faixa some em fade
// (pausada, guardando o ponto onde parou) e a outra entra. Se a fase não tem faixa, fica em silêncio.
// Usa tempo "real" para os fades, então continua funcionando nos cartões de transição (timeScale = 0).
public class GameMusic : MonoBehaviour
{
    public AudioClip harvestClip;
    public AudioClip bossClip;
    [Tooltip("Toca na mensagem de conclusão e no fim de jogo (vitória ou derrota).")]
    public AudioClip endClip;
    [Range(0f, 1f)] public float harvestVolume = 0.5f;
    [Range(0f, 1f)] public float bossVolume = 0.5f;
    [Range(0f, 1f)] public float endVolume = 0.55f;
    public float fadeSeconds = 1.2f;

    AudioSource harvest;
    AudioSource boss;
    AudioSource ending;

    void Awake()
    {
        harvest = NewSource(harvestClip);
        boss = NewSource(bossClip);
        ending = NewSource(endClip);
    }

    AudioSource NewSource(AudioClip clip)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }

    void Update()
    {
        var round = RoundManager.Instance;
        bool over = GameController.gameOver;

        float harvestTarget = 0f;
        float bossTarget = 0f;
        float endTarget = 0f;
        if (over)
        {
            endTarget = endVolume;
        }
        else if (round != null)
        {
            if (round.CurrentPhase == Phase.Harvest) harvestTarget = harvestVolume;
            else bossTarget = bossVolume;
        }

        Fade(harvest, harvestTarget);
        Fade(boss, bossTarget);
        Fade(ending, endTarget);
    }

    void Fade(AudioSource source, float target)
    {
        if (source == null || source.clip == null) return;

        if (target > 0f && !source.isPlaying) source.Play();   // retoma de onde parou

        float step = Time.unscaledDeltaTime * Mathf.Max(harvestVolume, bossVolume, endVolume, 0.01f) / Mathf.Max(0.05f, fadeSeconds);
        source.volume = Mathf.MoveTowards(source.volume, target, step);

        if (target <= 0f && source.volume <= 0f && source.isPlaying) source.Pause();   // guarda a posição
    }
}
