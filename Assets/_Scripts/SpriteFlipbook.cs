using UnityEngine;

// Toca uma animação de sprites uma vez (efeito de corte de grama) e se destrói no fim.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 16f;
    public bool randomFlip = true;

    SpriteRenderer sr;
    float time;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (randomFlip) sr.flipX = Random.value < 0.5f;
        Show(0);
    }

    void Update()
    {
        time += Time.deltaTime;
        int index = (int)(time * fps);
        if (index >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }
        Show(index);
    }

    void Show(int index)
    {
        if (frames != null && frames.Length > 0) sr.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }
}
