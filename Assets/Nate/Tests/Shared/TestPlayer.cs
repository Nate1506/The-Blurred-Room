using System;
using UnityEngine;

namespace BlurredRoom.Tests
{
    /// <summary>
    /// A test player: CharacterController root (tagged Player), camera child at eye height, and every Player* script
    /// driven by FakePlayerInput. Movement is disabled unless asked for, so the player doesn't fall in sight tests.
    /// </summary>
    public class TestPlayer
    {
        public const float EyeHeight = 1.6f;

        public GameObject Root;
        public CharacterController Controller;
        public Camera Camera;
        public FakePlayerInput Input;
        public PlayerInventory Inventory;
        public PlayerStamina Stamina;
        public PlayerSanity Sanity;
        public PlayerMovement Movement;
        public PlayerLook Look;
        public PlayerSight Sight;
        public PlayerInteractor Interactor;
        public PlayerPickup Pickup;
        public PlayerRitual Ritual;
        public PlayerPeephole Peephole;

        public Transform Transform => Root.transform;

        /// <param name="configure">Runs before Awake, while the root is inactive: set serialized fields here.</param>
        public static TestPlayer Build(Vector3 position, Quaternion rotation, bool enableMovement = false,
                                       Action<TestPlayer> configure = null)
        {
            var p = new TestPlayer();
            p.Root = new GameObject("TestPlayer");
            p.Root.SetActive(false);
            p.Root.tag = "Player";
            p.Root.transform.SetPositionAndRotation(position, rotation);

            p.Controller = p.Root.AddComponent<CharacterController>();
            p.Controller.center = new Vector3(0f, 1f, 0f);
            p.Controller.height = 2f;
            p.Controller.radius = 0.4f;

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(p.Root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            p.Camera = camGo.AddComponent<Camera>();

            p.Input = p.Root.AddComponent<FakePlayerInput>();
            p.Inventory = p.Root.AddComponent<PlayerInventory>();
            p.Stamina = p.Root.AddComponent<PlayerStamina>();
            p.Sanity = p.Root.AddComponent<PlayerSanity>();
            p.Sight = p.Root.AddComponent<PlayerSight>();
            p.Movement = p.Root.AddComponent<PlayerMovement>();
            p.Look = p.Root.AddComponent<PlayerLook>();
            p.Interactor = p.Root.AddComponent<PlayerInteractor>();
            p.Pickup = p.Root.AddComponent<PlayerPickup>();
            p.Ritual = p.Root.AddComponent<PlayerRitual>();
            p.Peephole = p.Root.AddComponent<PlayerPeephole>();

            TestReflection.Set(p.Look, "lockCursor", false);
            configure?.Invoke(p);

            p.Root.SetActive(true);
            p.Movement.enabled = enableMovement;
            return p;
        }
    }
}
