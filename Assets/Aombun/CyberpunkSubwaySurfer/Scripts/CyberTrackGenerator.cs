using UnityEngine;
using System.Collections.Generic;

namespace CyberpunkSubwaySurfer
{
    public class CyberTrackGenerator : MonoBehaviour
    {
        public const float LANE_DISTANCE = 2.8f;
        public const float CHUNK_LENGTH = 50f;
        public const int ACTIVE_CHUNKS_COUNT = 7;

        [Header("Material References")]
        public Material matTrackAsphalt;
        public Material matTrackGrid;
        public Material matNeonCyan;
        public Material matNeonMagenta;
        public Material matNeonYellow;
        public Material matLaserRed;
        public Material matHazard;
        public Material matSkyscraper;
        public Material matBillboardA;
        public Material matBillboardB;
        public Material matTrainHull;
        public Material matTrainWhite;
        public Material matTrainHeadlight;
        public Material matTrainSide;
        public Material matCoinGold;
        public Material matMetalDark;

        [Header("Realistic Architectural Materials (Non-Neon)")]
        public Material matBuildingConcrete;
        public Material matBuildingDarkSteel;
        public Material matBuildingGlass;
        public Material matBuildingWarmWindows;
        public Material matBuildingCoolWindows;
        public Material matRoadLine;

        [Header("Building Neon Accent Materials")]
        public Material matNeonHotPink;
        public Material matNeonOrange;
        public Material matNeonGreen;
        public Material matNeonPurple;
        public Material matNeonIce;
        public Material matNeonGold;
        public Material matNeonRed;
        public Material matNeonWhite;

        [Header("High-Altitude Holograms")]
        public Material matHologramGold;
        public Material matHologramCyan;
        public Material matHologramMagenta;

        [Header("Runtime State")]
        public Transform playerTransform;
        private List<GameObject> activeChunks = new List<GameObject>();
        private float spawnZ = 0f;
        private int chunkIndex = 0;

        private void Start()
        {
            LoadMaterialsIfNeeded();

            if (playerTransform == null)
            {
                CyberPlayerController player = FindFirstObjectByType<CyberPlayerController>();
                if (player != null) playerTransform = player.transform;
            }

            // Always clean up any stale pre-existing chunks to ensure the latest gameplay geometry is generated
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            activeChunks.Clear();
            spawnZ = 0f;
            chunkIndex = 0;

            for (int i = 0; i < ACTIVE_CHUNKS_COUNT; i++)
            {
                SpawnNextChunk(i < 2);
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            // When player moves forward, spawn new chunks and recycle old ones
            if (playerTransform.position.z + (CHUNK_LENGTH * 4) > spawnZ)
            {
                SpawnNextChunk(false);
                RecycleOldestChunk();
            }
        }

        public void LoadMaterialsIfNeeded()
        {
            if (matTrackAsphalt == null) matTrackAsphalt = Resources.Load<Material>("Cyber_Track_Asphalt") ?? GetMat("Cyber_Track_Asphalt");
            if (matTrackGrid == null) matTrackGrid = GetMat("Cyber_Track_Grid");
            if (matNeonCyan == null) matNeonCyan = GetMat("Cyber_Neon_Cyan");
            if (matNeonMagenta == null) matNeonMagenta = GetMat("Cyber_Neon_Magenta");
            if (matNeonYellow == null) matNeonYellow = GetMat("Cyber_Neon_Yellow");
            if (matLaserRed == null) matLaserRed = GetMat("Cyber_Laser_Red");
            if (matHazard == null) matHazard = GetMat("Cyber_Hazard");
            if (matSkyscraper == null) matSkyscraper = GetMat("Cyber_Skyscraper");
            if (matBillboardA == null) matBillboardA = GetMat("Cyber_Billboard_A");
            if (matBillboardB == null) matBillboardB = GetMat("Cyber_Billboard_B");
            if (matTrainHull == null) matTrainHull = GetMat("Cyber_Train_Hull");
            if (matTrainWhite == null) matTrainWhite = GetMat("Cyber_Train_White");
            if (matTrainHeadlight == null) matTrainHeadlight = GetMat("Cyber_Train_Headlight");
            if (matTrainSide == null) matTrainSide = GetMat("Cyber_Train_Side");
            if (matCoinGold == null) matCoinGold = GetMat("Cyber_Coin_Gold");
            if (matMetalDark == null) matMetalDark = GetMat("Cyber_Metal_Dark");

            // Realistic non-neon architecture
            if (matBuildingConcrete == null) matBuildingConcrete = CreateLitMaterial("BuildingConcrete", new Color(0.12f, 0.13f, 0.16f), Color.black, 0.1f, 0.25f);
            if (matBuildingDarkSteel == null) matBuildingDarkSteel = CreateLitMaterial("BuildingDarkSteel", new Color(0.16f, 0.18f, 0.22f), Color.black, 0.75f, 0.45f);
            if (matBuildingGlass == null) matBuildingGlass = CreateLitMaterial("BuildingGlass", new Color(0.06f, 0.08f, 0.12f), Color.black, 0.85f, 0.9f);
            if (matBuildingWarmWindows == null) matBuildingWarmWindows = CreateLitMaterial("BuildingWarmWin", new Color(0.12f, 0.12f, 0.15f), new Color(0.95f, 0.75f, 0.45f) * 0.35f, 0.2f, 0.3f);
            if (matBuildingCoolWindows == null) matBuildingCoolWindows = CreateLitMaterial("BuildingCoolWin", new Color(0.10f, 0.12f, 0.16f), new Color(0.35f, 0.65f, 0.95f) * 0.3f, 0.2f, 0.3f);
            if (matRoadLine == null) matRoadLine = CreateLitMaterial("RoadLine", new Color(0.72f, 0.74f, 0.78f), Color.black, 0.0f, 0.1f);

            // Vivid neon accent palette matching cyberpunk city vibe
            if (matNeonHotPink == null) matNeonHotPink = CreateNeonMaterial("NeonHotPink",  new Color(1.00f, 0.05f, 0.55f), 4.5f);
            if (matNeonOrange  == null) matNeonOrange  = CreateNeonMaterial("NeonOrange",   new Color(1.00f, 0.42f, 0.00f), 4.0f);
            if (matNeonGreen   == null) matNeonGreen   = CreateNeonMaterial("NeonGreen",    new Color(0.05f, 1.00f, 0.30f), 4.0f);
            if (matNeonPurple  == null) matNeonPurple  = CreateNeonMaterial("NeonPurple",   new Color(0.65f, 0.05f, 1.00f), 4.5f);
            if (matNeonIce     == null) matNeonIce     = CreateNeonMaterial("NeonIce",      new Color(0.05f, 0.90f, 1.00f), 4.5f);
            if (matNeonGold    == null) matNeonGold    = CreateNeonMaterial("NeonGold",     new Color(1.00f, 0.85f, 0.00f), 4.0f);
            if (matNeonRed     == null) matNeonRed     = CreateNeonMaterial("NeonRed",      new Color(1.00f, 0.10f, 0.08f), 4.5f);
            if (matNeonWhite   == null) matNeonWhite   = CreateNeonMaterial("NeonWhite",    new Color(0.92f, 0.95f, 1.00f), 3.5f);

            // High-altitude holograms
            if (matHologramGold == null) matHologramGold = CreateLitMaterial("HoloGold", new Color(1f, 0.82f, 0.2f, 0.85f), new Color(1f, 0.78f, 0.15f) * 2.4f, 0.1f, 0.9f, true);
            if (matHologramCyan == null) matHologramCyan = CreateLitMaterial("HoloCyan", new Color(0.1f, 0.85f, 1f, 0.85f), new Color(0f, 0.85f, 1f) * 2.4f, 0.1f, 0.9f, true);
            if (matHologramMagenta == null) matHologramMagenta = CreateLitMaterial("HoloMagenta", new Color(1f, 0.15f, 0.7f, 0.85f), new Color(1f, 0.1f, 0.6f) * 2.2f, 0.1f, 0.9f, true);
        }

        private Material CreateLitMaterial(string name, Color baseColor, Color emissionColor, float metallic = 0.5f, float smoothness = 0.5f, bool isTransparent = false)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material mat = new Material(sh);
            mat.name = name;
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emissionColor.maxColorComponent > 0.01f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }

            if (isTransparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return mat;
        }

        /// <summary>
        /// Creates a fully self-lit neon glow material with additive blending.
        /// intensity is an HDR multiplier applied to emissionColor.
        /// </summary>
        private Material CreateNeonMaterial(string name, Color col, float intensity = 4f)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material mat = new Material(sh);
            mat.name = name;
            Color dark = col * 0.12f;
            dark.a = 1f;
            mat.color = dark;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", dark);
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0.0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.95f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", col * intensity);
            return mat;
        }

        /// <summary>
        /// Returns one of 8 neon accent materials chosen by an integer key.
        /// </summary>
        private Material PickNeonMat(int key)
        {
            switch (Mathf.Abs(key) % 8)
            {
                case 0: return matNeonHotPink;
                case 1: return matNeonOrange;
                case 2: return matNeonIce;
                case 3: return matNeonGreen;
                case 4: return matNeonPurple;
                case 5: return matNeonGold;
                case 6: return matNeonRed;
                default: return matNeonWhite;
            }
        }

        /// <summary>
        /// Adds cyberpunk neon accents to a building:
        /// - Glowing floor-level base band
        /// - Horizontal neon floor strips every N meters up the facade
        /// - 2 vertical neon edge columns on the road-facing side
        /// - Crown glow trim on rooftop
        /// - Optional large neon billboard panel
        /// </summary>
        private void AddNeonAccents(Transform parent, float w, float h, float d,
                                    int seed, bool isLeft, System.Random rnd)
        {
            Material neonA = PickNeonMat(seed);
            Material neonB = PickNeonMat(seed + 3);

            float faceX = isLeft ? (w * 0.52f) : -(w * 0.52f);  // road-facing X
            float faceZ = 0f;

            // ── 1. Floor-level base neon band (wraps whole building base) ──
            // Front band (road-facing)
            CreateDecorationCube("NeonBase_Front", parent,
                new Vector3(faceX, 0.22f, faceZ),
                new Vector3(0.08f, 0.44f, d * 0.95f), neonA);
            // Top cap of base band
            CreateDecorationCube("NeonBase_Cap", parent,
                new Vector3(faceX, 0.46f, faceZ),
                new Vector3(0.12f, 0.08f, d * 0.95f), neonB);

            // ── 2. Horizontal neon floor bands every ~8m ──
            float bandStep = Mathf.Clamp(h / 5f, 6f, 12f);
            int bandCount = Mathf.Max(2, Mathf.FloorToInt(h / bandStep));
            for (int i = 1; i <= bandCount; i++)
            {
                float bandY = i * bandStep;
                if (bandY >= h - 2f) break;
                Material bMat = (i % 2 == 0) ? neonB : neonA;
                // Road-facing horizontal strip
                CreateDecorationCube($"NeonFloorBand_{i}", parent,
                    new Vector3(faceX, bandY, faceZ),
                    new Vector3(0.06f, 0.14f, d * 0.88f), bMat);
            }

            // ── 3. Vertical neon edge columns on road-facing facade ──
            float col1Z =  d * 0.36f;
            float col2Z = -d * 0.36f;
            CreateDecorationCube("NeonCol_1", parent,
                new Vector3(faceX, h * 0.5f, col1Z),
                new Vector3(0.10f, h * 0.85f, 0.10f), neonA);
            CreateDecorationCube("NeonCol_2", parent,
                new Vector3(faceX, h * 0.5f, col2Z),
                new Vector3(0.10f, h * 0.85f, 0.10f), neonB);

            // ── 4. Crown rooftop neon trim ring ──
            // Front edge of roof
            CreateDecorationCube("NeonCrown_Front", parent,
                new Vector3(faceX, h + 0.5f, faceZ),
                new Vector3(0.10f, 0.20f, d * 0.95f), neonA);
            // Left & right roof edges
            CreateDecorationCube("NeonCrown_L", parent,
                new Vector3(0f, h + 0.5f, d * 0.48f),
                new Vector3(w * 0.85f, 0.16f, 0.10f), neonB);
            CreateDecorationCube("NeonCrown_R", parent,
                new Vector3(0f, h + 0.5f, -d * 0.48f),
                new Vector3(w * 0.85f, 0.16f, 0.10f), neonB);

            // ── 5. Large neon billboard panel on upper facade ──
            if (rnd.NextDouble() > 0.30)
            {
                float billH = Mathf.Clamp(h * 0.18f, 4f, 10f);
                float billW = Mathf.Clamp(d * 0.55f, 5f, 15f);
                float billY = Mathf.Clamp(h * 0.45f, 6f, h - billH - 2f);
                Material billMat = PickNeonMat(seed + 5);
                GameObject bill = CreateDecorationCube("NeonBillboard", parent,
                    new Vector3(faceX, billY, faceZ),
                    new Vector3(0.12f, billH, billW), billMat);
                bill.AddComponent<CyberNeonFlicker>();
            }

            // ── 6. Extra mid-height accent sign box ──
            if (rnd.NextDouble() > 0.5)
            {
                float signY = h * 0.28f;
                CreateDecorationCube("NeonSign_Mid", parent,
                    new Vector3(faceX, signY, faceZ + (float)(rnd.NextDouble() * d * 0.2 - d * 0.1)),
                    new Vector3(0.12f, 1.8f, Mathf.Clamp(d * 0.3f, 3f, 7f)), PickNeonMat(seed + 7));
            }
        }

        private Material GetMat(string name)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>($"Assets/CyberpunkSubwaySurfer/Materials/{name}.mat");
#else
            return null;
#endif
        }

        public GameObject SpawnNextChunk(bool isSafeRunway)
        {
            LoadMaterialsIfNeeded();

            GameObject chunk = new GameObject($"TrackChunk_{chunkIndex:D3}");
            chunk.transform.SetParent(transform);
            chunk.transform.position = new Vector3(0f, 0f, spawnZ);

            // 1. Build Base Highway & Glowing Neon Rails
            BuildTrackBed(chunk);

            // 2. Build Side Crash Barriers & Overhead Gantries
            BuildPerimeterAndGantries(chunk);

            // 3. Build Cyberpunk City Backdrop (Skyscrapers & Billboards)
            BuildCityScenery(chunk);

            // 4. Build Gameplay Obstacles, Trains, and Coins
            if (!isSafeRunway)
            {
                BuildGameplayElements(chunk, chunkIndex);
            }
            else
            {
                // Safe runway: Place nice welcome coin lanes
                SpawnCoinRow(chunk, 0, 10f, 35f, 5, CoinType.Yellow);
                SpawnCoinRow(chunk, 1, 10f, 35f, 5, CoinType.Red);
                SpawnCoinRow(chunk, 2, 10f, 35f, 5, CoinType.Blue);
            }

            activeChunks.Add(chunk);
            spawnZ += CHUNK_LENGTH;
            chunkIndex++;
            return chunk;
        }

        private void RecycleOldestChunk()
        {
            if (activeChunks.Count > ACTIVE_CHUNKS_COUNT)
            {
                GameObject oldest = activeChunks[0];
                activeChunks.RemoveAt(0);
                Destroy(oldest);
            }
        }

        public void ResetGenerator()
        {
            foreach (GameObject chunk in activeChunks)
            {
                if (chunk != null) Destroy(chunk);
            }
            activeChunks.Clear();
            spawnZ = 0f;
            chunkIndex = 0;

            for (int i = 0; i < ACTIVE_CHUNKS_COUNT; i++)
            {
                SpawnNextChunk(i < 2);
            }
        }

        // ==========================================
        // 1. TRACK BED & RAILS (NON-NEON, REALISTIC HIGHWAY)
        // ==========================================
        private void BuildTrackBed(GameObject chunk)
        {
            // Main Road Surface (Length 50, Width 10.6, Height 0.4) - Dark asphalt pavement
            Material roadMat = (matTrackAsphalt != null) ? matTrackAsphalt : matMetalDark;
            GameObject road = CreateCube("RoadBed", chunk.transform, new Vector3(0f, -0.2f, CHUNK_LENGTH * 0.5f), new Vector3(10.6f, 0.4f, CHUNK_LENGTH), roadMat);
            // Ensure RoadBed has a solid BoxCollider for the player CharacterController to walk on!
            BoxCollider roadCol = road.GetComponent<BoxCollider>();
            if (roadCol == null) roadCol = road.AddComponent<BoxCollider>();

            // Dark Structural Lower Foundation (also keep solid collider as an extra safety catch)
            CreateCube("RoadFoundation", chunk.transform, new Vector3(0f, -2.5f, CHUNK_LENGTH * 0.5f), new Vector3(12.0f, 4.2f, CHUNK_LENGTH), matMetalDark);

            // Subtle Dashed White Lane Divider Lines (cosmetic only, no colliders)
            // Dividing Lane 0 from Lane 1 (at X = -1.4f) and Lane 1 from Lane 2 (at X = +1.4f)
            for (int dash = 0; dash < 8; dash++)
            {
                float z = dash * 6.25f + 3.1f;
                CreateDecorationCube($"LaneDash_L_{dash}", chunk.transform, new Vector3(-1.4f, 0.02f, z), new Vector3(0.12f, 0.02f, 3.2f), matRoadLine);
                CreateDecorationCube($"LaneDash_R_{dash}", chunk.transform, new Vector3(1.4f, 0.02f, z), new Vector3(0.12f, 0.02f, 3.2f), matRoadLine);
            }

            // Dark Concrete / Steel Curbs (solid colliders)
            CreateCube("Curb_Left", chunk.transform, new Vector3(-5.2f, 0.08f, CHUNK_LENGTH * 0.5f), new Vector3(0.24f, 0.16f, CHUNK_LENGTH), matMetalDark);
            CreateCube("Curb_Right", chunk.transform, new Vector3(5.2f, 0.08f, CHUNK_LENGTH * 0.5f), new Vector3(0.24f, 0.16f, CHUNK_LENGTH), matMetalDark);
        }

        // ==========================================
        // 2. CRASH BARRIERS & OVERHEAD GANTRIES (NON-NEON INDUSTRIAL)
        // ==========================================
        private void BuildPerimeterAndGantries(GameObject chunk)
        {
            // Left Crash Barrier: Solid Metal Body keeps player inside the track
            CreateCube("BarrierBody_L", chunk.transform, new Vector3(-5.4f, 0.5f, CHUNK_LENGTH * 0.5f), new Vector3(0.35f, 1.0f, CHUNK_LENGTH), matMetalDark);
            CreateDecorationCube("HazardBand_L", chunk.transform, new Vector3(-5.22f, 0.4f, CHUNK_LENGTH * 0.5f), new Vector3(0.02f, 0.45f, CHUNK_LENGTH), matHazard);
            CreateDecorationCube("CapRail_L", chunk.transform, new Vector3(-5.4f, 1.03f, CHUNK_LENGTH * 0.5f), new Vector3(0.14f, 0.06f, CHUNK_LENGTH), matMetalDark);

            // Right Crash Barrier: Solid Metal Body keeps player inside the track
            CreateCube("BarrierBody_R", chunk.transform, new Vector3(5.4f, 0.5f, CHUNK_LENGTH * 0.5f), new Vector3(0.35f, 1.0f, CHUNK_LENGTH), matMetalDark);
            CreateDecorationCube("HazardBand_R", chunk.transform, new Vector3(5.22f, 0.4f, CHUNK_LENGTH * 0.5f), new Vector3(0.02f, 0.45f, CHUNK_LENGTH), matHazard);
            CreateDecorationCube("CapRail_R", chunk.transform, new Vector3(5.4f, 1.03f, CHUNK_LENGTH * 0.5f), new Vector3(0.14f, 0.06f, CHUNK_LENGTH), matMetalDark);

            // Overhead Industrial Steel Gantry (placed in the middle of each chunk at Z = 25)
            GameObject gantry = new GameObject("OverheadGantry");
            gantry.transform.SetParent(chunk.transform);
            gantry.transform.localPosition = new Vector3(0f, 0f, 25f);

            // Dark Steel Pillars
            CreateDecorationCube("GantryPillar_L", gantry.transform, new Vector3(-5.5f, 3.2f, 0f), new Vector3(0.5f, 6.4f, 0.5f), matMetalDark);
            CreateDecorationCube("GantryPillar_R", gantry.transform, new Vector3(5.5f, 3.2f, 0f), new Vector3(0.5f, 6.4f, 0.5f), matMetalDark);

            // Steel Crossbeam (dark industrial framework, no neon trim)
            CreateDecorationCube("GantryCrossbeam", gantry.transform, new Vector3(0f, 6.2f, 0f), new Vector3(11.6f, 0.6f, 0.8f), matMetalDark);

            // Compact Highway Sign with subtle backlit graphic
            GameObject sign = CreateDecorationCube("GantrySign", gantry.transform, new Vector3(0f, 5.65f, 0f), new Vector3(1.8f, 0.65f, 0.1f), matBillboardB);
            sign.AddComponent<CyberNeonFlicker>();

            // Realistic heavy dark utility conduits / power cables
            CreateDecorationCube("Cable_L", chunk.transform, new Vector3(-4.8f, 6.8f, CHUNK_LENGTH * 0.5f), new Vector3(0.06f, 0.06f, CHUNK_LENGTH), matMetalDark);
            CreateDecorationCube("Cable_R", chunk.transform, new Vector3(4.8f, 6.8f, CHUNK_LENGTH * 0.5f), new Vector3(0.06f, 0.06f, CHUNK_LENGTH), matMetalDark);
        }

        // ==========================================
        // 3. CYBERPUNK CITY CANYON: UNIQUE SKYSCRAPERS & HIGH-ALTITUDE HOLOGRAMS
        // ==========================================
        private void BuildCityScenery(GameObject chunk)
        {
            int baseSeed = chunk.GetInstanceID() + chunkIndex * 137;
            System.Random rnd = new System.Random(baseSeed);

            // ── NEAR ROW: Left Side — 3 foreground buildings ──
            float currentZLeft = 1f;
            int bldgCountL = 3;
            for (int i = 0; i < bldgCountL; i++)
            {
                float z      = currentZLeft + (float)(rnd.NextDouble() * 2.0 + 6.0);
                float width  = (float)(rnd.NextDouble() * 8.0 + 14.0);
                float depth  = (float)(rnd.NextDouble() * 8.0 + 16.0);
                float height = (float)(rnd.NextDouble() * 80.0 + 55.0); // 55–135 m
                float x      = -(width * 0.5f + 7.8f);

                SpawnUniqueBuilding(chunk.transform, new Vector3(x, 0f, z), width, height, depth, baseSeed + i * 47, true);
                currentZLeft = z + depth * 0.5f;
            }

            // ── NEAR ROW: Right Side — 3 foreground buildings ──
            float currentZRight = 1f;
            int bldgCountR = 3;
            for (int i = 0; i < bldgCountR; i++)
            {
                float z      = currentZRight + (float)(rnd.NextDouble() * 2.0 + 6.0);
                float width  = (float)(rnd.NextDouble() * 8.0 + 14.0);
                float depth  = (float)(rnd.NextDouble() * 8.0 + 16.0);
                float height = (float)(rnd.NextDouble() * 80.0 + 55.0);
                float x      =  (width * 0.5f + 7.8f);

                SpawnUniqueBuilding(chunk.transform, new Vector3(x, 0f, z), width, height, depth, baseSeed + i * 59 + 83, false);
                currentZRight = z + depth * 0.5f;
            }

            // ── FAR BACKGROUND: Mega-tall sky-filling slabs — left & right ──
            BuildBackgroundSkyFill(chunk.transform, baseSeed, true,  rnd);
            BuildBackgroundSkyFill(chunk.transform, baseSeed, false, rnd);

            // ── SKY-BRIDGE SPANS crossing the corridor at high altitude ──
            BuildSkyBridgeSpans(chunk.transform, baseSeed, rnd);

            // ── High-Altitude Holographic Decorations ──
            SpawnHighAltitudeHologram(chunk.transform, chunkIndex);
        }

        /// <summary>
        /// Spawns 2-3 ultra-tall background megabuilding slabs far behind near buildings.
        /// 140-220 m tall — these fill the visible sky gap above the foreground skyline.
        /// </summary>
        private void BuildBackgroundSkyFill(Transform parent, int baseSeed, bool isLeft, System.Random rnd)
        {
            int count = 2 + (int)(rnd.NextDouble() * 2); // 2-3 slabs
            float zCursor = 4f;

            for (int i = 0; i < count; i++)
            {
                float z     = zCursor + (float)(rnd.NextDouble() * 5.0 + 4.0);
                float width = (float)(rnd.NextDouble() * 14.0 + 20.0);   // 20-34 m wide
                float depth = (float)(rnd.NextDouble() * 12.0 + 18.0);   // 18-30 m deep
                float h     = (float)(rnd.NextDouble() * 80.0 + 140.0);  // 140-220 m TALL

                float lateralOffset = width * 0.5f + (float)(rnd.NextDouble() * 10.0 + 26.0);
                float x = isLeft ? -lateralOffset : lateralOffset;

                int seed = baseSeed + (isLeft ? 1000 : 2000) + i * 73;

                Material bgMat = (seed % 3 == 0) ? matBuildingConcrete
                               : (seed % 3 == 1) ? matBuildingDarkSteel
                               : matBuildingGlass;

                // Main slab body (no collider needed)
                CreateDecorationCube($"BgSlab_{(isLeft ? "L" : "R")}_{i}", parent,
                    new Vector3(x, h * 0.5f, z),
                    new Vector3(width, h, depth), bgMat);

                Material nA = PickNeonMat(seed);
                Material nB = PickNeonMat(seed + 4);

                // Crown glow top
                CreateDecorationCube($"BgCrown_{i}", parent,
                    new Vector3(x, h + 0.4f, z),
                    new Vector3(width * 0.95f, 0.5f, depth * 0.95f), nA);

                // Vertical neon edge stripe on road-facing side
                float faceEdge = isLeft ? (x + width * 0.5f) : (x - width * 0.5f);
                CreateDecorationCube($"BgNeonStripe_{i}_A", parent,
                    new Vector3(faceEdge, h * 0.5f, z - depth * 0.3f),
                    new Vector3(0.18f, h * 0.9f, 0.18f), nA);
                CreateDecorationCube($"BgNeonStripe_{i}_B", parent,
                    new Vector3(faceEdge, h * 0.5f, z + depth * 0.3f),
                    new Vector3(0.18f, h * 0.9f, 0.18f), nB);

                // Floor-level base glow band
                CreateDecorationCube($"BgFloorBand_{i}", parent,
                    new Vector3(faceEdge, 0.3f, z),
                    new Vector3(0.14f, 0.5f, depth * 0.9f), nB);

                // Mid-height horizontal band
                float midY = h * (float)(rnd.NextDouble() * 0.2 + 0.3);
                CreateDecorationCube($"BgMidBand_{i}", parent,
                    new Vector3(faceEdge, midY, z),
                    new Vector3(0.10f, 0.22f, depth * 0.85f), nA);

                zCursor = z + depth * 0.5f;
            }
        }

        /// <summary>
        /// Spawns 1-2 large horizontal slab sky-bridges across the corridor at 38-58 m altitude.
        /// They partially block the open sky overhead, completing the enclosed canyon effect.
        /// Neon trim glows on all four underside edges.
        /// </summary>
        private void BuildSkyBridgeSpans(Transform parent, int baseSeed, System.Random rnd)
        {
            int count = 1 + (int)(rnd.NextDouble() * 2); // 1-2 bridges
            for (int i = 0; i < count; i++)
            {
                float z         = (float)(rnd.NextDouble() * 30.0 + 10.0);
                float bridgeY   = (float)(rnd.NextDouble() * 20.0 + 38.0); // 38-58 m up
                float spanW     = (float)(rnd.NextDouble() * 10.0 + 30.0); // 30-40 m wide
                float thickness = (float)(rnd.NextDouble() * 2.0  + 1.5f);
                float depth     = (float)(rnd.NextDouble() * 5.0  + 5.0);

                int seed = baseSeed + 9000 + i * 113;

                CreateDecorationCube($"SkyBridge_{i}_Deck", parent,
                    new Vector3(0f, bridgeY, z),
                    new Vector3(spanW, thickness, depth), matBuildingDarkSteel);

                Material nA = PickNeonMat(seed);
                Material nB = PickNeonMat(seed + 2);
                float underY = bridgeY - thickness * 0.5f - 0.07f;

                CreateDecorationCube($"SkyBridge_{i}_NeonL", parent,
                    new Vector3(-spanW * 0.5f, underY, z),
                    new Vector3(0.14f, 0.14f, depth * 0.95f), nA);
                CreateDecorationCube($"SkyBridge_{i}_NeonR", parent,
                    new Vector3( spanW * 0.5f, underY, z),
                    new Vector3(0.14f, 0.14f, depth * 0.95f), nB);
                CreateDecorationCube($"SkyBridge_{i}_NeonFront", parent,
                    new Vector3(0f, underY, z - depth * 0.5f),
                    new Vector3(spanW * 0.95f, 0.14f, 0.14f), nA);
                CreateDecorationCube($"SkyBridge_{i}_NeonBack", parent,
                    new Vector3(0f, underY, z + depth * 0.5f),
                    new Vector3(spanW * 0.95f, 0.14f, 0.14f), nB);
            }
        }


        /// <summary>
        /// Generates a completely unique skyscraper based on 6 architectural archetypes using realistic non-neon materials.
        /// </summary>
        private void SpawnUniqueBuilding(Transform parent, Vector3 basePos, float width, float totalHeight, float depth, int seed, bool isLeft)
        {
            GameObject root = new GameObject($"Building_{seed}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = basePos;

            int archetype = Mathf.Abs(seed) % 6;
            System.Random rnd = new System.Random(seed);

            // Architectural Material Palette (Realistic dark tones)
            Material primaryMat = (seed % 3 == 0) ? matBuildingConcrete : (seed % 3 == 1) ? matBuildingDarkSteel : matBuildingGlass;
            Material secondaryMat = (seed % 2 == 0) ? matBuildingDarkSteel : matMetalDark;
            Material windowMat = (seed % 2 == 0) ? matBuildingWarmWindows : matBuildingCoolWindows;

            switch (archetype)
            {
                case 0:
                    BuildSteppedTower(root.transform, width, totalHeight, depth, primaryMat, secondaryMat, windowMat, rnd, isLeft);
                    break;
                case 1:
                    BuildSlantedSpireTower(root.transform, width, totalHeight, depth, primaryMat, secondaryMat, rnd, isLeft);
                    break;
                case 2:
                    BuildBrutalistTower(root.transform, width, totalHeight, depth, primaryMat, secondaryMat, rnd, isLeft);
                    break;
                case 3:
                    BuildTwinSkybridgeTower(root.transform, width, totalHeight, depth, primaryMat, secondaryMat, rnd, isLeft);
                    break;
                case 4:
                    BuildHabitationBlock(root.transform, width, totalHeight, depth, primaryMat, windowMat, rnd, isLeft);
                    break;
                case 5:
                    BuildMonolithFinTower(root.transform, width, totalHeight, depth, primaryMat, secondaryMat, rnd, isLeft);
                    break;
            }

            // ── Cyberpunk neon accents on EVERY building ──
            AddNeonAccents(root.transform, width, totalHeight, depth, seed, isLeft, rnd);
        }

        // Archetype 0: Stepped / Tiered Ziggurat Megatower
        private void BuildSteppedTower(Transform parent, float w, float h, float d, Material matMain, Material matTrim, Material matWin, System.Random rnd, bool isLeft)
        {
            float h1 = h * 0.45f;
            float h2 = h * 0.35f;
            float h3 = h * 0.20f;

            // Base Tier
            CreateDecorationCube("Tier1_Base", parent, new Vector3(0f, h1 * 0.5f, 0f), new Vector3(w, h1, d), matMain);
            // Window band along base
            CreateDecorationCube("Tier1_Windows", parent, new Vector3(0f, h1 * 0.5f, 0f), new Vector3(w + 0.08f, h1 * 0.7f, d * 0.92f), matWin);

            // Mid Tier Setback
            CreateDecorationCube("Tier2_Mid", parent, new Vector3(0f, h1 + h2 * 0.5f, 0f), new Vector3(w * 0.75f, h2, d * 0.75f), matTrim);
            CreateDecorationCube("Tier2_Windows", parent, new Vector3(0f, h1 + h2 * 0.5f, 0f), new Vector3(w * 0.76f, h2 * 0.65f, d * 0.65f), matWin);

            // Crown Spire Tier
            CreateDecorationCube("Tier3_Top", parent, new Vector3(0f, h1 + h2 + h3 * 0.5f, 0f), new Vector3(w * 0.45f, h3, d * 0.45f), matMain);

            // Rooftop Communications Mast
            CreateDecorationCube("RoofMast", parent, new Vector3(0f, h + 3.5f, 0f), new Vector3(0.5f, 7.0f, 0.5f), matMetalDark);
            CreateDecorationCube("RoofAntennaRing", parent, new Vector3(0f, h + 5.0f, 0f), new Vector3(1.6f, 0.2f, 1.6f), matMetalDark);

            // High-Altitude Commercial Billboard mounted on upper facade (if facing road)
            if (rnd.NextDouble() > 0.4)
            {
                float faceX = isLeft ? (w * 0.37f) : -(w * 0.37f);
                GameObject ad = CreateDecorationCube("UpperBillboard", parent, new Vector3(faceX, h1 + h2 * 0.5f, 0f), new Vector3(0.1f, 6.5f, 12f), matBillboardA);
                ad.AddComponent<CyberNeonFlicker>();
            }
        }

        // Archetype 1: Angular Slanted Spire Skyscraper
        private void BuildSlantedSpireTower(Transform parent, float w, float h, float d, Material matMain, Material matTrim, System.Random rnd, bool isLeft)
        {
            // Main Tower Body
            CreateDecorationCube("MainTower", parent, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), matMain);

            // Angled Crown Wedge
            GameObject crown = CreateDecorationCube("SlantedCrown", parent, new Vector3(0f, h + 2.5f, 0f), new Vector3(w * 0.85f, 5.0f, d * 0.85f), matTrim);
            crown.transform.localRotation = Quaternion.Euler(isLeft ? 20f : -20f, 0f, 0f);

            // Exterior Vertical Elevator / Utility Spine
            float spineX = isLeft ? (w * 0.5f - 0.5f) : -(w * 0.5f - 0.5f);
            CreateDecorationCube("ElevatorSpine", parent, new Vector3(spineX, h * 0.5f, 0f), new Vector3(1.8f, h + 4.0f, 2.5f), matTrim);

            // Twin Communications Spires on Crown
            CreateDecorationCube("Spire_1", parent, new Vector3(w * 0.2f, h + 7.0f, 0f), new Vector3(0.35f, 9.0f, 0.35f), matMetalDark);
            CreateDecorationCube("Spire_2", parent, new Vector3(-w * 0.2f, h + 5.5f, 0f), new Vector3(0.25f, 6.0f, 0.25f), matMetalDark);
        }

        // Archetype 2: Cyber Brutalist Industrial Block with Cooling Units & Ducting
        private void BuildBrutalistTower(Transform parent, float w, float h, float d, Material matMain, Material matTrim, System.Random rnd, bool isLeft)
        {
            // Massive Monolithic Core
            CreateDecorationCube("CoreBlock", parent, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), matMain);

            // 2 Cantilevered Observation / Utility Pods protruding from facade
            float podY1 = h * 0.4f;
            float podY2 = h * 0.7f;
            float podSideX = isLeft ? (w * 0.5f + 1.2f) : -(w * 0.5f + 1.2f);
            CreateDecorationCube("Pod_1", parent, new Vector3(podSideX, podY1, 2.0f), new Vector3(3.2f, 4.0f, 5.5f), matTrim);
            CreateDecorationCube("Pod_2", parent, new Vector3(podSideX, podY2, -3.0f), new Vector3(3.0f, 3.5f, 5.0f), matTrim);

            // Industrial Vertical Conduit Pipes
            float pipeX = isLeft ? (w * 0.5f + 0.1f) : -(w * 0.5f + 0.1f);
            CreateDecorationCube("Pipe_1", parent, new Vector3(pipeX, h * 0.5f, 6.0f), new Vector3(0.4f, h, 0.4f), matMetalDark);
            CreateDecorationCube("Pipe_2", parent, new Vector3(pipeX, h * 0.5f, -6.0f), new Vector3(0.4f, h, 0.4f), matMetalDark);

            // Rooftop Industrial Cooling Tanks & HVAC Outlets
            CreateDecorationCube("HVAC_Block1", parent, new Vector3(0f, h + 2.0f, 3.0f), new Vector3(w * 0.5f, 4.0f, 5.0f), matTrim);
            CreateDecorationCube("WaterTank", parent, new Vector3(0f, h + 2.5f, -3.5f), new Vector3(4.0f, 5.0f, 4.0f), matMetalDark);
        }

        // Archetype 3: Twin-Pillar Skybridge Megastructure
        private void BuildTwinSkybridgeTower(Transform parent, float w, float h, float d, Material matMain, Material matTrim, System.Random rnd, bool isLeft)
        {
            float pillarD = d * 0.42f;
            float gapD = d * 0.16f;

            // Pillar A (Front)
            CreateDecorationCube("Pillar_Front", parent, new Vector3(0f, h * 0.5f, pillarD * 0.5f + gapD * 0.5f), new Vector3(w, h, pillarD), matMain);

            // Pillar B (Back)
            CreateDecorationCube("Pillar_Back", parent, new Vector3(0f, h * 0.5f, -(pillarD * 0.5f + gapD * 0.5f)), new Vector3(w, h, pillarD), matMain);

            // Elevated Skybridge 1 (spanning between Pillar A & B at 65% height)
            float bridgeY1 = h * 0.65f;
            CreateDecorationCube("Skybridge_Lower", parent, new Vector3(0f, bridgeY1, 0f), new Vector3(w * 0.85f, 4.0f, gapD + 1.0f), matTrim);

            // Elevated Skybridge 2 (near crown)
            float bridgeY2 = h * 0.88f;
            CreateDecorationCube("Skybridge_Upper", parent, new Vector3(0f, bridgeY2, 0f), new Vector3(w * 0.65f, 3.5f, gapD + 1.0f), matTrim);

            // Crown Roof Truss spanning both pillars
            CreateDecorationCube("CrownTruss", parent, new Vector3(0f, h + 1.5f, 0f), new Vector3(w * 0.9f, 3.0f, d * 0.95f), matTrim);
        }

        // Archetype 4: High-Density Habitation Block with Balconies
        private void BuildHabitationBlock(Transform parent, float w, float h, float d, Material matMain, Material matWin, System.Random rnd, bool isLeft)
        {
            // Core Habitation Monolith
            CreateDecorationCube("HabCore", parent, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), matMain);

            // Staggered Window / Balcony Bands
            int floorBands = 5;
            float bandSpacing = h / (floorBands + 1);
            for (int f = 1; f <= floorBands; f++)
            {
                float floorY = f * bandSpacing;
                // Horizontal Window Strip
                CreateDecorationCube($"WinStrip_{f}", parent, new Vector3(0f, floorY, 0f), new Vector3(w + 0.06f, 1.8f, d + 0.06f), matWin);

                // Exterior Maintenance Balcony facing road
                float balcX = isLeft ? (w * 0.5f + 0.6f) : -(w * 0.5f + 0.6f);
                CreateDecorationCube($"Balcony_{f}", parent, new Vector3(balcX, floorY - 0.4f, (f % 2 == 0 ? 3.0f : -3.0f)), new Vector3(1.2f, 0.4f, 6.0f), matMetalDark);
            }

            // Rooftop Satellite & Communications Array
            CreateDecorationCube("RoofSatelliteDish", parent, new Vector3(0f, h + 2.5f, 0f), new Vector3(3.2f, 3.2f, 0.6f), matMetalDark);
        }

        // Archetype 5: Monolithic Tech Megacorp with Vertical Cooling Fins & Buttresses
        private void BuildMonolithFinTower(Transform parent, float w, float h, float d, Material matMain, Material matTrim, System.Random rnd, bool isLeft)
        {
            // Main Slender Tech Monolith
            CreateDecorationCube("MonolithCore", parent, new Vector3(0f, h * 0.5f, 0f), new Vector3(w * 0.85f, h, d * 0.85f), matMain);

            // 4 Corner Structural Buttress Columns
            float colW = 2.0f;
            float offX = w * 0.45f;
            float offZ = d * 0.45f;
            float buttressH = h * 0.8f;
            CreateDecorationCube("Buttress_FL", parent, new Vector3(offX, buttressH * 0.5f, offZ), new Vector3(colW, buttressH, colW), matTrim);
            CreateDecorationCube("Buttress_FR", parent, new Vector3(offX, buttressH * 0.5f, -offZ), new Vector3(colW, buttressH, colW), matTrim);
            CreateDecorationCube("Buttress_BL", parent, new Vector3(-offX, buttressH * 0.5f, offZ), new Vector3(colW, buttressH, colW), matTrim);
            CreateDecorationCube("Buttress_BR", parent, new Vector3(-offX, buttressH * 0.5f, -offZ), new Vector3(colW, buttressH, colW), matTrim);

            // Vertical Aerodynamic Fins along facade
            float finX = isLeft ? (w * 0.43f) : -(w * 0.43f);
            CreateDecorationCube("Fin_1", parent, new Vector3(finX, h * 0.5f, -4.0f), new Vector3(0.8f, h * 0.9f, 0.2f), matTrim);
            CreateDecorationCube("Fin_2", parent, new Vector3(finX, h * 0.5f, 0.0f), new Vector3(0.8f, h * 0.9f, 0.2f), matTrim);
            CreateDecorationCube("Fin_3", parent, new Vector3(finX, h * 0.5f, 4.0f), new Vector3(0.8f, h * 0.9f, 0.2f), matTrim);

            // Penthouse Crown Dome
            CreateDecorationCube("CrownPenthouse", parent, new Vector3(0f, h + 1.8f, 0f), new Vector3(w * 0.6f, 3.6f, d * 0.6f), matTrim);
        }

        // ==========================================
        // HIGH-ALTITUDE HOLOGRAPHIC DECORATIONS
        // Elevated at Y = 22m to 34m, completely clear view for the player!
        // ==========================================
        private void SpawnHighAltitudeHologram(Transform chunkTransform, int index)
        {
            int holoType = index % 3;

            switch (holoType)
            {
                case 0:
                    // Giant Ethereal Holographic Koi / Flying Cyber Whale (like the golden creature in the picture!)
                    BuildHoloKoi(chunkTransform, index);
                    break;
                case 1:
                    // Orbital Wireframe Cyber Rings / Holo-Globe
                    BuildHoloOrbitalRings(chunkTransform, index);
                    break;
                case 2:
                    // Floating High-Altitude Cyber Corporation Emblem / Banner
                    BuildHoloCorporateEmblem(chunkTransform, index);
                    break;
            }
        }

        /// <summary>
        /// Giant majestic holographic Koi fish / Cyber Whale swimming through the foggy night sky
        /// high above the highway, tail waving, undulating smoothly with 0 collision interference.
        /// </summary>
        private void BuildHoloKoi(Transform parent, int index)
        {
            GameObject koi = new GameObject("HoloCyberKoi");
            koi.transform.SetParent(parent, false);

            // High in the sky: Y = 24m to 30m! Completely clear view of the road below.
            float skyY = 25.0f + (index % 3) * 3.5f;
            float skyX = (index % 2 == 0) ? -2.5f : 2.5f;
            koi.transform.localPosition = new Vector3(skyX, skyY, 25.0f);

            // Alternate between glowing golden hologram and celestial cyan
            Material koiMat = (index % 2 == 0) ? matHologramGold : matHologramCyan;

            // Head (Diamond wedge)
            GameObject head = CreateDecorationCube("Koi_Head", koi.transform, new Vector3(0f, 0f, 4.5f), new Vector3(2.4f, 1.8f, 3.2f), koiMat);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Main Torso (Widest glowing diamond)
            GameObject body = CreateDecorationCube("Koi_Torso", koi.transform, new Vector3(0f, 0f, 1.2f), new Vector3(3.4f, 2.6f, 4.8f), koiMat);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Rear Torso
            GameObject rear = CreateDecorationCube("Koi_Rear", koi.transform, new Vector3(0f, 0f, -2.6f), new Vector3(2.2f, 1.8f, 3.8f), koiMat);
            rear.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Dorsal Fin (Top glowing fin)
            CreateDecorationCube("Koi_DorsalFin", koi.transform, new Vector3(0f, 2.2f, 1.0f), new Vector3(0.2f, 2.4f, 3.2f), koiMat);

            // Left Pectoral Fin (Angled wing)
            GameObject finL = CreateDecorationCube("Koi_Fin_L", koi.transform, new Vector3(-2.8f, -0.4f, 2.2f), new Vector3(3.2f, 0.18f, 2.0f), koiMat);
            finL.transform.localRotation = Quaternion.Euler(15f, 25f, -20f);

            // Right Pectoral Fin (Angled wing)
            GameObject finR = CreateDecorationCube("Koi_Fin_R", koi.transform, new Vector3(2.8f, -0.4f, 2.2f), new Vector3(3.2f, 0.18f, 2.0f), koiMat);
            finR.transform.localRotation = Quaternion.Euler(15f, -25f, 20f);

            // Tail Joint & Flowing Caudal Tail Fin (Animated tail wagging)
            GameObject tailJoint = new GameObject("TailJoint");
            tailJoint.transform.SetParent(koi.transform, false);
            tailJoint.transform.localPosition = new Vector3(0f, 0f, -4.5f);

            // Flowing Tail Fin Blade
            CreateDecorationCube("Koi_TailFin", tailJoint.transform, new Vector3(0f, 0f, -2.4f), new Vector3(0.25f, 4.2f, 4.8f), koiMat);

            // Attach Hologram Animator
            CyberHologram holo = koi.AddComponent<CyberHologram>();
            holo.type = HologramType.FloatingCreature;
            holo.bobAmplitude = 1.6f;
            holo.bobFrequency = 0.55f;
            holo.tailTransform = tailJoint.transform;
        }

        /// <summary>
        /// Concentric wireframe orbital cyber rings rotating high in the skyline.
        /// </summary>
        private void BuildHoloOrbitalRings(Transform parent, int index)
        {
            GameObject rings = new GameObject("HoloOrbitalRings");
            rings.transform.SetParent(parent, false);
            rings.transform.localPosition = new Vector3(0f, 27.0f, 25.0f);

            Material ringMat = matHologramCyan;
            Material coreMat = matHologramGold;

            // Outer Ring Segments (Approximated 8-segment circle)
            float rOuter = 5.5f;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                float segX = Mathf.Cos(angle) * rOuter;
                float segY = Mathf.Sin(angle) * rOuter;
                GameObject seg = CreateDecorationCube($"Ring1_Seg_{i}", rings.transform, new Vector3(segX, segY, 0f), new Vector3(0.18f, 4.2f, 0.18f), ringMat);
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f + 90f);
            }

            // Inner Counter-Tilted Ring
            GameObject innerRing = new GameObject("InnerRing");
            innerRing.transform.SetParent(rings.transform, false);
            innerRing.transform.localRotation = Quaternion.Euler(35f, 45f, 0f);

            float rInner = 3.8f;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                float segX = Mathf.Cos(angle) * rInner;
                float segY = Mathf.Sin(angle) * rInner;
                GameObject seg = CreateDecorationCube($"Ring2_Seg_{i}", innerRing.transform, new Vector3(segX, segY, 0f), new Vector3(0.16f, 3.8f, 0.16f), matHologramMagenta);
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f + 90f);
            }

            // Central Floating Glowing Holo-Core Diamond
            GameObject core = CreateDecorationCube("CoreDiamond", rings.transform, Vector3.zero, new Vector3(1.8f, 1.8f, 1.8f), coreMat);
            core.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);

            CyberHologram holo = rings.AddComponent<CyberHologram>();
            holo.type = HologramType.RotatingOrb;
            holo.rotateSpeed = 22f;
        }

        /// <summary>
        /// Floating high-altitude corporate hologram emblem / matrix logo suspended between building crowns.
        /// </summary>
        private void BuildHoloCorporateEmblem(Transform parent, int index)
        {
            GameObject emblem = new GameObject("HoloCorporateEmblem");
            emblem.transform.SetParent(parent, false);
            emblem.transform.localPosition = new Vector3(0f, 26.0f, 25.0f);

            Material emblemMat = (index % 2 == 0) ? matHologramCyan : matHologramMagenta;

            // Geometric Cyber Crest Frame
            CreateDecorationCube("HoloBar_Top", emblem.transform, new Vector3(0f, 3.0f, 0f), new Vector3(9.0f, 0.2f, 0.2f), emblemMat);
            CreateDecorationCube("HoloBar_Btm", emblem.transform, new Vector3(0f, -3.0f, 0f), new Vector3(9.0f, 0.2f, 0.2f), emblemMat);
            CreateDecorationCube("HoloBar_L", emblem.transform, new Vector3(-4.5f, 0f, 0f), new Vector3(0.2f, 6.0f, 0.2f), emblemMat);
            CreateDecorationCube("HoloBar_R", emblem.transform, new Vector3(4.5f, 0f, 0f), new Vector3(0.2f, 6.0f, 0.2f), emblemMat);

            // Holographic Billboard Projection Field
            GameObject proj = CreateDecorationCube("HoloGraphicField", emblem.transform, Vector3.zero, new Vector3(8.5f, 5.5f, 0.05f), matBillboardA);
            proj.AddComponent<CyberNeonFlicker>();

            // Floating Central Diamond Insignia
            GameObject crest = CreateDecorationCube("CrestDiamond", emblem.transform, new Vector3(0f, 0f, 0.2f), new Vector3(2.2f, 2.2f, 0.1f), matHologramGold);
            crest.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            CyberHologram holo = emblem.AddComponent<CyberHologram>();
            holo.type = HologramType.PulseBeacon;
            holo.rotateSpeed = 10f;
        }

        // ==========================================
        // 4. GAMEPLAY OBSTACLES, TRAINS & COINS
        // ==========================================
        private void BuildGameplayElements(GameObject chunk, int index)
        {
            int pattern = index % 5;

            switch (pattern)
            {
                case 0:
                    // Pattern 0: Laser Grid Gauntlet — slide for YELLOW (life) coins
                    SpawnLaserBarrier(chunk, 0, 15f);
                    SpawnLaserBarrier(chunk, 1, 28f);
                    SpawnLaserBarrier(chunk, 2, 40f);
                    SpawnCoinRow(chunk, 1, 10f, 22f, 4, CoinType.Yellow); // life bar
                    SpawnCoinRow(chunk, 2, 23f, 35f, 4, CoinType.Yellow);
                    break;

                case 1:
                    // Pattern 1: Maglev Train — BLUE (slow) coins on open lane
                    SpawnMaglevTrain(chunk, 1, 15f);
                    SpawnHighBarrier(chunk, 0, 20f);
                    SpawnCoinRow(chunk, 2, 10f, 35f, 6, CoinType.Blue); // slow-enemy bar
                    break;

                case 2:
                    // Pattern 2: Barricade gauntlet — YELLOW arc + RED side
                    SpawnHighBarrier(chunk, 0, 14f);
                    SpawnHighBarrier(chunk, 2, 14f);
                    SpawnCoinArc(chunk, 1, 10f, 25f, 5, CoinType.Yellow); // arc for life
                    SpawnHighBarrier(chunk, 1, 32f);
                    SpawnLaserBarrier(chunk, 0, 32f);
                    SpawnCoinRow(chunk, 2, 28f, 42f, 5, CoinType.Red);   // speed bar
                    break;

                case 3:
                    // Pattern 3: Dual Maglev — RED speed coins down center
                    SpawnMaglevTrain(chunk, 0, 12f);
                    SpawnMaglevTrain(chunk, 2, 18f);
                    SpawnCoinRow(chunk, 1, 8f, 44f, 8, CoinType.Red);
                    break;

                case 4:
                    // Pattern 4: Train + Laser — BLUE coin reward for dodging
                    SpawnMaglevTrain(chunk, 0, 10f);
                    SpawnHighBarrier(chunk, 1, 20f);
                    SpawnLaserBarrier(chunk, 2, 20f);
                    SpawnCoinRow(chunk, 1, 30f, 45f, 5, CoinType.Blue);
                    break;
            }
        }

        // ==========================================
        // MAGLEV TRAIN BUILDER (ULTRA-SHORT COMPACT SLOPED NOSE)
        // ==========================================
        private void SpawnMaglevTrain(GameObject chunk, int lane, float startZ, bool includeRamp = true)
        {
            float laneX = (lane - 1) * LANE_DISTANCE;
            float trainLength = 22f;
            float trainHeight = 1.6f; // Sleek low profile subway car (leaves complete clear view)
            float trainWidth = 2.3f;
            float centerZ = startZ + trainLength * 0.5f;

            GameObject train = new GameObject($"MaglevTrain_Lane{lane}");
            train.transform.SetParent(chunk.transform);
            train.transform.localPosition = new Vector3(laneX, 0f, centerZ);

            // 1. Main Hull Body (from z = -trainLength/2 to +trainLength/2)
            GameObject hull = CreateCube("Hull", train.transform, new Vector3(0f, trainHeight * 0.5f, 0f), new Vector3(trainWidth, trainHeight, trainLength), matTrainHull);

            // 2. Aerodynamic Top Shell / Walkable Roof
            GameObject roof = CreateCube("Roof", train.transform, new Vector3(0f, trainHeight, 0f), new Vector3(trainWidth * 0.96f, 0.10f, trainLength), matTrainWhite);
            BoxCollider roofCol = roof.GetComponent<BoxCollider>();
            if (roofCol == null) roofCol = roof.AddComponent<BoxCollider>();

            // Roof Neon Light Guides (cosmetic, remove collider)
            GameObject railL = CreateCube("RoofRail_L", train.transform, new Vector3(-trainWidth * 0.45f, trainHeight + 0.06f, 0f), new Vector3(0.05f, 0.05f, trainLength), matNeonCyan);
            DestroyCollider(railL);
            GameObject railR = CreateCube("RoofRail_R", train.transform, new Vector3(trainWidth * 0.45f, trainHeight + 0.06f, 0f), new Vector3(0.05f, 0.05f, trainLength), matNeonCyan);
            DestroyCollider(railR);

            // Side Decals & Window Strips (cosmetic, remove collider)
            GameObject decalL = CreateCube("SideDecal_L", train.transform, new Vector3(-trainWidth * 0.51f, trainHeight * 0.55f, 0f), new Vector3(0.02f, 0.5f, trainLength * 0.9f), matTrainSide);
            DestroyCollider(decalL);
            GameObject decalR = CreateCube("SideDecal_R", train.transform, new Vector3(trainWidth * 0.51f, trainHeight * 0.55f, 0f), new Vector3(0.02f, 0.5f, trainLength * 0.9f), matTrainSide);
            DestroyCollider(decalR);

            // 3. Ultra-Short Head Slope (tight 1.4m wedge directly on train bumper)
            float slopeRun = 1.4f; // Ultra-short slope run
            float halfLength = trainLength * 0.5f;
            float slopeAngle = Mathf.Atan2(trainHeight, slopeRun) * Mathf.Rad2Deg; // ~48.8 deg (within 60 deg slope limit)
            float rampHypotenuse = Mathf.Sqrt(slopeRun * slopeRun + trainHeight * trainHeight); // ~2.13m
            float rampCenterZ = -halfLength - (slopeRun * 0.5f); // -11.7f
            float rampCenterY = trainHeight * 0.5f; // 0.80f

            // Walkable sloped ramp surface
            GameObject ramp = CreateCube("HeadSlopeRamp", train.transform, new Vector3(0f, rampCenterY, rampCenterZ), new Vector3(trainWidth * 0.90f, 0.10f, rampHypotenuse), matNeonCyan);
            ramp.transform.localRotation = Quaternion.Euler(-slopeAngle, 0f, 0f);

            // Compact yellow hazard chevron strip
            GameObject rampHazard = CreateCube("RampHazard", ramp.transform, new Vector3(0f, 0.06f, 0f), new Vector3(trainWidth * 0.40f, 0.012f, rampHypotenuse * 0.65f), matHazard);
            DestroyCollider(rampHazard);

            // Compact neon blue edge trims
            GameObject slopeRailL = CreateCube("SlopeRail_L", ramp.transform, new Vector3(-trainWidth * 0.42f, 0.06f, 0f), new Vector3(0.04f, 0.04f, rampHypotenuse), matNeonCyan);
            DestroyCollider(slopeRailL);
            GameObject slopeRailR = CreateCube("SlopeRail_R", ramp.transform, new Vector3(trainWidth * 0.42f, 0.06f, 0f), new Vector3(0.04f, 0.04f, rampHypotenuse), matNeonCyan);
            DestroyCollider(slopeRailR);

            // Compact underbody filler (cosmetic only, no colliders)
            GameObject stepBody = CreateCube("NoseUnderbody", train.transform, new Vector3(0f, trainHeight * 0.25f, -halfLength - slopeRun * 0.35f), new Vector3(trainWidth * 0.84f, trainHeight * 0.5f, slopeRun * 0.6f), matTrainHull);
            DestroyCollider(stepBody);

            // Sleek corner headlights (cosmetic, no colliders)
            float footZ = -halfLength - slopeRun;
            GameObject headL = CreateCube("Headlight_L", train.transform, new Vector3(-trainWidth * 0.36f, 0.14f, footZ + 0.15f), new Vector3(0.18f, 0.12f, 0.12f), matTrainHeadlight);
            DestroyCollider(headL);
            GameObject headR = CreateCube("Headlight_R", train.transform, new Vector3(trainWidth * 0.36f, 0.14f, footZ + 0.15f), new Vector3(0.18f, 0.12f, 0.12f), matTrainHeadlight);
            DestroyCollider(headR);

            // 4. Rooftop Coins — RED speed coins on train roof
            for (float z = -halfLength + 2.0f; z <= halfLength - 2.0f; z += 3.2f)
            {
                SpawnCoin(train.transform, new Vector3(0f, trainHeight + 0.50f, z), CoinType.Red);
            }
        }

        // ==========================================
        // LASER BARRIER (DUCK / SLIDE UNDER)
        // ==========================================
        private void SpawnLaserBarrier(GameObject chunk, int lane, float zPos)
        {
            float laneX = (lane - 1) * LANE_DISTANCE;
            float laserY = 1.15f; // Height player can slide under!

            GameObject laserObj = new GameObject($"LaserBarrier_Lane{lane}");
            laserObj.transform.SetParent(chunk.transform);
            laserObj.transform.localPosition = new Vector3(laneX, 0f, zPos);

            // Left & Right Cyber Emitter Pillars (lethal if collided)
            GameObject pillarL = CreateCube("Pillar_L", laserObj.transform, new Vector3(-1.25f, 1.1f, 0f), new Vector3(0.3f, 2.2f, 0.3f), matMetalDark);
            GameObject pillarR = CreateCube("Pillar_R", laserObj.transform, new Vector3(1.25f, 1.1f, 0f), new Vector3(0.3f, 2.2f, 0.3f), matMetalDark);
            CyberObstacle obsL = pillarL.AddComponent<CyberObstacle>();
            obsL.obstacleType = ObstacleType.SolidPillar;
            CyberObstacle obsR = pillarR.AddComponent<CyberObstacle>();
            obsR.obstacleType = ObstacleType.SolidPillar;

            // Glowing High-Energy Laser Beams (cosmetic visual, remove primitive colliders)
            GameObject beam1 = CreateCube("LaserBeam1", laserObj.transform, new Vector3(0f, laserY, 0f), new Vector3(2.4f, 0.08f, 0.08f), matLaserRed);
            GameObject beam2 = CreateCube("LaserBeam2", laserObj.transform, new Vector3(0f, laserY + 0.25f, 0f), new Vector3(2.4f, 0.08f, 0.08f), matLaserRed);
            DestroyCollider(beam1);
            DestroyCollider(beam2);

            // Obstacle Trigger for laser beam
            BoxCollider col = laserObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, laserY + 0.1f, 0f);
            col.size = new Vector3(2.4f, 0.6f, 0.4f);

            CyberObstacle obs = laserObj.AddComponent<CyberObstacle>();
            obs.obstacleType = ObstacleType.LowLaser;
            obs.obstacleHeight = laserY;
        }

        // ==========================================
        // HIGH BARRICADE (JUMP OVER)
        // ==========================================
        private void SpawnHighBarrier(GameObject chunk, int lane, float zPos)
        {
            float laneX = (lane - 1) * LANE_DISTANCE;
            float barrierHeight = 0.85f;

            GameObject barrierObj = new GameObject($"HighBarrier_Lane{lane}");
            barrierObj.transform.SetParent(chunk.transform);
            barrierObj.transform.localPosition = new Vector3(laneX, 0f, zPos);

            // Base Barricade Box & Neon (visuals, remove primitive colliders so trigger handles collision)
            GameObject body = CreateCube("BarricadeBody", barrierObj.transform, new Vector3(0f, barrierHeight * 0.5f, 0f), new Vector3(2.4f, barrierHeight, 0.5f), matHazard);
            GameObject neon = CreateCube("TopNeon", barrierObj.transform, new Vector3(0f, barrierHeight + 0.04f, 0f), new Vector3(2.4f, 0.08f, 0.52f), matNeonYellow);
            DestroyCollider(body);
            DestroyCollider(neon);

            // Obstacle Trigger (hitting this causes instant death!)
            BoxCollider col = barrierObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, barrierHeight * 0.5f, 0f);
            col.size = new Vector3(2.4f, barrierHeight, 0.6f);

            CyberObstacle obs = barrierObj.AddComponent<CyberObstacle>();
            obs.obstacleType = ObstacleType.HighBarrier;
            obs.obstacleHeight = barrierHeight;
        }

        // ==========================================
        // COIN SPAWNERS
        // ==========================================
        private void SpawnCoinRow(GameObject chunk, int lane, float startZ, float endZ, int count, CoinType type = CoinType.Yellow)
        {
            float laneX = (lane - 1) * LANE_DISTANCE;
            float step = (endZ - startZ) / Mathf.Max(1, count - 1);

            for (int i = 0; i < count; i++)
            {
                float z = startZ + i * step;
                SpawnCoin(chunk.transform, new Vector3(laneX, 0.65f, z), type);
            }
        }

        private void SpawnCoinArc(GameObject chunk, int lane, float startZ, float endZ, int count, CoinType type = CoinType.Yellow)
        {
            float laneX = (lane - 1) * LANE_DISTANCE;
            float step = (endZ - startZ) / Mathf.Max(1, count - 1);

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float z = startZ + i * step;
                // Parabolic jump arc
                float y = 0.65f + Mathf.Sin(t * Mathf.PI) * 1.8f;
                SpawnCoin(chunk.transform, new Vector3(laneX, y, z), type);
            }
        }

        public GameObject SpawnCoin(Transform parent, Vector3 localPos, CoinType type = CoinType.Yellow)
        {
            // Name reflects type so it's identifiable in the Hierarchy
            string typeName = type == CoinType.Red ? "RED" : type == CoinType.Blue ? "BLUE" : "YEL";
            GameObject coin = new GameObject($"CyberCoin_{typeName}");
            coin.transform.SetParent(parent);
            coin.transform.localPosition = localPos;

            // ── Vivid self-lit coin material per type ──
            Material coinMat;
            if (type == CoinType.Red)
            {
                coinMat = CreateNeonMaterial("CoinMat_Red",  new Color(1.00f, 0.12f, 0.18f), 6f);
            }
            else if (type == CoinType.Blue)
            {
                coinMat = CreateNeonMaterial("CoinMat_Blue", new Color(0.05f, 0.60f, 1.00f), 6f);
            }
            else
            {
                // Yellow — bright gold emission
                coinMat = CreateNeonMaterial("CoinMat_Gold", new Color(1.00f, 0.85f, 0.05f), 6f);
            }

            // Larger diamond (0.80 vs 0.55 before) so it's eye-catching from a distance
            GameObject visual = CreateCube("CoinMesh", coin.transform, Vector3.zero, new Vector3(0.80f, 0.80f, 0.18f), coinMat);
            visual.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            DestroyCollider(visual);

            // Small glow halo ring around the diamond
            GameObject ring = CreateCube("CoinRing", coin.transform, Vector3.zero, new Vector3(0.95f, 0.95f, 0.06f), coinMat);
            ring.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            DestroyCollider(ring);

            SphereCollider col = coin.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.55f;

            CyberCoin cyberCoin = coin.AddComponent<CyberCoin>();
            cyberCoin.coinType = type;
            return coin;
        }

        // ==========================================
        // PRIMITIVE UTILITY
        // ==========================================
        private GameObject CreateCube(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.identity;

            if (mat != null)
            {
                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = mat;
            }

            return go;
        }

        private GameObject CreateDecorationCube(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = CreateCube(name, parent, localPos, localScale, mat);
            DestroyCollider(go);
            return go;
        }

        private static void DestroyCollider(GameObject go)
        {
            if (go == null) return;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }
        }
    }
}
