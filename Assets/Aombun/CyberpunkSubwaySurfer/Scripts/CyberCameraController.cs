using UnityEngine;

namespace CyberpunkSubwaySurfer
{
    public class CyberCameraController : MonoBehaviour
    {
        [Header("Follow Target")]
        public Transform target;
        public CyberPlayerController playerController;

        [Header("Camera Offset")]
        public float distance = 6.4f;
        public float height = 3.6f;
        public float horizontalSmoothness = 10f;
        public float verticalSmoothness = 8f;
        public Vector3 lookAheadOffset = new Vector3(0f, 1.2f, 6.0f);

        [Header("Speed Dynamics")]
        public float baseFOV = 64f;
        public float maxFOV = 76f;
        private Camera cam;

        [Header("Screen Shake")]
        private float shakeIntensity = 0f;
        private float shakeTimer = 0f;

        private Vector3 currentVelocity;
        private float currentX;

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (target == null)
            {
                CyberPlayerController player = FindFirstObjectByType<CyberPlayerController>();
                if (player != null)
                {
                    target = player.transform;
                    playerController = player;
                }
            }

            if (target != null)
            {
                currentX = target.position.x;
                transform.position = CalculateTargetPosition();
                transform.LookAt(target.position + lookAheadOffset);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Smooth X movement separately for cinematic lane transition feel
            currentX = Mathf.Lerp(currentX, target.position.x * 0.75f, Time.deltaTime * horizontalSmoothness);

            Vector3 desiredPos = new Vector3(currentX, target.position.y + height, target.position.z - distance);

            // Apply screen shake offset
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;
                desiredPos += shakeOffset;
            }

            transform.position = desiredPos;

            // Look ahead down the track
            Vector3 lookTarget = new Vector3(currentX * 0.5f, target.position.y + lookAheadOffset.y, target.position.z + lookAheadOffset.z);
            transform.LookAt(lookTarget);

            // Dynamic FOV based on speed
            if (cam != null && playerController != null)
            {
                float speedT = Mathf.InverseLerp(playerController.baseForwardSpeed, playerController.maxForwardSpeed, playerController.currentForwardSpeed);
                cam.fieldOfView = Mathf.Lerp(baseFOV, maxFOV, speedT);
            }
        }

        public void TriggerShake(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeTimer = duration;
        }

        private Vector3 CalculateTargetPosition()
        {
            return new Vector3(target.position.x * 0.75f, target.position.y + height, target.position.z - distance);
        }
    }
}
