using System.Collections.Generic;
using UnityEngine;

// Engrenagem solta na arena da fase do boss: o herói pega andando por cima.
// Também é gerada aqui (enquanto o jogador ainda pode usar uma engrenagem).
[RequireComponent(typeof(CircleCollider2D))]
public class GearPickup : MonoBehaviour
{
    static readonly List<GearPickup> Alive = new List<GearPickup>();

    public static int Count
    {
        get { return Alive.Count; }
    }

    public static void ClearAll()
    {
        for (int i = Alive.Count - 1; i >= 0; i--)
        {
            if (Alive[i] != null) Destroy(Alive[i].gameObject);
        }
        Alive.Clear();
    }

    public float bobAmount = 0.06f;
    public float bobSpeed = 3f;
    public float lifetime = 12f;

    Vector3 basePosition;
    float age;

    void OnEnable()
    {
        Alive.Add(this);
    }

    void OnDisable()
    {
        Alive.Remove(this);
    }

    void Start()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        // O tempo passa só durante a luta (some se ficar muito tempo sem ser pego).
        if (RoundManager.Instance == null || !RoundManager.Instance.Playing) return;

        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmount);

        // Pisca no final para avisar que vai sumir.
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = lifetime - age > 3f || Mathf.Repeat(age * 6f, 1f) > 0.5f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<HeroController>() == null) return;

        GameController.AddGear();
        Destroy(gameObject);
    }
}
