using System.Collections;
using UnityEngine;

// Fase 2: o jogador anda em 8 direções e atira com o mouse.
// Botão direito = folha pontiaguda (munição de flor). Botão esquerdo = bomba de formiga (munição especial).
[RequireComponent(typeof(Rigidbody2D))]
public class HeroController : MonoBehaviour
{
    public float speed = 5f;

    [Header("Visual (folha de 6 colunas x 8 linhas)")]
    [Tooltip("Linhas: baixo, baixo-esq, esquerda, cima-esq, cima, cima-dir, direita, baixo-dir. Colunas = caminhada.")]
    public Sprite[] walkSheet;
    public float walkFps = 10f;

    public LeafShot leafPrefab;
    public AntBomb bombPrefab;
    [Header("Tiros dourados (poder da aliança: o dobro da cadência e da bomba)")]
    public LeafShot goldLeafPrefab;
    public AntBomb goldBombPrefab;
    public float leafCooldown = 0.15f;
    public float bombCooldown = 0.6f;
    public float invulnerableSeconds = 1.2f;
    public Vector2 arenaMin = new Vector2(-7f, -4.2f);
    public Vector2 arenaMax = new Vector2(7f, 4.2f);

    // Setor de 45 graus (0 = direita, anti-horário) -> linha da folha de sprites.
    static readonly int[] RowBySector = { 6, 5, 4, 3, 2, 1, 0, 7 };

    Rigidbody2D body;
    SpriteRenderer sprite;
    int facingRow;
    float animTime;
    float nextLeaf;
    float nextBomb;
    float invulnerableUntil;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
    }

    public void PlaceAt(Vector3 position)
    {
        position.z = 0f;
        transform.position = position;
        if (body != null) body.linearVelocity = Vector2.zero;
        invulnerableUntil = 0f;
        if (sprite != null) sprite.enabled = true;
    }

    void Update()
    {
        if (RoundManager.Instance == null || !RoundManager.Instance.Playing)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        body.linearVelocity = input.normalized * speed;

        // Perigos do mapa: piscina afoga na hora; erva daninha machuca com poucas engrenagens.
        GrassManager grass = RoundManager.Instance.grass;
        Vector3Int cell = grass.WorldToCell(transform.position);
        if (grass.IsPool(cell))
        {
            GameController.Kill(DeathCause.Drowned);
            return;
        }
        if (grass.IsWeedBlocking(cell)) TakeHit();

        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0f;

        // O herói olha para onde o mouse mira (8 direções), mesmo andando para outro lado.
        Vector2 aim = (Vector2)mouse - (Vector2)transform.position;
        if (aim.sqrMagnitude > 0.01f) facingRow = RowForAim(aim);

        if (sprite != null)
        {
            if (walkSheet != null && walkSheet.Length >= 48)
            {
                bool walking = input.sqrMagnitude > 0.01f;
                if (walking) animTime += Time.deltaTime * walkFps;
                sprite.flipX = false;
                sprite.sprite = walkSheet[facingRow * 6 + (walking ? (int)animTime % 6 : 0)];
            }
            else
            {
                float dx = mouse.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.05f) sprite.flipX = dx < 0f;
            }
        }

        // Poder da aliança: o dobro da cadência, tiros dourados e bomba com o dobro de dano.
        bool power = RingPower.Active;
        float rate = power ? 0.5f : 1f;

        if (Input.GetMouseButton(1) && Time.time >= nextLeaf && GameController.UseFlower())
        {
            nextLeaf = Time.time + leafCooldown * rate;
            Vector2 direction = ((Vector2)mouse - (Vector2)transform.position).normalized;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
            LeafShot prefab = power && goldLeafPrefab != null ? goldLeafPrefab : leafPrefab;
            LeafShot leaf = Instantiate(prefab, transform.position + (Vector3)(direction * 0.4f), Quaternion.identity);
            leaf.Launch(direction);
        }

        if (Input.GetMouseButton(0) && Time.time >= nextBomb && GameController.UseSpecial())
        {
            nextBomb = Time.time + bombCooldown * rate;
            Vector2 target = new Vector2(
                Mathf.Clamp(mouse.x, arenaMin.x, arenaMax.x),
                Mathf.Clamp(mouse.y, arenaMin.y, arenaMax.y));
            AntBomb bombPrefabToUse = power && goldBombPrefab != null ? goldBombPrefab : bombPrefab;
            AntBomb bomb = Instantiate(bombPrefabToUse, target, Quaternion.identity);
            bomb.Launch();
        }
    }

    public static int RowForAim(Vector2 aim)
    {
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        int sector = Mathf.RoundToInt(angle / 45f);
        sector = ((sector % 8) + 8) % 8;
        return RowBySector[sector];
    }

    public void TakeHit(int damage = 1)
    {
        // Dano 0 (ataque totalmente absorvido pelo poder da aliança): não tira engrenagem nem dá invulnerabilidade.
        if (damage <= 0 || Time.time < invulnerableUntil || GameController.gameOver) return;

        invulnerableUntil = Time.time + invulnerableSeconds;
        GameController.LoseGear(damage);
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        while (Time.time < invulnerableUntil)
        {
            if (sprite != null) sprite.enabled = !sprite.enabled;
            yield return new WaitForSeconds(0.08f);
        }
        if (sprite != null) sprite.enabled = true;
    }
}
