using UnityEngine;

namespace BlurredRoom
{
    /// <summary>First-person camera. Yaw turns this body, pitch turns the camera pivot. Uses GameSettings.</summary>
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] PlayerInputSource input;
        [SerializeField] Transform cameraPivot;
        [SerializeField] Camera playerCamera;
        [SerializeField] float degreesPerUnit = 0.1f;
        [SerializeField] float minPitch = -85f;
        [SerializeField] float maxPitch = 85f;
        [SerializeField] bool lockCursor = true;

        float pitch;

        void Awake()
        {
            if (!input) input = GetComponentInParent<PlayerInputSource>();
            if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();
            if (!cameraPivot && playerCamera) cameraPivot = playerCamera.transform;
        }

        void OnEnable()
        {
            GameSettings.Changed += ApplySettings;
            ApplySettings();
            if (lockCursor) SetCursorLocked(true);
        }

        void OnDisable() => GameSettings.Changed -= ApplySettings;

        void Update()
        {
            if (!input || input.MovementLocked || !cameraPivot) return;

            Vector2 look = input.Look * (GameSettings.LookSensitivity * degreesPerUnit);
            transform.Rotate(0f, look.x, 0f, Space.Self);
            pitch += GameSettings.InvertY ? look.y : -look.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void ApplySettings()
        {
            if (playerCamera) playerCamera.fieldOfView = GameSettings.FieldOfView;
        }
    }
}
