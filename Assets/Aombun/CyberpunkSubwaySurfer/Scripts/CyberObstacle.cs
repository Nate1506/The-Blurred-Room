using UnityEngine;

namespace CyberpunkSubwaySurfer
{
    public enum ObstacleType
    {
        HighBarrier,  // Must jump over (or change lane)
        LowLaser,     // Must slide under (or change lane)
        TrainHull,    // Solid collision with train side
        SolidPillar   // Solid lane obstruction
    }

    public class CyberObstacle : MonoBehaviour
    {
        [Header("Obstacle Settings")]
        public ObstacleType obstacleType = ObstacleType.HighBarrier;
        public float obstacleHeight = 1.0f;

        private void OnTriggerEnter(Collider other)
        {
            CyberPlayerController player = other.GetComponent<CyberPlayerController>();
            if (player == null) player = other.GetComponentInParent<CyberPlayerController>();
            if (player != null)
            {
                player.OnHitObstacle(this);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            CyberPlayerController player = collision.collider.GetComponent<CyberPlayerController>();
            if (player == null) player = collision.collider.GetComponentInParent<CyberPlayerController>();
            if (player != null)
            {
                player.OnHitObstacle(this);
            }
        }
    }
}
