using UnityEngine;

// Fase 1: o cortador anda sozinho, de célula em célula, como no Snake.
// Ao bater na cerca perde uma engrenagem e fica parado até o jogador apertar uma direção.
public class MowerController : MonoBehaviour
{
    public GrassManager grass;
    public float stepsPerSecond = 7f;

    [Header("Visual (folha de 5 colunas x 5 linhas)")]
    [Tooltip("Linhas: baixo, cima, esquerda, direita, parado. Colunas 0-3 = caminhada, 4 = batida. Na linha 'parado' as colunas são as 4 direções.")]
    public Sprite[] sheet;
    public float walkFramesPerCell = 2f;

    [Header("Chance de ganhar engrenagem ao cortar")]
    [Range(0f, 1f)] public float weedGearChance = 0.25f;
    [Range(0f, 1f)] public float anthillGearChance = 1f / 6f;

    public Vector3Int Cell { get; private set; }

    Vector2Int dir = Vector2Int.right;
    Vector2Int queued;
    bool hasQueued;
    bool idle;
    bool moving;
    Vector3Int target;
    Vector3 from;
    Vector3 to;
    float t;
    SpriteRenderer spriteRenderer;
    float animTime;
    float hitTime = -10f;

    const float HitPoseSeconds = 0.7f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void PlaceAt(Vector3 worldPosition)
    {
        Cell = grass.WorldToCell(worldPosition);
        transform.position = grass.CellCenter(Cell);
        moving = false;
        hasQueued = false;
        t = 0f;
        hitTime = -10f;
        // Toda rodada começa parada: o cortador só anda depois que o jogador aperta uma direção.
        idle = true;
        ApplyRotation();
        Collect(grass.Cut(Cell));
    }

    void Update()
    {
        if (RoundManager.Instance == null || !RoundManager.Instance.Playing) return;

        ReadInput();

        if (moving)
        {
            t += Time.deltaTime * stepsPerSecond;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t));
            if (t >= 1f) Arrive();
        }
        else if (idle)
        {
            // Parado depois de bater: qualquer direção livre põe o cortador de volta em movimento.
            if (hasQueued)
            {
                hasQueued = false;
                Vector3Int wanted = Cell + (Vector3Int)queued;
                if (!grass.IsBlocked(wanted) && !grass.IsWeedBlocking(wanted))
                {
                    dir = queued;
                    idle = false;
                    t = 0f;
                    BeginStep();
                }
            }
        }
        else
        {
            BeginStep();
        }
    }

    void ReadInput()
    {
        // Parado no começo da rodada, também vale a direção em que ele já aponta (qualquer tecla de direção).
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Queue(Vector2Int.up);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Queue(Vector2Int.down);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Queue(Vector2Int.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Queue(Vector2Int.right);
    }

    void Queue(Vector2Int direction)
    {
        queued = direction;
        hasQueued = true;
    }

    void Arrive()
    {
        t -= 1f;
        moving = false;
        Cell = target;
        transform.position = to;

        // Piscina: o cortador para de funcionar na hora.
        if (grass.IsPool(Cell))
        {
            GameController.Kill(DeathCause.MowerBroke);
            return;
        }

        Collect(grass.Cut(Cell));

        if (hasQueued)
        {
            dir = queued;
            hasQueued = false;
        }
        BeginStep();
    }

    void BeginStep()
    {
        target = Cell + (Vector3Int)dir;
        ApplyRotation();

        // Cerca, ou erva daninha com poucas engrenagens: bate e para.
        if (grass.IsBlocked(target) || grass.IsWeedBlocking(target))
        {
            Hit();
            return;
        }

        from = transform.position;
        to = grass.CellCenter(target);
        moving = true;
    }

    void Hit()
    {
        moving = false;
        idle = true;
        t = 0f;
        hitTime = Time.time;
        GameController.LoseGear();
    }

    void LateUpdate()
    {
        if (sheet == null || sheet.Length < 25 || spriteRenderer == null) return;

        int row = FacingRow();
        if (Time.time - hitTime < HitPoseSeconds)
        {
            spriteRenderer.sprite = sheet[row * 5 + 4];
        }
        else if (moving)
        {
            animTime += Time.deltaTime * stepsPerSecond * walkFramesPerCell;
            spriteRenderer.sprite = sheet[row * 5 + (int)animTime % 4];
        }
        else
        {
            spriteRenderer.sprite = sheet[4 * 5 + row];
        }
    }

    // Linhas da folha: 0 baixo, 1 cima, 2 esquerda, 3 direita.
    int FacingRow()
    {
        if (dir == Vector2Int.down) return 0;
        if (dir == Vector2Int.up) return 1;
        if (dir == Vector2Int.left) return 2;
        return 3;
    }

    void Collect(CutResult result)
    {
        switch (result)
        {
            case CutResult.Grass:
                GameController.flowerAmmo++;
                if (RingPower.Instance != null) RingPower.Instance.TryFindWhileCutting();
                break;
            case CutResult.Dense:
                GameController.flowerAmmo += 3;
                break;
            case CutResult.Anthill:
                GameController.specialAmmo++;
                if (Random.value < anthillGearChance) GameController.AddGear();
                break;
            case CutResult.Weed:
                GameController.flowerAmmo += 2;
                if (Random.value < weedGearChance) GameController.AddGear();
                break;
            case CutResult.Gear:
                GameController.AddGear();
                break;
            case CutResult.WeedBlocked:
                GameController.LoseGear();
                break;
        }
    }

    void ApplyRotation()
    {
        // Com a arte definitiva cada direção tem o seu desenho; sem ela, gira o sprite de teste.
        if (sheet != null && sheet.Length >= 25)
        {
            transform.rotation = Quaternion.identity;
            return;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
