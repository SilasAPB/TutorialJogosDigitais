using System;
using System.Collections;
using UnityEngine;

public enum Phase
{
    Harvest,
    Boss
}

[Serializable]
public class StageConfig
{
    public float bossMaxHp = 400f;
    public float grassSpawnInterval = 0.5f;
    [Range(0f, 1f)] public float initialFill = 0.5f;
    public float bossSpeedMultiplier = 1f;
    public float bulletSpeed = 4f;
    public Sprite bossSprite;
    public Color bossTint = Color.white;
    [Tooltip("Ciclo de caminhada próprio deste estágio (48 quadros: 8 linhas x 6 colunas). Vazio = usa a sheet padrão do boss.")]
    public Sprite[] bossWalkSheet;
    [Tooltip("Tamanho do boss neste estágio.")]
    public float bossScale = 1.6f;

    [Header("Ataques do boss")]
    [Tooltip("Segundos de pausa entre um ataque e outro (dividido pelo multiplicador de velocidade).")]
    public float attackPause = 1.2f;
    public int radialCount = 14;
    public int radialRings = 3;
    public int fanBullets = 5;
    public int fanVolleys = 3;
    public int spiralArms = 3;
    public float spiralInterval = 0.08f;

    [Header("Perigos do mapa")]
    public bool hasPool;
    public RectInt poolArea = new RectInt(1, -6, 7, 4);
    [Range(0f, 1f)] public float weedChance;
    public int maxWeeds;
    public int initialWeeds;
}

// Alterna as fases (corte de grama / boss), controla os timers e o avanço de estágio.
public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    public float phaseDuration = 30f;
    public float transitionDuration = 3f;
    [Tooltip("Golpes que cada engrenagem aguenta (nova, enferrujada, quebrada) antes de sumir.")]
    public int hitsPerGear = 3;
    public StageConfig[] stages;

    public GrassManager grass;
    public MowerController mower;
    public HeroController hero;
    public BossController boss;
    public Vector2Int mowerStartCell = new Vector2Int(-8, 0);

    [Header("Engrenagem na fase do boss")]
    public GearPickup gearPickupPrefab;
    public float bossGearInterval = 9f;
    public int maxBossGears = 1;
    public Vector2 pickupAreaMin = new Vector2(-6.5f, -3.6f);
    public Vector2 pickupAreaMax = new Vector2(6.5f, 3.6f);
    float nextGearDrop;

    public Phase CurrentPhase { get; private set; }
    public float PhaseTimeLeft { get; private set; }
    bool playing;

    // Falso enquanto o painel de ajuda está aberto: tudo que depende de "Playing" congela.
    public bool Playing
    {
        get { return playing && !GameController.paused; }
        private set { playing = value; }
    }

    public bool InTransition { get; private set; }
    public string TransitionTitle { get; private set; }
    public string TransitionSubtitle { get; private set; }
    public float TransitionTimeLeft { get; private set; }

    public StageConfig CurrentStage
    {
        get { return stages[Mathf.Clamp(GameController.stage, 0, stages.Length - 1)]; }
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        GameController.hitsPerGear = hitsPerGear;
        GameController.Init();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    void Start()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        mower.gameObject.SetActive(false);
        hero.gameObject.SetActive(false);
        boss.gameObject.SetActive(false);

        SetupStage(true);
        Vector3 lastPosition = grass.CellCenter(mowerStartCell);

        bool newStage = false;
        while (!GameController.gameOver)
        {
            string roundTitle = GameSettings.Tr("RODADA ", "ROUND ") + GameController.round;
            string roundSubtitle = GameSettings.Tr("Corte a grama e junte munição!", "Cut the grass and collect ammo!");
            if (newStage)
            {
                roundTitle = GameSettings.Tr("ESTÁGIO ", "STAGE ") + (GameController.stage + 1) + " - " + roundTitle;
                roundSubtitle = GameSettings.Tr("O G.R. voltou mais forte! ", "G.R. is back, and stronger! ");
                if (CurrentStage.hasPool || CurrentStage.maxWeeds > 0)
                    roundSubtitle += GameSettings.Tr("Cuidado com a piscina e as ervas daninhas!", "Watch out for the pool and the weeds!");
                newStage = false;
            }

            yield return Transition(Phase.Harvest, roundTitle, roundSubtitle);

            EnterHarvest(lastPosition);
            yield return PlayPhase(Phase.Harvest);
            if (GameController.gameOver) break;
            lastPosition = mower.transform.position;

            yield return Transition(
                Phase.Boss,
                GameSettings.Tr("HORA DO BOSS!", "BOSS TIME!"),
                GameSettings.Tr("Mire com o mouse: direito = folha, esquerdo = bomba", "Aim with the mouse: right = leaf, left = bomb"));

            EnterBoss(lastPosition);
            yield return PlayPhase(Phase.Boss);
            if (GameController.gameOver) break;
            lastPosition = hero.transform.position;

            if (GameController.bossHp <= 0f)
            {
                boss.Die();
                GameController.stage++;
                if (GameController.stage >= stages.Length)
                {
                    GameController.won = true;
                    GameController.gameOver = true;
                    break;
                }

                SetupStage(true);
                newStage = true;
            }

            GameController.round++;
        }

        Playing = false;
        Time.timeScale = 0f;
    }

    void SetupStage(bool refillGrass)
    {
        StageConfig config = CurrentStage;
        GameController.bossMaxHp = config.bossMaxHp;
        GameController.bossHp = config.bossMaxHp;
        grass.ConfigureStage(config);
        if (refillGrass) grass.ResetField(config.initialFill, mowerStartCell, config.initialWeeds);
    }

    void EnterHarvest(Vector3 position)
    {
        CurrentPhase = Phase.Harvest;
        ClearProjectiles();
        hero.gameObject.SetActive(false);
        boss.Deactivate();

        mower.gameObject.SetActive(true);
        mower.PlaceAt(position);
        grass.Active = true;
    }

    void EnterBoss(Vector3 position)
    {
        CurrentPhase = Phase.Boss;
        grass.Active = false;
        grass.RemoveGearTiles();
        mower.gameObject.SetActive(false);

        hero.gameObject.SetActive(true);
        hero.PlaceAt(position);
        boss.Activate();
        GearPickup.ClearAll();
        nextGearDrop = Time.time + bossGearInterval;
    }

    // Na fase do boss as engrenagens também nascem soltas na arena; o herói pega andando.
    void Update()
    {
        if (!Playing || CurrentPhase != Phase.Boss) return;

        if (gearPickupPrefab == null) return;
        if (Time.time < nextGearDrop) return;

        nextGearDrop = Time.time + bossGearInterval;
        if (GearPickup.Count >= maxBossGears || !GameController.CanUseGear) return;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            var spot = new Vector2(UnityEngine.Random.Range(pickupAreaMin.x, pickupAreaMax.x), UnityEngine.Random.Range(pickupAreaMin.y, pickupAreaMax.y));
            if (Vector2.Distance(spot, hero.transform.position) < 1.5f) continue;
            if (Vector2.Distance(spot, boss.transform.position) < 1.8f) continue;
            Instantiate(gearPickupPrefab, new Vector3(spot.x, spot.y, 0f), Quaternion.identity);
            return;
        }
    }

    IEnumerator PlayPhase(Phase phase)
    {
        PhaseTimeLeft = phaseDuration;
        Playing = true;

        while (PhaseTimeLeft > 0f && !GameController.gameOver)
        {
            if (phase == Phase.Boss && GameController.bossHp <= 0f) break;

            float dt = Time.deltaTime;
            // Enquanto as frases da aliança estão na tela, o tempo da rodada fica parado (o tempo total segue contando).
            if (!(phase == Phase.Boss && RingPower.Active)) PhaseTimeLeft -= dt;
            GameController.totalTime += dt;
            yield return null;
        }

        PhaseTimeLeft = Mathf.Max(0f, PhaseTimeLeft);
        Playing = false;
    }

    // Durante o cartão, o HUD já mostra a fase que vem aí e o tempo cheio dela.
    IEnumerator Transition(Phase upcoming, string title, string subtitle)
    {
        Playing = false;
        CurrentPhase = upcoming;
        PhaseTimeLeft = phaseDuration;
        InTransition = true;
        TransitionTitle = title;
        TransitionSubtitle = subtitle;
        TransitionTimeLeft = transitionDuration;
        Time.timeScale = 0f;

        while (TransitionTimeLeft > 0f)
        {
            TransitionTimeLeft -= Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        InTransition = false;
    }

    static void ClearProjectiles()
    {
        BossBullet.ClearAll();
        LeafShot.ClearAll();
        AntBomb.ClearAll();
        GearPickup.ClearAll();
    }
}
