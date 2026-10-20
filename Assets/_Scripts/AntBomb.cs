using UnityEngine;

// Bomba de formiga: cai exatamente no ponto clicado e explode em área.
public class AntBomb : MonoBehaviour
{
    public Transform body;
    public SpriteRenderer shadow;
    [Tooltip("Sombra pequena embaixo do jarro (cresce enquanto ele cai). Opcional.")]
    public Transform bodyShadow;
    public GameObject explosionPrefab;
    public float fallTime = 0.6f;
    public float fallHeight = 4f;
    public float radius = 1.4f;
    public float damage = 25f;

    float elapsed;

    public static void ClearAll()
    {
        foreach (var bomb in FindObjectsByType<AntBomb>()) Destroy(bomb.gameObject);
    }

    public void Launch()
    {
        elapsed = 0f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float k = Mathf.Clamp01(elapsed / fallTime);

        body.localPosition = Vector3.up * Mathf.Lerp(fallHeight, 0f, k);

        // A sombra cresce até cobrir exatamente a área da explosão.
        float fullScale = radius * 2f / shadow.sprite.bounds.size.x;
        shadow.transform.localScale = Vector3.one * (fullScale * Mathf.Lerp(0.3f, 1f, k));

        if (bodyShadow != null) bodyShadow.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, k);

        if (k >= 1f) Explode();
    }

    void Explode()
    {
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            var boss = hit.GetComponentInParent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
                break;
            }
        }

        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
