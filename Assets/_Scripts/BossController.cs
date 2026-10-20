using System.Collections;
using UnityEngine;

// G.R. Asshole (Giant Root): flutua pela arena e solta padrões de projéteis (estilo Soul Knight).
// A vida fica em GameController.bossHp, então persiste entre as rodadas.
public class BossController : MonoBehaviour
{
    [Header("Projéteis")]
    [Tooltip("Normal: bola de fogo (anéis e espirais).")]
    public BossBullet bulletPrefab;
    [Tooltip("Rápido: espinho roxo (leques mirados). Se vazio, usa o normal.")]
    public BossBullet fastBulletPrefab;
    [Tooltip("Grande: esfera de espinhos (anel grande e lento). Se vazio, usa o normal.")]
    public BossBullet bigBulletPrefab;
    public GameObject deathEffectPrefab;
    public Vector2 homePosition = new Vector2(3f, 1.5f);
    public Vector2 arenaMin = new Vector2(-5.2f, -2.4f);
    public Vector2 arenaMax = new Vector2(5.2f, 2.8f);
    public float moveSpeed = 1.2f;
    public float openingDelay = 2.5f;

    [Header("Visual (8 direções, mesma ordem do herói)")]
    [Tooltip("Baixo, baixo-esq, esquerda, cima-esq, cima, cima-dir, direita, baixo-dir. Uma imagem por direção (usada se não houver walkSheet).")]
    public Sprite[] facingSprites;
    [Tooltip("Ciclo de caminhada: 8 linhas (mesma ordem acima) x 6 colunas, em ordem linha a linha.")]
    public Sprite[] walkSheet;
    public float walkFps = 9f;
    float walkTime;
    Sprite[] activeSheet;

    SpriteRenderer sprite;
    Sprite defaultSprite;
    Color baseColor = Color.white;
    Vector2 waypoint;
    Vector2 lastMove = Vector2.down;
    float nextWaypoint;
    float flashUntil;
    StageConfig stage;

    bool Alive
    {
        get { return GameController.bossHp > 0f && !GameController.gameOver; }
    }

    float SpeedMultiplier
    {
        get { return stage != null ? stage.bossSpeedMultiplier : 1f; }
    }

    float BulletSpeed
    {
        get { return stage != null ? stage.bulletSpeed : 4f; }
    }

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
        {
            baseColor = sprite.color;
            defaultSprite = sprite.sprite;
        }
    }

    public void Activate()
    {
        stage = RoundManager.Instance.CurrentStage;
        transform.position = homePosition;
        waypoint = homePosition;
        nextWaypoint = 0f;
        gameObject.SetActive(true);

        // Cada estágio tem o seu boss: sheet de caminhada e tamanho próprios (maior e mais bravo no estágio 2).
        activeSheet = stage.bossWalkSheet != null && stage.bossWalkSheet.Length >= 48 ? stage.bossWalkSheet : walkSheet;
        transform.localScale = Vector3.one * stage.bossScale;
        if (sprite != null)
        {
            sprite.sprite = stage.bossSprite != null ? stage.bossSprite : defaultSprite;
            baseColor = stage.bossTint;
            sprite.color = baseColor;
        }
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void Die()
    {
        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        gameObject.SetActive(false);
    }

    public void TakeDamage(float amount)
    {
        GameController.DamageBoss(amount);
        flashUntil = Time.time + 0.08f;
    }

    void OnEnable()
    {
        StartCoroutine(AttackLoop());
    }

    void Update()
    {
        if (RoundManager.Instance == null || !RoundManager.Instance.Playing) return;

        if (Time.time >= nextWaypoint)
        {
            waypoint = new Vector2(Random.Range(arenaMin.x, arenaMax.x), Random.Range(arenaMin.y, arenaMax.y));
            nextWaypoint = Time.time + Random.Range(2f, 3.5f);
        }

        Vector2 before = transform.position;
        transform.position = Vector2.MoveTowards(transform.position, waypoint, moveSpeed * SpeedMultiplier * Time.deltaTime);
        Vector2 step = (Vector2)transform.position - before;
        if (step.sqrMagnitude > 1e-8f) lastMove = step;

        if (sprite != null)
        {
            // O sprite muda conforme a direção em que o boss anda (8 direções), com ciclo de caminhada.
            int row = HeroController.RowForAim(lastMove);
            if (activeSheet != null && activeSheet.Length >= 48)
            {
                walkTime += Time.deltaTime * walkFps;
                sprite.sprite = activeSheet[row * 6 + (int)walkTime % 6];
            }
            else if (facingSprites != null && facingSprites.Length >= 8)
            {
                sprite.sprite = facingSprites[row];
            }
            sprite.color = Time.time < flashUntil ? Color.Lerp(baseColor, Color.white, 0.7f) : baseColor;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var hero = other.GetComponentInParent<HeroController>();
        if (hero != null) hero.TakeHit(RingPower.ReduceBossDamage(1));
    }

    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(openingDelay);

        int index = 0;
        while (Alive)
        {
            switch (index % 4)
            {
                case 0: yield return Radial(stage.radialCount, stage.radialRings); break;
                case 1: yield return AimedFan(stage.fanBullets, 14f, stage.fanVolleys); break;
                case 2: yield return Spiral(2.4f, stage.spiralArms); break;
                // anel de esferas grandes e lentas (menos balas, mas é preciso desviar com mais cuidado)
                default: yield return Radial(Mathf.Max(6, stage.radialCount / 2), 2, bigBulletPrefab, 0.7f); break;
            }
            index++;
            yield return new WaitForSeconds(stage.attackPause / SpeedMultiplier);
        }
    }

    void Fire(float angleDegrees, float speed, BossBullet prefab = null)
    {
        if (prefab == null) prefab = bulletPrefab;
        if (!Alive || prefab == null) return;

        float rad = angleDegrees * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        BossBullet bullet = Instantiate(prefab, transform.position, Quaternion.identity);
        bullet.Launch(direction, speed);
    }

    // Anéis de projéteis, cada um girado em relação ao anterior.
    IEnumerator Radial(int count, int rings, BossBullet prefab = null, float speedFactor = 1f)
    {
        for (int ring = 0; ring < rings && Alive; ring++)
        {
            float offset = ring * (180f / count);
            for (int i = 0; i < count; i++) Fire(offset + i * 360f / count, BulletSpeed * speedFactor, prefab);
            yield return new WaitForSeconds(0.4f / SpeedMultiplier);
        }
    }

    // Leque de tiros mirados no herói.
    IEnumerator AimedFan(int bullets, float spread, int volleys)
    {
        for (int v = 0; v < volleys && Alive; v++)
        {
            Vector2 toHero = (Vector2)RoundManager.Instance.hero.transform.position - (Vector2)transform.position;
            float aim = Mathf.Atan2(toHero.y, toHero.x) * Mathf.Rad2Deg;
            for (int i = 0; i < bullets; i++)
            {
                float offset = (i - (bullets - 1) / 2f) * spread;
                Fire(aim + offset, BulletSpeed * 1.4f, fastBulletPrefab);
            }
            yield return new WaitForSeconds(0.55f / SpeedMultiplier);
        }
    }

    // Espiral contínua com vários braços.
    IEnumerator Spiral(float duration, int arms)
    {
        float elapsed = 0f;
        float angle = Random.Range(0f, 360f);
        float interval = stage.spiralInterval / SpeedMultiplier;
        while (elapsed < duration && Alive)
        {
            for (int a = 0; a < arms; a++) Fire(angle + a * 360f / arms, BulletSpeed);
            angle += 17f;
            elapsed += interval;
            yield return new WaitForSeconds(interval);
        }
    }
}
