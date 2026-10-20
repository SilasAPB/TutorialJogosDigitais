using UnityEngine;

// Toca uma animação de sprites em loop (projéteis, jarro da bomba caindo).
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLoop : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 12f;
    public bool randomStart = true;

    SpriteRenderer sr;
    float time;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (randomStart && frames != null && frames.Length > 0) time = Random.value * frames.Length / Mathf.Max(1f, fps);
        Show();
    }

    void Update()
    {
        time += Time.deltaTime;
        Show();
    }

    void Show()
    {
        if (frames == null || frames.Length == 0) return;
        sr.sprite = frames[(int)(time * fps) % frames.Length];
    }
}
