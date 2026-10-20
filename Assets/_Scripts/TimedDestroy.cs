using UnityEngine;

// Destrói o objeto depois de alguns segundos (efeitos de partículas).
public class TimedDestroy : MonoBehaviour
{
    public float seconds = 1.5f;

    void Start()
    {
        Destroy(gameObject, seconds);
    }
}
