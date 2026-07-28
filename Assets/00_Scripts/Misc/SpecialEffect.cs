using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SpecialEffect : MonoBehaviour
{
    [SerializeField] private AudioClip[] SFX;
    [SerializeField] private float lifetime = 1f;
    private float timer = 0f;
    private Light2D light2d;
    private float lightIntensity;
    private float lightRadius;

    private void Awake()
    {
        light2d = GetComponentInChildren<Light2D>();

        if (light2d != null)
        {
            lightIntensity = light2d.intensity;
            lightRadius = light2d.pointLightOuterRadius;
        }

        AudioSourcePool.Play(SFX, 0.5f);
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
