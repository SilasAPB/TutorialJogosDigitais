using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum CutResult
{
    None,
    Grass,
    Anthill,
    Weed,
    WeedBlocked,
    Gear,
    Dense
}

// Cuida da grama no Tilemap: preenche o campo, deixa a grama cortar, faz nascer grama nova
// (e erva daninha, no estágio 2) e guarda a piscina do estágio 2.
public class GrassManager : MonoBehaviour
{
    public Tilemap grassMap;
    public Tilemap fenceMap;
    public Tilemap poolMap;
    public TileBase tallGrassTile;
    [Tooltip("Variações da grama alta; uma é sorteada a cada célula. Se vazio, usa só 'tallGrassTile'.")]
    public TileBase[] tallGrassVariants;
    [Tooltip("Mato denso (mais raro): dá 3 de munição ao ser cortado. Variações sorteadas.")]
    public TileBase[] denseVariants;
    [Range(0f, 1f)] public float denseChance = 0.10f;
    public TileBase sproutTile;
    [Tooltip("Estágios do broto (do menor para o maior) até virar grama alta. Se vazio, usa só 'sproutTile'.")]
    public TileBase[] sproutStages;
    public TileBase anthillTile;
    [Tooltip("Estágios de crescimento do formigueiro (do menor para o maior). Se vazio, usa só 'anthillTile'.")]
    public TileBase[] anthillStages;
    public float anthillGrowSeconds = 3f;
    public TileBase weedTile;
    [Tooltip("Variações da erva daninha; uma é sorteada a cada célula. Se vazio, usa só 'weedTile'.")]
    public TileBase[] weedVariants;
    public TileBase gearTile;
    public TileBase poolTile;
    [Tooltip("Águas da piscina (variações). Só a água mata; a borda é segura.")]
    public TileBase[] poolWater;
    [Tooltip("Borda da piscina, na ordem: cima, baixo, esquerda, direita, canto cima-esq, cima-dir, baixo-esq, baixo-dir.")]
    public TileBase[] poolEdges;
    public GameObject grassCutEffect;
    [Tooltip("Efeitos animados de corte; um é sorteado a cada corte. Se vazio, usa 'grassCutEffect'.")]
    public GameObject[] grassCutEffects;
    public GameObject anthillCutEffect;
    public MowerController mower;

    [Header("Área jogável (células, inclusive)")]
    public Vector2Int minCell = new Vector2Int(-10, -6);
    public Vector2Int maxCell = new Vector2Int(9, 5);

    [Header("Nascimento de grama")]
    public float spawnInterval = 0.5f;
    public float sproutTime = 1.5f;
    [Range(0f, 1f)] public float anthillChance = 0.05f;
    public int maxAnthills = 2;

    [Header("Engrenagem no campo (rara)")]
    public float gearSpawnInterval = 15f;
    public int maxGearsOnField = 2;

    [Header("Estágio atual (preenchido pelo RoundManager)")]
    [Range(0f, 1f)] public float weedChance;
    public int maxWeeds;

    public bool Active { get; set; }

    class Sprout
    {
        public float time;
        public int stage;
        public bool weed;
    }

    class AnthillGrowth
    {
        public int stage;
        public float next;
    }

    readonly Dictionary<Vector3Int, Sprout> sprouts = new Dictionary<Vector3Int, Sprout>();
    readonly Dictionary<Vector3Int, AnthillGrowth> anthills = new Dictionary<Vector3Int, AnthillGrowth>();
    readonly List<Vector3Int> ready = new List<Vector3Int>();
    float nextSpawn;
    float gearTimer;
    int anthillCount;
    int weedCount;
    int gearCount;

    public Vector3Int WorldToCell(Vector3 world)
    {
        return grassMap.WorldToCell(world);
    }

    public Vector3 CellCenter(Vector3Int cell)
    {
        return grassMap.GetCellCenterWorld(cell);
    }

    public Vector3 CellCenter(Vector2Int cell)
    {
        return CellCenter(new Vector3Int(cell.x, cell.y, 0));
    }

    public bool IsInside(Vector3Int cell)
    {
        return cell.x >= minCell.x && cell.x <= maxCell.x && cell.y >= minCell.y && cell.y <= maxCell.y;
    }

    // Cerca ou fora do campo: o cortador não passa.
    public bool IsBlocked(Vector3Int cell)
    {
        return !IsInside(cell) || fenceMap.HasTile(cell);
    }

    bool IsTall(TileBase tile)
    {
        if (tile == null) return false;
        if (tile == tallGrassTile) return true;
        return tallGrassVariants != null && System.Array.IndexOf(tallGrassVariants, tile) >= 0;
    }

    bool IsDense(TileBase tile)
    {
        return tile != null && denseVariants != null && System.Array.IndexOf(denseVariants, tile) >= 0;
    }

    // Grama que nasce: na maioria das vezes alta comum, de vez em quando mato denso.
    TileBase NewGrowth()
    {
        if (denseVariants != null && denseVariants.Length > 0 && Random.value < denseChance)
            return denseVariants[Random.Range(0, denseVariants.Length)];
        return NewTall();
    }

    TileBase NewTall()
    {
        if (tallGrassVariants != null && tallGrassVariants.Length > 0)
            return tallGrassVariants[Random.Range(0, tallGrassVariants.Length)];
        return tallGrassTile;
    }

    bool IsAnthill(TileBase tile)
    {
        if (tile == null) return false;
        if (tile == anthillTile) return true;
        return anthillStages != null && System.Array.IndexOf(anthillStages, tile) >= 0;
    }

    TileBase AnthillTile(int stage)
    {
        if (anthillStages != null && anthillStages.Length > 0)
            return anthillStages[Mathf.Clamp(stage, 0, anthillStages.Length - 1)];
        return anthillTile;
    }

    bool IsWeed(TileBase tile)
    {
        if (tile == null) return false;
        if (tile == weedTile) return true;
        return weedVariants != null && System.Array.IndexOf(weedVariants, tile) >= 0;
    }

    bool HasWeedTiles
    {
        get { return weedTile != null || (weedVariants != null && weedVariants.Length > 0); }
    }

    TileBase NewWeed()
    {
        if (weedVariants != null && weedVariants.Length > 0)
            return weedVariants[Random.Range(0, weedVariants.Length)];
        return weedTile;
    }

    // Qualquer célula da piscina (água ou borda): aí a grama não nasce.
    public bool IsPoolArea(Vector3Int cell)
    {
        return poolMap != null && poolMap.HasTile(cell);
    }

    // Só a água mata; a borda de pedra é segura.
    public bool IsPool(Vector3Int cell)
    {
        if (poolMap == null) return false;
        TileBase tile = poolMap.GetTile(cell);
        if (tile == null) return false;
        if (tile == poolTile) return true;
        return poolWater != null && System.Array.IndexOf(poolWater, tile) >= 0;
    }

    // Erva daninha machuca quem tem menos engrenagens do que o mínimo para cortá-la.
    public bool IsWeedBlocking(Vector3Int cell)
    {
        return IsWeed(grassMap.GetTile(cell)) && GameController.gears < GameController.WeedSafeGears;
    }

    public void ConfigureStage(StageConfig config)
    {
        spawnInterval = config.grassSpawnInterval;
        weedChance = config.weedChance;
        maxWeeds = config.maxWeeds;

        if (poolMap == null) return;

        poolMap.ClearAllTiles();
        if (!config.hasPool) return;

        RectInt area = config.poolArea;
        bool hasRim = poolEdges != null && poolEdges.Length >= 8 && poolWater != null && poolWater.Length > 0
                      && area.width >= 3 && area.height >= 3;

        for (int x = area.xMin; x < area.xMax; x++)
        {
            for (int y = area.yMin; y < area.yMax; y++)
            {
                TileBase tile = poolTile;
                if (hasRim)
                {
                    bool left = x == area.xMin, right = x == area.xMax - 1;
                    bool bottom = y == area.yMin, top = y == area.yMax - 1;
                    if (top && left) tile = poolEdges[4];
                    else if (top && right) tile = poolEdges[5];
                    else if (bottom && left) tile = poolEdges[6];
                    else if (bottom && right) tile = poolEdges[7];
                    else if (top) tile = poolEdges[0];
                    else if (bottom) tile = poolEdges[1];
                    else if (left) tile = poolEdges[2];
                    else if (right) tile = poolEdges[3];
                    else tile = poolWater[Random.Range(0, poolWater.Length)];
                }
                poolMap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    public void ResetField(float fill, Vector2Int avoidCenter, int initialWeeds)
    {
        grassMap.ClearAllTiles();
        sprouts.Clear();
        anthills.Clear();
        anthillCount = 0;
        weedCount = 0;
        gearCount = 0;
        gearTimer = 0f;

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (Mathf.Abs(x - avoidCenter.x) <= 2 && Mathf.Abs(y - avoidCenter.y) <= 2) continue;
                if (IsPoolArea(cell)) continue;
                if (Random.value < fill) grassMap.SetTile(cell, NewGrowth());
            }
        }

        for (int i = 0; i < initialWeeds; i++)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var cell = new Vector3Int(Random.Range(minCell.x, maxCell.x + 1), Random.Range(minCell.y, maxCell.y + 1), 0);
                if (Mathf.Abs(cell.x - avoidCenter.x) <= 3 && Mathf.Abs(cell.y - avoidCenter.y) <= 3) continue;
                if (!HasWeedTiles || IsPoolArea(cell) || IsWeed(grassMap.GetTile(cell))) continue;

                grassMap.SetTile(cell, NewWeed());
                weedCount++;
                break;
            }
        }
    }

    public CutResult Cut(Vector3Int cell)
    {
        TileBase tile = grassMap.GetTile(cell);
        if (tile == null) return CutResult.None;

        if (IsTall(tile))
        {
            grassMap.SetTile(cell, null);
            SpawnEffect(RandomGrassEffect(), cell);
            return CutResult.Grass;
        }

        if (IsDense(tile))
        {
            grassMap.SetTile(cell, null);
            SpawnEffect(RandomGrassEffect(), cell);
            return CutResult.Dense;
        }

        if (IsAnthill(tile))
        {
            grassMap.SetTile(cell, null);
            anthills.Remove(cell);
            anthillCount = Mathf.Max(0, anthillCount - 1);
            SpawnEffect(anthillCutEffect, cell);
            return CutResult.Anthill;
        }

        if (IsWeed(tile))
        {
            if (GameController.gears < GameController.WeedSafeGears) return CutResult.WeedBlocked;

            grassMap.SetTile(cell, null);
            weedCount = Mathf.Max(0, weedCount - 1);
            SpawnEffect(RandomGrassEffect(), cell);
            return CutResult.Weed;
        }

        if (gearTile != null && tile == gearTile)
        {
            grassMap.SetTile(cell, null);
            gearCount = Mathf.Max(0, gearCount - 1);
            return CutResult.Gear;
        }

        // Broto ainda não é cortável.
        return CutResult.None;
    }

    // 'sproutTime' é o tempo total até virar grama alta; cada estágio do broto leva uma fatia igual.
    float SproutStageTime
    {
        get
        {
            int stages = sproutStages != null ? sproutStages.Length : 0;
            return sproutTime / Mathf.Max(1, stages);
        }
    }

    TileBase FirstSprout()
    {
        if (sproutStages != null && sproutStages.Length > 0) return sproutStages[0];
        return sproutTile;
    }

    // Na fase do boss a grama vira só fundo: as engrenagens que sobraram no chão somem (lá elas nascem como itens soltos).
    public void RemoveGearTiles()
    {
        if (gearTile == null) return;

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (grassMap.GetTile(cell) == gearTile) grassMap.SetTile(cell, null);
            }
        }
        gearCount = 0;
    }

    GameObject RandomGrassEffect()
    {
        if (grassCutEffects != null && grassCutEffects.Length > 0)
            return grassCutEffects[Random.Range(0, grassCutEffects.Length)];
        return grassCutEffect;
    }

    void SpawnEffect(GameObject prefab, Vector3Int cell)
    {
        if (prefab != null) Instantiate(prefab, CellCenter(cell), Quaternion.identity);
    }

    void Update()
    {
        if (!Active || RoundManager.Instance == null || !RoundManager.Instance.Playing) return;

        ready.Clear();
        foreach (var pair in sprouts)
        {
            if (Time.time >= pair.Value.time) ready.Add(pair.Key);
        }
        foreach (var cell in ready)
        {
            Sprout sprout = sprouts[cell];
            int stages = sproutStages != null ? sproutStages.Length : 0;
            if (sprout.stage + 1 < stages)
            {
                // próximo estágio do broto
                sprout.stage++;
                sprout.time = Time.time + SproutStageTime;
                grassMap.SetTile(cell, sproutStages[sprout.stage]);
            }
            else
            {
                sprouts.Remove(cell);
                grassMap.SetTile(cell, sprout.weed ? NewWeed() : NewGrowth());
            }
        }

        // Formigueiro cresce por estágios com o tempo (continua cortável em qualquer estágio).
        ready.Clear();
        foreach (var pair in anthills)
        {
            if (Time.time >= pair.Value.next) ready.Add(pair.Key);
        }
        foreach (var cell in ready)
        {
            AnthillGrowth growth = anthills[cell];
            if (anthillStages == null || growth.stage >= anthillStages.Length - 1)
            {
                growth.next = float.MaxValue;
                continue;
            }
            growth.stage++;
            growth.next = Time.time + anthillGrowSeconds;
            grassMap.SetTile(cell, AnthillTile(growth.stage));
        }

        if (Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + spawnInterval;
            SpawnRandom();
        }

        // Engrenagem: rara, só nasce se o jogador ainda pode carregar mais.
        gearTimer += Time.deltaTime;
        if (gearTimer >= gearSpawnInterval)
        {
            gearTimer = 0f;
            if (gearTile != null && gearCount < maxGearsOnField && GameController.CanUseGear)
                SpawnGear();
        }
    }

    void SpawnGear()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            var cell = new Vector3Int(Random.Range(minCell.x, maxCell.x + 1), Random.Range(minCell.y, maxCell.y + 1), 0);
            if (grassMap.HasTile(cell) || IsBlocked(cell) || IsPoolArea(cell)) continue;
            if (mower != null && mower.gameObject.activeInHierarchy && mower.Cell == cell) continue;

            grassMap.SetTile(cell, gearTile);
            gearCount++;
            return;
        }
    }

    void SpawnRandom()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            var cell = new Vector3Int(Random.Range(minCell.x, maxCell.x + 1), Random.Range(minCell.y, maxCell.y + 1), 0);
            if (grassMap.HasTile(cell) || IsBlocked(cell) || IsPoolArea(cell)) continue;
            if (mower != null && mower.gameObject.activeInHierarchy && mower.Cell == cell) continue;

            if (anthillCount < maxAnthills && Random.value < anthillChance)
            {
                grassMap.SetTile(cell, AnthillTile(0));
                anthills[cell] = new AnthillGrowth { stage = 0, next = Time.time + anthillGrowSeconds };
                anthillCount++;
            }
            else
            {
                bool weed = HasWeedTiles && weedCount < maxWeeds && Random.value < weedChance;
                if (weed) weedCount++;

                grassMap.SetTile(cell, FirstSprout());
                sprouts[cell] = new Sprout { time = Time.time + SproutStageTime, stage = 0, weed = weed };
            }
            return;
        }
    }
}
