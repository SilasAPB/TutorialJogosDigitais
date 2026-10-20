using UnityEngine;

public enum DeathCause
{
    None,
    Hits,
    MowerBroke,
    Drowned
}

// Estado global da partida. Persiste entre as fases (a cena e o mapa são mantidos).
public static class GameController
{
    public const int MaxGears = 7;
    public const int StartGears = 5;

    // Com menos engrenagens que isso, o cortador não aguenta cortar erva daninha.
    public const int WeedSafeGears = 3;

    public static int gears;

    // Golpes que cada engrenagem aguenta antes de sumir (nova -> enferrujada -> quebrada). Ajustado pelo RoundManager.
    public static int hitsPerGear = 3;

    // Dano acumulado em cada engrenagem (0 = nova). Só a última engrenagem leva golpes.
    public static int[] gearDamage = new int[MaxGears];
    public static int flowerAmmo;
    public static int specialAmmo;
    public static int stage;
    public static int round;
    public static float bossHp;
    public static float bossMaxHp = 1f;
    public static float totalTime;
    // Aliança da esposa: só pode ser encontrada uma vez por partida; ringCount > 0 = ainda não usada.
    public static bool ringCollected;
    public static int ringCount;

    public static bool gameOver;
    public static bool won;

    // Pausa pelo painel de ajuda: congela corte, herói, boss e nascimento de grama (via RoundManager.Playing).
    public static bool paused;
    public static DeathCause deathCause;

    public static void Init()
    {
        gears = StartGears;
        gearDamage = new int[MaxGears];
        flowerAmmo = 0;
        specialAmmo = 0;
        stage = 0;
        round = 1;
        bossHp = 1f;
        bossMaxHp = 1f;
        totalTime = 0f;
        gameOver = false;
        won = false;
        paused = false;
        ringCollected = false;
        ringCount = 0;
        deathCause = DeathCause.None;
    }

    // Cada ponto de dano estraga a última engrenagem um estágio; quando passa do último, ela some.
    public static void LoseGear(int damage = 1)
    {
        for (int i = 0; i < damage; i++)
        {
            if (gameOver || gears <= 0) return;

            int last = Mathf.Clamp(gears - 1, 0, MaxGears - 1);
            gearDamage[last]++;
            if (gearDamage[last] >= Mathf.Max(1, hitsPerGear))
            {
                gearDamage[last] = 0;
                gears--;
                if (gears <= 0) Kill(DeathCause.Hits);
            }
        }
    }

    // Morte instantânea (piscina) ou fim por falta de engrenagens.
    public static void Kill(DeathCause cause)
    {
        if (gameOver) return;

        gears = 0;
        gearDamage = new int[MaxGears];
        won = false;
        deathCause = cause;
        gameOver = true;
    }

    // Ganhar engrenagem: se a última está desgastada, ela é consertada (volta a ser nova);
    // só quando todas estão novas é que entra uma engrenagem a mais.
    public static void AddGear()
    {
        if (gears > 0 && gearDamage[gears - 1] > 0)
        {
            gearDamage[gears - 1] = 0;
        }
        else if (gears < MaxGears)
        {
            gearDamage[gears] = 0;
            gears++;
        }
    }

    // Há algo a ganhar? (usado para decidir se vale a pena nascer uma engrenagem no campo)
    public static bool CanUseGear
    {
        get { return gears < MaxGears || (gears > 0 && gearDamage[gears - 1] > 0); }
    }

    public static bool UseFlower()
    {
        if (flowerAmmo <= 0) return false;
        flowerAmmo--;
        return true;
    }

    public static bool UseSpecial()
    {
        if (specialAmmo <= 0) return false;
        specialAmmo--;
        return true;
    }

    public static void DamageBoss(float amount)
    {
        bossHp = Mathf.Max(0f, bossHp - amount);
    }

    // Usado pelo PlayerMovement antigo do tutorial (não faz mais parte do jogo).
    public static void Collect() { }

    public static string FormatTime(float seconds)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }
}
