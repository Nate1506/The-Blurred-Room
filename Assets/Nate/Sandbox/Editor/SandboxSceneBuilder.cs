using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BlurredRoom.Sandbox.EditorTools
{
    /// <summary>
    /// Builds and saves Assets/Nate/Sandbox/NateSandbox.unity (grey-box dorm floor) plus its materials, data and NavMesh.
    /// Run once in batchmode:
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod BlurredRoom.Sandbox.EditorTools.SandboxSceneBuilder.Build
    /// Re-running overwrites the scene and the generated assets.
    /// Layout (metres): corridor along X from 0 to 20, Z from -1.5 to 1.5. Rooms are 5x5 on both sides:
    ///   Room 1 (player)  x 2..7,  south | Room 2  x 2..7,  north
    ///   Room 3           x 11..16, south | Room 4  x 11..16, north (locked, altar)
    /// </summary>
    public static class SandboxSceneBuilder
    {
        const string Root = "Assets/Nate/Sandbox";
        const string ScenePath = Root + "/NateSandbox.unity";
        const string MaterialDir = Root + "/Materials";
        const string DataDir = Root + "/Data";
        const string NavMeshPath = Root + "/NateSandbox-NavMesh.asset";

        const float WallHeight = 3f;
        const float WallThickness = 0.2f;
        const float DoorWidth = 1.2f;
        const float CorridorHalfWidth = 1.5f;
        const float CorridorLength = 20f;
        const float RoomDepth = 5f;

        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static void Build()
        {
            try
            {
                BuildScene();
                Debug.Log("[SandboxSceneBuilder] Built and saved " + ScenePath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        static void BuildScene()
        {
            EnsureFolder(Root);
            EnsureFolder(MaterialDir);
            EnsureFolder(DataDir);
            Materials.Clear();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupRenderSettings();

            // ---------- level geometry (baked into the NavMesh) ----------
            var level = new GameObject("Level");
            BuildGeometry(level.transform);

            var lights = new GameObject("Lights");
            BuildLights(lights.transform);

            // ---------- doors (not part of the bake; panels carve the NavMesh at runtime) ----------
            var doorsRoot = new GameObject("Doors");
            Door room1 = MakeDoor("Room 1 Door", new Vector3(4.5f, 0f, -CorridorHalfWidth), true, false, doorsRoot.transform);
            Door room2 = MakeDoor("Room 2 Door", new Vector3(4.5f, 0f, CorridorHalfWidth), false, false, doorsRoot.transform);
            Door room3 = MakeDoor("Room 3 Door", new Vector3(13.5f, 0f, -CorridorHalfWidth), true, false, doorsRoot.transform);
            Door room4 = MakeDoor("Room 4 Door", new Vector3(13.5f, 0f, CorridorHalfWidth), false, true, doorsRoot.transform);
            Door[] allDoors = { room1, room2, room3, room4 };

            // ---------- story data ----------
            ClueData diary = Clue("clue_diary", "Diary", "The previous caretaker's diary: \"The knocking always comes after dark.\"");
            ClueData photo = Clue("clue_old_photo", "Old photo", "A photo of the dorm, 1987. A blurred man stands in the corridor.");
            ClueData landladyStory = Clue("clue_landlady_story", "Landlady's story", "Room 4 has been locked since a student vanished.");
            ClueData studentRumour = Clue("clue_student_rumour", "Student's rumour", "A man with no face walks the corridor at night.");

            EndingData[] endingAssets =
            {
                Ending("ending_lost", "Lost in the past", EndingReasons.SanityLost, 0,
                       "Your mind slips. The corridor stretches into 1987 and you never find your way back."),
                Ending("ending_taken", "Taken", EndingReasons.Caught, 0,
                       "A cold hand on your shoulder. The Blurred Man has a new face now."),
                Ending("ending_time_up", "Time's up", EndingReasons.TimeUp, 0,
                       "The third night ends. The landlady finds the caretaker room empty."),
                Ending("ending_truth", "The truth", EndingReasons.Solved, 10,
                       "The ritual is done and you know what happened here. The knocking stops for good.",
                       3, new[] { "ritual_done" }),
                Ending("ending_ran_away", "Ran away", EndingReasons.Solved, 0,
                       "You run out into the night without looking back. Some doors stay closed.")
            };

            // ---------- player ----------
            PlayerParts player = BuildPlayer(new Vector3(4.5f, 0.05f, -4.5f));

            // ---------- managers ----------
            var systems = new GameObject("GameSystems");
            var time = systems.AddComponent<TimeOfDayManager>();
            Configure(time, so =>
            {
                so.FindProperty("startPhase").intValue = (int)DayPhase.Day;
                so.FindProperty("startDay").intValue = 1;
                so.FindProperty("autoAdvance").boolValue = true;
                so.FindProperty("dayDuration").floatValue = 60f;
                so.FindProperty("eveDuration").floatValue = 30f;
                so.FindProperty("nightDuration").floatValue = 90f;
            });
            systems.AddComponent<ClueManager>();
            var endingManager = systems.AddComponent<EndingManager>();
            Configure(endingManager, so => SetArray(so.FindProperty("endings"), endingAssets));
            var scheduler = systems.AddComponent<DoorVisitScheduler>();
            Configure(scheduler, so =>
            {
                SetArray(so.FindProperty("doors"), allDoors);
                so.FindProperty("activePhases").intValue = (int)(DayPhaseMask.Eve | DayPhaseMask.Night);
                so.FindProperty("secondsBetweenVisits").vector2Value = new Vector2(25f, 45f);
                so.FindProperty("enemyChance").floatValue = 0.4f;
            });

            var hubGo = new GameObject("SystemHub");
            hubGo.transform.SetParent(systems.transform, false);
            var hub = hubGo.AddComponent<SystemHub>();
            Configure(hub, so =>
            {
                so.FindProperty("playerInput").objectReferenceValue = player.Input;
                so.FindProperty("playerSanity").objectReferenceValue = player.Sanity;
                so.FindProperty("lastDay").intValue = 3;
                so.FindProperty("lockPlayerOnEnding").boolValue = true;
            });

            // ---------- NPCs ----------
            var npcs = new GameObject("NPCs");
            MakeNeutral("Landlady", "npc_landlady", new Vector3(9f, 0f, 0.6f), Quaternion.Euler(0f, 180f, 0f),
                DayPhaseMask.Day | DayPhaseMask.Eve, landladyStory, npcs.transform,
                "Oh, you're the new caretaker. Welcome to the dorm.",
                "Room 4 has been locked since... the incident. The key is around here somewhere.",
                "And don't open the door at night. Whatever it says.");
            MakeNeutral("Student", "npc_student", new Vector3(4.5f, 0f, 4.8f), Quaternion.Euler(0f, 180f, 0f),
                DayPhaseMask.All, studentRumour, npcs.transform,
                "Hey. You hear the knocking too, right?",
                "They say a man with no face walks this corridor at night.",
                "If you ever see him... don't keep staring.");
            MakeEnemy(new Vector3(17.5f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), npcs.transform);

            // ---------- items, ritual, exit ----------
            var world = new GameObject("World");
            MakePickup("Room key", "room_key", null, new Vector3(16.8f, 0f, -1.1f), "Key", world.transform, level.transform);
            MakePickup("Candle", "candle", null, new Vector3(6.2f, 0f, 6.0f), "Candle", world.transform, level.transform);
            MakePickup("Candle", "candle", null, new Vector3(1.0f, 0f, 1.1f), "Candle", world.transform, level.transform);
            MakePickup("Old photo", "old_photo", photo, new Vector3(15.2f, 0f, -6.0f), "Photo", world.transform, level.transform);
            MakePickup("Diary", "diary", diary, new Vector3(2.8f, 0f, -6.0f), "Diary", world.transform, level.transform);
            MakeAltar(new Vector3(13.5f, 0f, 5.4f), world.transform);
            MakeExit(new Vector3(19.4f, 1f, 0f), world.transform);

            // ---------- sandbox helpers ----------
            var sandbox = new GameObject("Sandbox");
            var hud = sandbox.AddComponent<SandboxDebugHud>();
            var keys = sandbox.AddComponent<SandboxDebugKeys>();
            Configure(keys, so =>
            {
                so.FindProperty("playerRoomDoor").objectReferenceValue = room1;
                so.FindProperty("hud").objectReferenceValue = hud;
                so.FindProperty("systemHub").objectReferenceValue = hubGo;
                so.FindProperty("lastDay").intValue = 3;
            });
            Configure(hud, so => so.FindProperty("debugKeys").objectReferenceValue = keys);
            var keyDoor = room4.gameObject.AddComponent<SandboxKeyDoor>();
            Configure(keyDoor, so =>
            {
                so.FindProperty("door").objectReferenceValue = room4;
                so.FindProperty("inventory").objectReferenceValue = player.Inventory;
                so.FindProperty("keyItemId").stringValue = "room_key";
            });

            // ---------- NavMesh ----------
            BakeNavMesh(level);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ================= geometry =================

        static void BuildGeometry(Transform level)
        {
            float zRoom = CorridorHalfWidth + RoomDepth; // 6.5
            Box("Floor", level, new Vector3(CorridorLength / 2f, -0.1f, 0f), new Vector3(CorridorLength, 0.2f, zRoom * 2f), "Floor");

            // Corridor walls with door gaps, end walls.
            float[] doorX = { 4.5f, 13.5f };
            WallX("Corridor Wall S", level, -CorridorHalfWidth, 0f, CorridorLength, doorX);
            WallX("Corridor Wall N", level, CorridorHalfWidth, 0f, CorridorLength, doorX);
            WallZ("End Wall W", level, 0f, -CorridorHalfWidth, CorridorHalfWidth);
            WallZ("End Wall E", level, CorridorLength, -CorridorHalfWidth, CorridorHalfWidth);

            // Rooms.
            Room("Room 1", level, 2f, 7f, -1f);
            Room("Room 2", level, 2f, 7f, 1f);
            Room("Room 3", level, 11f, 16f, -1f);
            Room("Room 4", level, 11f, 16f, 1f);

            // Tables for items.
            foreach (Vector3 p in new[]
                     {
                         new Vector3(16.8f, 0f, -1.1f), new Vector3(6.2f, 0f, 6.0f), new Vector3(1.0f, 0f, 1.1f),
                         new Vector3(15.2f, 0f, -6.0f), new Vector3(2.8f, 0f, -6.0f)
                     })
                Box("Table", level, new Vector3(p.x, 0.375f, p.z), new Vector3(0.8f, 0.75f, 0.5f), "Table");

            // A bed in the caretaker room, just to make it recognisable.
            Box("Bed", level, new Vector3(6.0f, 0.25f, -4.5f), new Vector3(1.0f, 0.5f, 2.0f), "Table");
        }

        static void Room(string name, Transform parent, float x0, float x1, float side)
        {
            var room = new GameObject(name).transform;
            room.SetParent(parent, false);
            float zNear = side * CorridorHalfWidth;
            float zFar = side * (CorridorHalfWidth + RoomDepth);
            WallX(name + " Back Wall", room, zFar, x0, x1, new float[0]);
            WallZ(name + " Side Wall A", room, x0, Mathf.Min(zNear, zFar), Mathf.Max(zNear, zFar));
            WallZ(name + " Side Wall B", room, x1, Mathf.Min(zNear, zFar), Mathf.Max(zNear, zFar));
        }

        /// <summary>Wall along X at a fixed Z, with door gaps (centres) and lintels above them.</summary>
        static void WallX(string name, Transform parent, float z, float x0, float x1, float[] gaps)
        {
            float start = x0;
            int i = 0;
            var sorted = new List<float>(gaps);
            sorted.Sort();
            foreach (float g in sorted)
            {
                float gapStart = g - DoorWidth / 2f, gapEnd = g + DoorWidth / 2f;
                if (gapStart > start)
                    Box($"{name} {i++}", parent, new Vector3((start + gapStart) / 2f, WallHeight / 2f, z),
                        new Vector3(gapStart - start, WallHeight, WallThickness), "Wall");
                Box($"{name} Lintel", parent, new Vector3(g, (WallHeight + 2.15f) / 2f, z),
                    new Vector3(DoorWidth, WallHeight - 2.15f, WallThickness), "Wall");
                start = gapEnd;
            }
            if (x1 > start)
                Box($"{name} {i}", parent, new Vector3((start + x1) / 2f, WallHeight / 2f, z),
                    new Vector3(x1 - start, WallHeight, WallThickness), "Wall");
        }

        static void WallZ(string name, Transform parent, float x, float z0, float z1)
        {
            Box(name, parent, new Vector3(x, WallHeight / 2f, (z0 + z1) / 2f),
                new Vector3(WallThickness, WallHeight, z1 - z0 + WallThickness), "Wall");
        }

        static void BuildLights(Transform parent)
        {
            var sun = new GameObject("Sun (Directional)").AddComponent<Light>();
            sun.transform.SetParent(parent, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.intensity = 0.5f;
            sun.color = new Color(0.85f, 0.85f, 1f);
            sun.shadows = LightShadows.Soft;

            foreach (float x in new[] { 3f, 10f, 17f })
                PointLight("Corridor Light", parent, new Vector3(x, 2.6f, 0f), 9f, 1.3f, new Color(1f, 0.85f, 0.65f));
            PointLight("Room 1 Light", parent, new Vector3(4.5f, 2.6f, -4f), 6f, 0.9f, new Color(1f, 0.9f, 0.7f));
            PointLight("Room 2 Light", parent, new Vector3(4.5f, 2.6f, 4f), 6f, 0.7f, new Color(0.8f, 0.85f, 1f));
            PointLight("Room 3 Light", parent, new Vector3(13.5f, 2.6f, -4f), 6f, 0.7f, new Color(0.8f, 0.85f, 1f));
            PointLight("Room 4 Light", parent, new Vector3(13.5f, 2.6f, 4f), 6f, 0.6f, new Color(0.8f, 0.5f, 1f));
        }

        static void PointLight(string name, Transform parent, Vector3 pos, float range, float intensity, Color color)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = color;
        }

        static void SetupRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.26f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.07f);
            RenderSettings.fogStartDistance = 8f;
            RenderSettings.fogEndDistance = 30f;
        }

        // ================= doors =================

        /// <param name="corridorIsPlusZ">True for south rooms (the corridor is on +Z of the door).</param>
        static Door MakeDoor(string name, Vector3 position, bool corridorIsPlusZ, bool locked, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, corridorIsPlusZ ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f));
            // Local +Z of the door root always points into the corridor (outside).

            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(root.transform, false);
            hinge.localPosition = new Vector3(-DoorWidth / 2f, 0f, 0f);

            GameObject panel = LocalBox("Panel", hinge, new Vector3(DoorWidth / 2f, 1.05f, 0f), new Vector3(DoorWidth - 0.04f, 2.1f, 0.08f), "Door");
            var obstacle = panel.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            LocalBox("Peephole Dot", panel.transform, new Vector3(0f, 0.27f, 0.6f), new Vector3(0.05f, 0.025f, 0.3f), "Altar", false);

            var stand = new GameObject("VisitorStandPoint").transform;
            stand.SetParent(root.transform, false);
            stand.localPosition = new Vector3(0f, 0f, 1.3f);
            stand.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var camGo = new GameObject("PeepholeCamera");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0.15f);
            var cam = camGo.AddComponent<Camera>();
            cam.depth = 10f;
            cam.fieldOfView = 100f;
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.enabled = false;

            var door = root.AddComponent<Door>();
            Configure(door, so =>
            {
                so.FindProperty("startOpen").boolValue = false;
                so.FindProperty("locked").boolValue = locked;
                so.FindProperty("hinge").objectReferenceValue = hinge;
                so.FindProperty("openAngle").floatValue = 95f;
                so.FindProperty("swingSpeed").floatValue = 180f;
                so.FindProperty("hasPeephole").boolValue = true;
                so.FindProperty("peepholeCamera").objectReferenceValue = cam;
                so.FindProperty("visitorStandPoint").objectReferenceValue = stand;
                so.FindProperty("defaultKnock.knocks").intValue = SandboxKnock.Knocks;
                so.FindProperty("defaultKnock.interval").floatValue = SandboxKnock.Interval;
                so.FindProperty("defaultKnock.answerWindow").floatValue = SandboxKnock.AnswerWindow;
            });
            return door;
        }

        // ================= player =================

        struct PlayerParts
        {
            public PlayerInputSource Input;
            public PlayerSanity Sanity;
            public PlayerInventory Inventory;
        }

        static PlayerParts BuildPlayer(Vector3 position)
        {
            var root = new GameObject("Player") { tag = "Player" };
            root.transform.position = position;

            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.minMoveDistance = 0f; // see PLAYTEST.md: the default 0.001 skips moves at very high frame rates

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 1.6f, 0f);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.depth = 0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
            camGo.AddComponent<AudioListener>();

            var input = root.AddComponent<InputSystemPlayerInput>();
            var inventory = root.AddComponent<PlayerInventory>();
            var stamina = root.AddComponent<PlayerStamina>();
            var sanity = root.AddComponent<PlayerSanity>();
            var sight = root.AddComponent<PlayerSight>();
            var movement = root.AddComponent<PlayerMovement>();
            var look = root.AddComponent<PlayerLook>();
            var interactor = root.AddComponent<PlayerInteractor>();
            var pickup = root.AddComponent<PlayerPickup>();
            var ritual = root.AddComponent<PlayerRitual>();
            var peephole = root.AddComponent<PlayerPeephole>();

            Configure(sight, so => so.FindProperty("viewCamera").objectReferenceValue = cam);
            Configure(movement, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("stamina").objectReferenceValue = stamina;
            });
            Configure(look, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("cameraPivot").objectReferenceValue = pivot;
                so.FindProperty("playerCamera").objectReferenceValue = cam;
            });
            Configure(interactor, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("sight").objectReferenceValue = sight;
            });
            Configure(pickup, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("sight").objectReferenceValue = sight;
                so.FindProperty("inventory").objectReferenceValue = inventory;
            });
            Configure(ritual, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("sight").objectReferenceValue = sight;
            });
            Configure(peephole, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("sight").objectReferenceValue = sight;
            });

            return new PlayerParts { Input = input, Sanity = sanity, Inventory = inventory };
        }

        // ================= NPCs =================

        static void MakeNeutral(string displayName, string id, Vector3 pos, Quaternion rot, DayPhaseMask phases,
                                ClueData clue, Transform parent, params string[] lines)
        {
            var root = new GameObject(displayName);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, rot);
            GameObject visual = Capsule("Visual", root.transform, "Neutral");

            var npc = root.AddComponent<NeutralNpc>();
            Configure(npc, so =>
            {
                so.FindProperty("npcId").stringValue = id;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("activePhases").intValue = (int)phases;
                so.FindProperty("canBeVisitor").boolValue = true;
                so.FindProperty("visualRoot").objectReferenceValue = visual;
                SetArray(so.FindProperty("lines"), lines);
                SetArray(so.FindProperty("cluesToGive"), new Object[] { clue });
                so.FindProperty("loopLines").boolValue = false;
                so.FindProperty("leaveIfIgnored").boolValue = true;
            });
        }

        static void MakeEnemy(Vector3 pos, Quaternion rot, Transform parent)
        {
            var root = new GameObject("The Blurred Man");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, rot);
            GameObject visual = Capsule("Visual", root.transform, "Enemy");
            visual.transform.localScale = new Vector3(0.8f, 1.05f, 0.8f);
            visual.transform.localPosition = new Vector3(0f, 1.05f, 0f);

            var enemy = root.AddComponent<EnemyNpc>(); // adds the NavMeshAgent (RequireComponent)
            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2.1f;
            Configure(enemy, so =>
            {
                so.FindProperty("npcId").stringValue = "enemy_blurred_man";
                so.FindProperty("displayName").stringValue = "The Blurred Man";
                so.FindProperty("activePhases").intValue = (int)DayPhaseMask.Night;
                so.FindProperty("canBeVisitor").boolValue = true;
                so.FindProperty("visualRoot").objectReferenceValue = visual;
            });
        }

        static GameObject Capsule(string name, Transform parent, string material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            go.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            // A "face" so you can tell which way it looks.
            LocalBox("Face", go.transform, new Vector3(0f, 0.55f, 0.45f), new Vector3(0.6f, 0.12f, 0.15f), "Table", false);
            return go;
        }

        // ================= items =================

        static void MakePickup(string displayName, string itemId, ClueData clue, Vector3 tablePos, string material,
                               Transform parent, Transform level)
        {
            GameObject go = Box(displayName, parent, new Vector3(tablePos.x, 0.86f, tablePos.z), new Vector3(0.22f, 0.22f, 0.22f), material);
            var item = go.AddComponent<PickupItem>();
            Configure(item, so =>
            {
                so.FindProperty("itemId").stringValue = itemId;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("clue").objectReferenceValue = clue;
                so.FindProperty("destroyOnPickup").boolValue = true;
            });
        }

        static void MakeAltar(Vector3 pos, Transform parent)
        {
            GameObject go = Box("Altar", parent, new Vector3(pos.x, 0.5f, pos.z), new Vector3(1.2f, 1f, 0.6f), "Altar");
            var ritual = go.AddComponent<RitualPoint>();
            Configure(ritual, so =>
            {
                so.FindProperty("ritualId").stringValue = "altar";
                so.FindProperty("prompt").stringValue = "Perform ritual (needs a candle)";
                so.FindProperty("duration").floatValue = 4f;
                so.FindProperty("requiredItemId").stringValue = "candle";
                so.FindProperty("consumeItem").boolValue = true;
                so.FindProperty("sanityCost").floatValue = 10f;
                so.FindProperty("flagOnComplete").stringValue = "ritual_done";
                so.FindProperty("oneShot").boolValue = true;
            });
        }

        static void MakeExit(Vector3 pos, Transform parent)
        {
            GameObject go = Box("Exit (trigger)", parent, pos, new Vector3(0.8f, 2f, 2.8f), "Exit");
            go.GetComponent<BoxCollider>().isTrigger = true;
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            go.AddComponent<SandboxExitTrigger>();
        }

        // ================= data =================

        static ClueData Clue(string id, string title, string description)
        {
            var clue = ScriptableObject.CreateInstance<ClueData>();
            Configure(clue, so =>
            {
                so.FindProperty("id").stringValue = id;
                so.FindProperty("title").stringValue = title;
                so.FindProperty("description").stringValue = description;
            });
            AssetDatabase.CreateAsset(clue, $"{DataDir}/Clue_{title.Replace("'", "").Replace(' ', '_')}.asset");
            return clue;
        }

        static EndingData Ending(string id, string title, string reason, int priority, string text,
                                 int minimumClues = 0, string[] requiredFlags = null)
        {
            var e = ScriptableObject.CreateInstance<EndingData>();
            Configure(e, so =>
            {
                so.FindProperty("id").stringValue = id;
                so.FindProperty("title").stringValue = title;
                so.FindProperty("text").stringValue = text;
                so.FindProperty("triggerReason").stringValue = reason;
                so.FindProperty("priority").intValue = priority;
                so.FindProperty("minimumClueCount").intValue = minimumClues;
                SetArray(so.FindProperty("requiredFlags"), requiredFlags ?? new string[0]);
            });
            AssetDatabase.CreateAsset(e, $"{DataDir}/Ending_{title.Replace("'", "").Replace(' ', '_')}.asset");
            return e;
        }

        // ================= NavMesh =================

        static void BakeNavMesh(GameObject level)
        {
            var surface = level.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if (!surface.navMeshData) throw new InvalidOperationException("NavMesh bake produced no data.");

            if (AssetDatabase.LoadAssetAtPath<Object>(NavMeshPath)) AssetDatabase.DeleteAsset(NavMeshPath);
            surface.navMeshData.name = "NateSandbox-NavMesh";
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            EditorUtility.SetDirty(surface);
        }

        // ================= helpers =================

        static GameObject Box(string name, Transform parent, Vector3 worldCenter, Vector3 size, string material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = worldCenter;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            return go;
        }

        static GameObject LocalBox(string name, Transform parent, Vector3 localCenter, Vector3 localSize, string material,
                                   bool keepCollider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localScale = localSize;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            { "Floor", new Color(0.35f, 0.35f, 0.35f) },
            { "Wall", new Color(0.62f, 0.62f, 0.6f) },
            { "Door", new Color(0.45f, 0.3f, 0.18f) },
            { "Table", new Color(0.3f, 0.22f, 0.15f) },
            { "Key", new Color(1f, 0.8f, 0.1f) },
            { "Candle", new Color(0.95f, 0.95f, 0.85f) },
            { "Photo", new Color(0.3f, 0.9f, 0.9f) },
            { "Diary", new Color(0.9f, 0.4f, 0.6f) },
            { "Altar", new Color(0.45f, 0.2f, 0.6f) },
            { "Neutral", new Color(0.2f, 0.45f, 0.95f) },
            { "Enemy", new Color(0.85f, 0.1f, 0.1f) },
            { "Exit", new Color(0.2f, 0.85f, 0.3f) }
        };

        static Material Mat(string name)
        {
            if (Materials.TryGetValue(name, out Material m)) return m;
            Shader shader = Shader.Find("Standard");
            if (!shader) throw new InvalidOperationException("Standard shader not found.");
            m = new Material(shader) { name = "Sandbox_" + name, color = Colors[name] };
            if (name == "Exit") m.EnableKeyword("_EMISSION");
            if (name == "Exit") m.SetColor("_EmissionColor", Colors[name] * 0.6f);
            AssetDatabase.CreateAsset(m, $"{MaterialDir}/Sandbox_{name}.mat");
            Materials[name] = m;
            return m;
        }

        static void Configure(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(SerializedProperty array, IList<Object> values)
        {
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void SetArray(SerializedProperty array, IList<string> values)
        {
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
