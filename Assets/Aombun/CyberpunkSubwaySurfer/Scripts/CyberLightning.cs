using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CyberpunkSubwaySurfer
{
    /// <summary>
    /// Procedural lightning system for the Cyberpunk Subway Surfer scene.
    /// Spawns dramatic lightning strikes between overhead cables and toward the ground,
    /// with randomized timing, branching bolts, and a brief scene flash effect.
    /// Attach this to any persistent GameObject (e.g., GameManager or TrackGenerator).
    /// </summary>
    public class CyberLightning : MonoBehaviour
    {
        // ─────────────── Timing ───────────────
        [Header("Strike Timing")]
        [Tooltip("Minimum seconds between lightning strikes.")]
        public float minInterval = 3f;
        [Tooltip("Maximum seconds between lightning strikes.")]
        public float maxInterval = 9f;

        // ─────────────── Geometry ───────────────
        [Header("Bolt Geometry")]
        [Tooltip("Number of main segment subdivisions per bolt.")]
        public int segmentCount = 16;
        [Tooltip("How far each segment can randomly deviate (meters).")]
        public float jitterAmount = 0.55f;
        [Tooltip("Maximum number of sub-branches per strike.")]
        public int maxBranches = 4;
        [Tooltip("Length of each branch relative to its host segment.")]
        [Range(0.2f, 0.8f)]
        public float branchLengthRatio = 0.45f;

        // ─────────────── Visuals ───────────────
        [Header("Bolt Visuals")]
        [Tooltip("Width of the main bolt (world units).")]
        public float boltWidth = 0.06f;
        [Tooltip("Width of branch bolts.")]
        public float branchWidth = 0.03f;
        [Tooltip("How long the bolt is visible (seconds).")]
        public float boltDuration = 0.12f;
        [Tooltip("Number of rapid visible/invisible flickers within the duration.")]
        public int flickerCount = 3;

        // ─────────────── Colours ───────────────
        [Header("Colours")]
        public Color boltColor     = new Color(0.75f, 0.88f, 1.00f);
        public Color boltEmission  = new Color(0.55f, 0.72f, 1.00f);
        [Tooltip("HDR multiplier for the emission glow.")]
        public float emissionIntensity = 6f;

        // ─────────────── Flash ───────────────
        [Header("Scene Flash")]
        [Tooltip("Add a brief ambient light flash when lightning strikes.")]
        public bool enableFlash = true;
        public float flashIntensity = 1.8f;
        public float flashDuration  = 0.08f;
        public Color flashColor     = new Color(0.80f, 0.90f, 1.00f);

        // ─────────────── Spawn Area ───────────────
        [Header("Strike Area (relative to this GameObject)")]
        [Tooltip("Lightning originates from this Y height above the track (overhead cables region).")]
        public float originYMin = 6.0f;
        public float originYMax = 9.0f;
        [Tooltip("Horizontal spread of the strike origin along X.")]
        public float originXRange = 5.5f;
        [Tooltip("How far ahead of the player the strike can land.")]
        public float strikeZMin = 10f;
        public float strikeZMax = 60f;
        [Tooltip("Ground level Y (bolt terminates here).")]
        public float groundY = 0.0f;

        // ─────────────── Internal ───────────────
        private float _nextStrikeTime;
        private Material _boltMat;
        private Light _flashLight;
        private Transform _playerTransform;

        private void Awake()
        {
            _boltMat = BuildBoltMaterial();
        }

        private void Start()
        {
            _nextStrikeTime = Time.time + Random.Range(minInterval * 0.5f, minInterval);

            // Find player for Z reference
            CyberPlayerController player = FindFirstObjectByType<CyberPlayerController>();
            if (player != null) _playerTransform = player.transform;

            // Create a flash light
            if (enableFlash)
            {
                GameObject flashGO = new GameObject("LightningFlashLight");
                flashGO.transform.SetParent(transform);
                flashGO.transform.localPosition = new Vector3(0f, 12f, 30f);
                _flashLight = flashGO.AddComponent<Light>();
                _flashLight.type      = LightType.Directional;
                _flashLight.color     = flashColor;
                _flashLight.intensity = 0f;
                _flashLight.shadows   = LightShadows.None;
            }
        }

        private void Update()
        {
            if (Time.time >= _nextStrikeTime)
            {
                TriggerStrike();
                _nextStrikeTime = Time.time + Random.Range(minInterval, maxInterval);
            }
        }

        // ─────────────── Public API ───────────────
        /// <summary>Immediately trigger a lightning strike (can be called externally).</summary>
        public void TriggerStrike()
        {
            float baseZ = (_playerTransform != null) ? _playerTransform.position.z : 0f;

            Vector3 origin = new Vector3(
                Random.Range(-originXRange, originXRange),
                Random.Range(originYMin, originYMax),
                baseZ + Random.Range(strikeZMin, strikeZMax)
            );

            Vector3 target = new Vector3(
                origin.x + Random.Range(-0.8f, 0.8f),
                groundY,
                origin.z + Random.Range(-0.5f, 0.5f)
            );

            StartCoroutine(StrikeRoutine(origin, target));
        }

        // ─────────────── Coroutine ───────────────
        private IEnumerator StrikeRoutine(Vector3 origin, Vector3 target)
        {
            // Build the main bolt points
            List<Vector3> points = BuildBoltPath(origin, target, jitterAmount, segmentCount);

            // Build branch paths
            List<(List<Vector3> pts, float width)> branches = new List<(List<Vector3>, float)>();
            int numBranches = Random.Range(1, maxBranches + 1);
            for (int b = 0; b < numBranches; b++)
            {
                int branchSeg = Random.Range(2, points.Count - 2);
                Vector3 branchStart = points[branchSeg];
                Vector3 branchDir   = (target - origin).normalized;
                // Deviate branch direction
                branchDir += new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.1f, -0.5f), Random.Range(-0.2f, 0.2f));
                branchDir.Normalize();
                float branchLen = Vector3.Distance(origin, target) * branchLengthRatio * Random.Range(0.5f, 1.0f);
                Vector3 branchEnd = branchStart + branchDir * branchLen;
                branches.Add((BuildBoltPath(branchStart, branchEnd, jitterAmount * 0.6f, segmentCount / 2), branchWidth));
            }

            // Spawn LineRenderers
            List<LineRenderer> renderers = new List<LineRenderer>();

            GameObject mainGO = CreateBoltRenderer("Lightning_Main", points, boltWidth);
            renderers.Add(mainGO.GetComponent<LineRenderer>());

            foreach (var (pts, w) in branches)
            {
                GameObject branchGO = CreateBoltRenderer("Lightning_Branch", pts, w);
                renderers.Add(branchGO.GetComponent<LineRenderer>());
            }

            // Flash loop
            float elapsed = 0f;
            float flickerStep = boltDuration / (flickerCount * 2f);
            bool visible = true;

            while (elapsed < boltDuration)
            {
                SetRenderersVisible(renderers, visible);
                visible = !visible;
                yield return new WaitForSeconds(flickerStep);
                elapsed += flickerStep;
            }

            SetRenderersVisible(renderers, false);

            // Destroy bolt GameObjects
            foreach (LineRenderer lr in renderers)
            {
                if (lr != null) Destroy(lr.gameObject);
            }

            // Scene flash
            if (enableFlash && _flashLight != null)
            {
                StartCoroutine(FlashRoutine());
            }
        }

        private IEnumerator FlashRoutine()
        {
            _flashLight.intensity = flashIntensity;
            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                _flashLight.intensity = Mathf.Lerp(flashIntensity, 0f, t / flashDuration);
                yield return null;
            }
            _flashLight.intensity = 0f;
        }

        // ─────────────── Helpers ───────────────
        private List<Vector3> BuildBoltPath(Vector3 start, Vector3 end, float jitter, int segments)
        {
            List<Vector3> pts = new List<Vector3>(segments + 1);
            pts.Add(start);
            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / segments;
                Vector3 straight = Vector3.Lerp(start, end, t);
                Vector3 offset   = new Vector3(
                    Random.Range(-jitter, jitter),
                    Random.Range(-jitter * 0.5f, jitter * 0.5f),
                    Random.Range(-jitter * 0.3f, jitter * 0.3f)
                );
                pts.Add(straight + offset);
            }
            pts.Add(end);
            return pts;
        }

        private GameObject CreateBoltRenderer(string name, List<Vector3> worldPoints, float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace   = true;
            lr.material        = _boltMat;
            lr.startWidth      = width;
            lr.endWidth        = width * 0.4f;
            lr.positionCount   = worldPoints.Count;
            lr.SetPositions(worldPoints.ToArray());
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            return go;
        }

        private void SetRenderersVisible(List<LineRenderer> renderers, bool visible)
        {
            foreach (LineRenderer lr in renderers)
            {
                if (lr != null) lr.enabled = visible;
            }
        }

        private Material BuildBoltMaterial()
        {
            // Try URP first, fall back to Standard
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Standard");

            Material mat = new Material(sh);
            mat.name = "LightningBoltMat";

            // Set base colour
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", boltColor);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     boltColor);

            // Enable emission
            mat.EnableKeyword("_EMISSION");
            Color emCol = boltEmission * emissionIntensity;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emCol);

            // Additive blending for glow effect
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite",   0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 10;

            return mat;
        }

        private void OnDestroy()
        {
            if (_boltMat != null) Destroy(_boltMat);
        }
    }
}
