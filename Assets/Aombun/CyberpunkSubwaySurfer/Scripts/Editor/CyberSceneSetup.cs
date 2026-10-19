using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CyberpunkSubwaySurfer.Editor
{
    public static class CyberSceneSetup
    {
        [MenuItem("Cyberpunk/2. Setup Cyberpunk Subway Surfer Scene")]
        public static void SetupScene()
        {
            // 1. Ensure Textures & Materials are generated first
            CyberAssetGenerator.GenerateAll();

            // 2. Setup Lighting & Fog
            SetupEnvironmentLighting();

            // 3. Setup Player
            GameObject playerGo = SetupPlayer();

            // 4. Setup Camera
            SetupCamera(playerGo);

            // 5. Setup Track Generator & Prefab Chunks
            GameObject generatorGo = SetupTrackGenerator(playerGo);

            // 6. Setup Game Manager & UI
            SetupGameManager(playerGo, generatorGo);

            // Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#00f0ff><b>[CyberSceneSetup]</b> Cyberpunk Subway Surfer Scene Setup Complete! Press PLAY to run!</color>");
        }

        private static void SetupEnvironmentLighting()
        {
            // Cyberpunk Dark Blue / Navy Atmospheric Fog
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.06f, 1f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.006f;

            // Ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.12f, 0.18f, 1f);

            // Directional Light adjustment
            Light dirLight = Object.FindFirstObjectByType<Light>();
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                dirLight.color = new Color(0.40f, 0.50f, 0.75f, 1f);
                dirLight.intensity = 0.40f;
                dirLight.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            }
        }

        private static GameObject SetupPlayer()
        {
            // Destroy existing player if any
            CyberPlayerController existing = Object.FindFirstObjectByType<CyberPlayerController>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject player = new GameObject("CyberPlayer");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.radius = 0.45f;
            cc.height = 2.0f;
            cc.center = new Vector3(0f, 1.0f, 0f);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 60f;

            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            CyberPlayerController controller = player.AddComponent<CyberPlayerController>();

            // Load materials
            Material suitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CyberpunkSubwaySurfer/Materials/Cyber_Player_Suit.mat");
            Material visorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CyberpunkSubwaySurfer/Materials/Cyber_Player_Visor.mat");
            Material cyanMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CyberpunkSubwaySurfer/Materials/Cyber_Neon_Cyan.mat");
            Material darkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CyberpunkSubwaySurfer/Materials/Cyber_Metal_Dark.mat");

            // Build Clean Bone Hierarchy (All pivots keep localScale = Vector3.one)
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(player.transform, false);
            controller.visualRoot = visualRoot.transform;

            // Hips
            GameObject hipsPivot = new GameObject("HipsPivot");
            hipsPivot.transform.SetParent(visualRoot.transform, false);
            hipsPivot.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            CreateMesh("HipsMesh", hipsPivot.transform, Vector3.zero, new Vector3(0.42f, 0.22f, 0.28f), suitMat);
            CreateMesh("BeltNeon", hipsPivot.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.44f, 0.05f, 0.30f), cyanMat);

            // Torso
            GameObject torsoPivot = new GameObject("TorsoPivot");
            torsoPivot.transform.SetParent(hipsPivot.transform, false);
            torsoPivot.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            CreateMesh("TorsoMesh", torsoPivot.transform, new Vector3(0f, 0.26f, 0f), new Vector3(0.48f, 0.50f, 0.32f), suitMat);
            CreateMesh("ChestArmor", torsoPivot.transform, new Vector3(0f, 0.30f, 0.13f), new Vector3(0.40f, 0.36f, 0.08f), darkMat);
            CreateMesh("ReactorCore", torsoPivot.transform, new Vector3(0f, 0.32f, 0.18f), new Vector3(0.16f, 0.16f, 0.04f), cyanMat);

            // Jetpack & Thrusters
            GameObject jetpackPivot = new GameObject("JetpackPivot");
            jetpackPivot.transform.SetParent(torsoPivot.transform, false);
            jetpackPivot.transform.localPosition = new Vector3(0f, 0.26f, -0.20f);
            CreateMesh("JetpackMesh", jetpackPivot.transform, Vector3.zero, new Vector3(0.34f, 0.42f, 0.16f), darkMat);
            GameObject thrustL = CreateMesh("Thrust_L", jetpackPivot.transform, new Vector3(-0.11f, -0.22f, 0f), new Vector3(0.08f, 0.18f, 0.08f), cyanMat);
            GameObject thrustR = CreateMesh("Thrust_R", jetpackPivot.transform, new Vector3(0.11f, -0.22f, 0f), new Vector3(0.08f, 0.18f, 0.08f), cyanMat);
            controller.thrusterLeft = thrustL.transform;
            controller.thrusterRight = thrustR.transform;

            // Head & Helmet
            GameObject headPivot = new GameObject("HeadPivot");
            headPivot.transform.SetParent(torsoPivot.transform, false);
            headPivot.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            CreateMesh("HeadMesh", headPivot.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.32f, 0.34f, 0.32f), suitMat);
            CreateMesh("VisorMesh", headPivot.transform, new Vector3(0f, 0.17f, 0.16f), new Vector3(0.30f, 0.10f, 0.06f), visorMat);
            CreateMesh("HelmetFin", headPivot.transform, new Vector3(0f, 0.34f, -0.02f), new Vector3(0.05f, 0.07f, 0.30f), cyanMat);

            // Arms
            GameObject armPivotL = new GameObject("ArmPivot_L");
            armPivotL.transform.SetParent(torsoPivot.transform, false);
            armPivotL.transform.localPosition = new Vector3(-0.32f, 0.42f, 0f);
            CreateMesh("Shoulder_L", armPivotL.transform, Vector3.zero, new Vector3(0.16f, 0.14f, 0.20f), darkMat);
            CreateMesh("ArmMesh_L", armPivotL.transform, new Vector3(0f, -0.24f, 0f), new Vector3(0.13f, 0.42f, 0.13f), suitMat);
            CreateMesh("Wrist_L", armPivotL.transform, new Vector3(0f, -0.42f, 0f), new Vector3(0.15f, 0.12f, 0.15f), cyanMat);
            controller.leftArm = armPivotL.transform;

            GameObject armPivotR = new GameObject("ArmPivot_R");
            armPivotR.transform.SetParent(torsoPivot.transform, false);
            armPivotR.transform.localPosition = new Vector3(0.32f, 0.42f, 0f);
            CreateMesh("Shoulder_R", armPivotR.transform, Vector3.zero, new Vector3(0.16f, 0.14f, 0.20f), darkMat);
            CreateMesh("ArmMesh_R", armPivotR.transform, new Vector3(0f, -0.24f, 0f), new Vector3(0.13f, 0.42f, 0.13f), suitMat);
            CreateMesh("Wrist_R", armPivotR.transform, new Vector3(0f, -0.42f, 0f), new Vector3(0.15f, 0.12f, 0.15f), cyanMat);
            controller.rightArm = armPivotR.transform;

            // Legs
            GameObject legPivotL = new GameObject("LegPivot_L");
            legPivotL.transform.SetParent(hipsPivot.transform, false);
            legPivotL.transform.localPosition = new Vector3(-0.14f, -0.05f, 0f);
            CreateMesh("LegMesh_L", legPivotL.transform, new Vector3(0f, -0.34f, 0f), new Vector3(0.15f, 0.65f, 0.15f), suitMat);
            CreateMesh("Knee_L", legPivotL.transform, new Vector3(0f, -0.32f, 0.08f), new Vector3(0.13f, 0.10f, 0.05f), cyanMat);
            CreateMesh("Foot_L", legPivotL.transform, new Vector3(0f, -0.69f, 0.07f), new Vector3(0.16f, 0.12f, 0.30f), darkMat);
            controller.leftLeg = legPivotL.transform;

            GameObject legPivotR = new GameObject("LegPivot_R");
            legPivotR.transform.SetParent(hipsPivot.transform, false);
            legPivotR.transform.localPosition = new Vector3(0.14f, -0.05f, 0f);
            CreateMesh("LegMesh_R", legPivotR.transform, new Vector3(0f, -0.34f, 0f), new Vector3(0.15f, 0.65f, 0.15f), suitMat);
            CreateMesh("Knee_R", legPivotR.transform, new Vector3(0f, -0.32f, 0.08f), new Vector3(0.13f, 0.10f, 0.05f), cyanMat);
            CreateMesh("Foot_R", legPivotR.transform, new Vector3(0f, -0.69f, 0.07f), new Vector3(0.16f, 0.12f, 0.30f), darkMat);
            controller.rightLeg = legPivotR.transform;

            return player;
        }

        private static void SetupCamera(GameObject player)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f, 1f);

            var urpCam = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (urpCam == null) urpCam = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;

            CyberCameraController camCtrl = cam.GetComponent<CyberCameraController>();
            if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<CyberCameraController>();

            camCtrl.target = player.transform;
            camCtrl.playerController = player.GetComponent<CyberPlayerController>();
            camCtrl.distance = 6.4f;
            camCtrl.height = 3.5f;
            camCtrl.lookAheadOffset = new Vector3(0f, 1.2f, 6.0f);
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 350f;
        }

        private static GameObject SetupTrackGenerator(GameObject player)
        {
            CyberTrackGenerator existing = Object.FindFirstObjectByType<CyberTrackGenerator>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject genGo = new GameObject("TrackGenerator");
            CyberTrackGenerator generator = genGo.AddComponent<CyberTrackGenerator>();
            generator.playerTransform = player.transform;
            generator.LoadMaterialsIfNeeded();

            // Pre-generate 6 chunks in Editor so user immediately sees the full cyberpunk city and subway tracks!
            for (int i = 0; i < 6; i++)
            {
                generator.SpawnNextChunk(i < 2);
            }

            return genGo;
        }

        private static void SetupGameManager(GameObject player, GameObject generator)
        {
            CyberGameManager existing = Object.FindFirstObjectByType<CyberGameManager>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject gmGo = new GameObject("GameManager");
            CyberGameManager gm = gmGo.AddComponent<CyberGameManager>();
            gm.player = player.GetComponent<CyberPlayerController>();
            gm.trackGenerator = generator.GetComponent<CyberTrackGenerator>();
        }

        private static GameObject CreateMesh(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            // Remove box collider from cosmetic character parts so they don't block CharacterController
            BoxCollider col = go.GetComponent<BoxCollider>();
            if (col != null) Object.DestroyImmediate(col);

            if (mat != null)
            {
                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = mat;
            }

            return go;
        }
    }
}
