using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SpecialEffect : MonoBehaviour
{
    [SerializeField] private AudioClip[] SFX;
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float lifetime = 1f;
    private SpriteRenderer spriteRenderer;
    private Light2D light2d;
    private float lightIntensity;
    private float lightRadius;
    private float timer = 0f;

    private void Awake()
    {
        transform.localScale = new Vector3(Random.Range(0.9f, 1.1f), Random.Range(0.9f, 1.1f), 0);

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && Random.Range(0f, 1f) > 0.5f)
        {
            spriteRenderer.flipX = true;
        }

        light2d = GetComponentInChildren<Light2D>();

        if (light2d != null)
        {
            lightIntensity = light2d.intensity;
            lightRadius = light2d.pointLightOuterRadius;
        }

        AudioSourcePool.Play(SFX, volume);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (light2d != null)
        {
            float t = timer / lifetime;
            light2d.intensity = lightIntensity * (1f - t * t);
            light2d.pointLightOuterRadius = lightRadius * (1f - t * t);
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
