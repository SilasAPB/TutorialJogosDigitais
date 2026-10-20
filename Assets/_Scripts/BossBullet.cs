using UnityEngine;

// Projétil do boss. Tira uma engrenagem do herói e some ao bater na cerca, com um efeito de impacto.
[RequireComponent(typeof(Rigidbody2D))]
public class BossBullet : MonoBehaviour
{
    public float lifetime = 8f;
    [Tooltip("Pontos de dano na engrenagem: roxo 1, vermelho 2, verde 3.")]
    public int damage = 1;
    [Tooltip("Gira o desenho para a direção do tiro (balas alongadas). Desligue nas bolas.")]
    public bool rotateToDirection = true;
    public GameObject hitEffect;

    public static void ClearAll()
    {
        foreach (var bullet in FindObjectsByType<BossBullet>()) Destroy(bullet.gameObject);
    }

    public void Launch(Vector2 direction, float speed)
    {
        Vector2 dir = direction.normalized;
        if (rotateToDirection)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        GetComponent<Rigidbody2D>().linearVelocity = dir * speed;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hero = other.GetComponentInParent<HeroController>();
        if (hero != null)
        {
            hero.TakeHit(RingPower.ReduceBossDamage(damage));
            Impact();
        }
        else if (other.GetComponent<CompositeCollider2D>() != null)
        {
            Impact();
        }
    }

    void Impact()
    {
        if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
