using UnityEngine;

namespace CyberpunkSubwaySurfer
{
    public enum HologramType
    {
        FloatingCreature, // Holographic Koi / cyber creature swimming through skyline
        RotatingOrb,      // Rotating orbital cyber rings or globe
        PulseBeacon,      // High-altitude floating holo-sign / emblem
        ScanningProjector // Upward projecting hologram light cone
    }

    /// <summary>
    /// Animates high-altitude decorative cyberpunk holograms with swimming undulations,
    /// orbital rotations, and scanline bobbing without obstructing the player's road view.
    /// </summary>
    public class CyberHologram : MonoBehaviour
    {
        public HologramType type = HologramType.RotatingOrb;
        public float rotateSpeed = 25f;
        public float bobAmplitude = 1.2f;
        public float bobFrequency = 0.8f;
        public Transform tailTransform; // Optional child transform for swimming tail fin wag

        private Vector3 startLocalPos;
        private float seed;

        private void Start()
        {
            startLocalPos = transform.localPosition;
            seed = Random.value * 10f;
        }

        private void Update()
        {
            float t = Time.time + seed;

            switch (type)
            {
                case HologramType.FloatingCreature:
                    // Swimming undulation high above the city
                    float bobY = Mathf.Sin(t * bobFrequency) * bobAmplitude;
                    float swayX = Mathf.Cos(t * bobFrequency * 0.7f) * (bobAmplitude * 0.8f);
                    transform.localPosition = startLocalPos + new Vector3(swayX, bobY, 0f);

                    // Gentle banking and body pitch
                    transform.localRotation = Quaternion.Euler(
                        Mathf.Sin(t * bobFrequency) * 5f,
                        Mathf.Sin(t * 0.35f) * 8f,
                        -swayX * 3.5f
                    );

                    // Tail fin wag
                    if (tailTransform != null)
                    {
                        tailTransform.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * bobFrequency * 2.8f) * 28f, 0f);
                    }
                    break;

                case HologramType.RotatingOrb:
                    transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);
                    transform.Rotate(Vector3.right, (rotateSpeed * 0.35f) * Time.deltaTime, Space.Self);
                    transform.localPosition = startLocalPos + new Vector3(0f, Mathf.Sin(t * 1.2f) * 0.5f, 0f);
                    break;

                case HologramType.PulseBeacon:
                    transform.Rotate(Vector3.up, (rotateSpeed * 0.4f) * Time.deltaTime, Space.World);
                    transform.localPosition = startLocalPos + new Vector3(0f, Mathf.Sin(t * 0.8f) * 0.4f, 0f);
                    break;

                case HologramType.ScanningProjector:
                    transform.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.2f) * 10f, 0f, Mathf.Cos(t * 0.9f) * 10f);
                    break;
            }
        }
    }
}
