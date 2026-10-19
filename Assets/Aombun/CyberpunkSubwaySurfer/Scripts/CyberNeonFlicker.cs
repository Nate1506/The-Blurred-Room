using UnityEngine;

namespace CyberpunkSubwaySurfer
{
    public class CyberNeonFlicker : MonoBehaviour
    {
        [Header("Flicker Settings")]
        public Renderer targetRenderer;
        public Light targetLight;
        public float flickerChance = 0.05f;
        public float minFlickerDuration = 0.04f;
        public float maxFlickerDuration = 0.12f;

        private Material matInstance;
        private Color originalEmissionColor;
        private float originalLightIntensity;
        private bool isFlickering = false;
        private float flickerTimer = 0f;

        private void Start()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                matInstance = targetRenderer.material;
                if (matInstance.HasProperty("_EmissionColor"))
                {
                    originalEmissionColor = matInstance.GetColor("_EmissionColor");
                }
            }

            if (targetLight == null) targetLight = GetComponent<Light>();
            if (targetLight != null)
            {
                originalLightIntensity = targetLight.intensity;
            }
        }

        private void Update()
        {
            if (isFlickering)
            {
                flickerTimer -= Time.deltaTime;
                if (flickerTimer <= 0f)
                {
                    // Restore original
                    RestoreState();
                    isFlickering = false;
                }
            }
            else
            {
                if (Random.value < flickerChance)
                {
                    StartFlicker();
                }
            }
        }

        private void StartFlicker()
        {
            isFlickering = true;
            flickerTimer = Random.Range(minFlickerDuration, maxFlickerDuration);

            float dimFactor = Random.Range(0.0f, 0.3f);
            if (matInstance != null && matInstance.HasProperty("_EmissionColor"))
            {
                matInstance.SetColor("_EmissionColor", originalEmissionColor * dimFactor);
            }
            if (targetLight != null)
            {
                targetLight.intensity = originalLightIntensity * dimFactor;
            }
        }

        private void RestoreState()
        {
            if (matInstance != null && matInstance.HasProperty("_EmissionColor"))
            {
                matInstance.SetColor("_EmissionColor", originalEmissionColor);
            }
            if (targetLight != null)
            {
                targetLight.intensity = originalLightIntensity;
            }
        }

        private void OnDestroy()
        {
            if (matInstance != null)
            {
                Destroy(matInstance);
            }
        }
    }
}
