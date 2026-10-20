using UnityEngine;

// Folha pontiaguda: sai em linha reta na direção do clique e estoura em folhinhas ao acertar.
[RequireComponent(typeof(Rigidbody2D))]
public class LeafShot : MonoBehaviour
{
    public float speed = 14f;
    public float damage = 2f;
    public float lifetime = 3f;
    public GameObject hitEffect;

    public static void ClearAll()
    {
        foreach (var leaf in FindObjectsByType<LeafShot>()) Destroy(leaf.gameObject);
    }

    public void Launch(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        GetComponent<Rigidbody2D>().linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var boss = other.GetComponentInParent<BossController>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
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
