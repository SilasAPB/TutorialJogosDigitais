using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD da partida: timers, engrenagens, munição, barra do boss e cartão de transição entre fases.
public class HudController : MonoBehaviour
{
    [Header("Barra superior")]
    public TMP_Text phaseText;
    public TMP_Text roundTimerText;
    public TMP_Text totalTimeText;
    public TMP_Text stageText;
    public TMP_Text flowerText;
    public TMP_Text specialText;
    public Image[] gearIcons;
    [Header("Aliança (aparece só depois de pega, com o número de usos)")]
    public Image ringIcon;
    public TMP_Text ringText;
    public Sprite[] ringFrames;
    public float ringFps = 8f;
    [Tooltip("Estados da engrenagem: 0 = nova, 1 = enferrujada, 2 = quebrada.")]
    public Sprite[] gearStates;

    [Header("Boss")]
    public GameObject bossBarRoot;
    public Image bossFill;
    public TMP_Text bossNameText;

    [Header("Transição entre fases")]
    public GameObject transitionRoot;
    public TMP_Text transitionTitle;
    public TMP_Text transitionSubtitle;
    public TMP_Text transitionTotalTime;
    public TMP_Text transitionCountdown;

    static readonly Color GearEmpty = new Color(1f, 1f, 1f, 0.2f);
    static readonly Color GearGain = new Color(1f, 0.85f, 0.2f, 1f);
    static readonly Color TimerWarning = new Color(1f, 0.35f, 0.3f, 1f);
    const float PulseDuration = 0.6f;

    int lastGears = -1;
    int pulseIndex = -1;
    float pulseStart;

    void Update()
    {
        var round = RoundManager.Instance;
        if (round == null) return;

        // Ao ganhar uma engrenagem, o ícone dela pisca em dourado e cresce um pouco.
        if (lastGears >= 0 && GameController.gears > lastGears)
        {
            pulseIndex = GameController.gears - 1;
            pulseStart = Time.unscaledTime;
        }
        lastGears = GameController.gears;
        float pulse = Mathf.Clamp01((Time.unscaledTime - pulseStart) / PulseDuration);

        for (int i = 0; i < gearIcons.Length; i++)
        {
            // Sprite conforme o desgaste desta engrenagem (nova -> enferrujada -> quebrada).
            if (gearStates != null && gearStates.Length > 0)
            {
                int damage = i < GameController.gears && i < GameController.gearDamage.Length ? GameController.gearDamage[i] : 0;
                int hits = Mathf.Max(1, GameController.hitsPerGear);
                int state = hits <= 1 ? 0 : Mathf.RoundToInt(damage * (gearStates.Length - 1f) / (hits - 1f));
                gearIcons[i].sprite = gearStates[Mathf.Clamp(state, 0, gearStates.Length - 1)];
            }

            bool pulsing = i == pulseIndex && pulse < 1f;
            gearIcons[i].color = pulsing ? Color.Lerp(GearGain, Color.white, pulse)
                : i < GameController.gears ? Color.white : GearEmpty;
            gearIcons[i].rectTransform.localScale = Vector3.one * (pulsing ? Mathf.Lerp(1.6f, 1f, pulse) : 1f);
        }

        bool hasRing = GameController.ringCount > 0;
        if (ringIcon != null)
        {
            ringIcon.gameObject.SetActive(hasRing);
            if (hasRing && ringFrames != null && ringFrames.Length > 0)
                ringIcon.sprite = ringFrames[(int)(Time.unscaledTime * ringFps) % ringFrames.Length];
        }
        if (ringText != null)
        {
            ringText.gameObject.SetActive(hasRing);
            ringText.text = GameController.ringCount.ToString();
        }

        flowerText.text = GameController.flowerAmmo.ToString();
        specialText.text = GameController.specialAmmo.ToString();
        totalTimeText.text = GameSettings.Tr("Tempo total ", "Total time ") + GameController.FormatTime(GameController.totalTime);
        stageText.text = GameSettings.Tr("ESTÁGIO ", "STAGE ") + Mathf.Min(GameController.stage + 1, round.stages.Length) + "/" + round.stages.Length;

        phaseText.text = round.CurrentPhase == Phase.Harvest
            ? GameSettings.Tr("CORTE A GRAMA", "CUT THE GRASS")
            : GameSettings.Tr("BOSS FIGHT", "BOSS FIGHT");

        roundTimerText.text = GameController.FormatTime(Mathf.Ceil(round.PhaseTimeLeft));
        roundTimerText.color = round.PhaseTimeLeft <= 5f ? TimerWarning : Color.white;

        bool bossPhase = round.CurrentPhase == Phase.Boss;
        bossBarRoot.SetActive(bossPhase);
        if (bossPhase)
            bossFill.fillAmount = GameController.bossMaxHp > 0f ? GameController.bossHp / GameController.bossMaxHp : 0f;
        bossNameText.text = "G.R. ASSHOLE";

        transitionRoot.SetActive(round.InTransition);
        if (round.InTransition)
        {
            transitionTitle.text = round.TransitionTitle;
            transitionSubtitle.text = round.TransitionSubtitle;
            transitionTotalTime.text = GameSettings.Tr("Tempo total: ", "Total time: ") + GameController.FormatTime(GameController.totalTime);
            transitionCountdown.text = Mathf.Max(1, Mathf.CeilToInt(round.TransitionTimeLeft)).ToString();
        }
    }
}
