using UnityEngine;

namespace CyberpunkSubwaySurfer
{
    public enum CoinType { Yellow, Red, Blue }

    public class CyberCoin : MonoBehaviour
    {
        [Header("Coin Type")]
        public CoinType coinType = CoinType.Yellow;
        public int coinValue = 1;

        [Header("Coin Animation")]
        public float rotateSpeed = 180f;
        public float bobHeight = 0.2f;
        public float bobSpeed = 4f;

        [Header("Magnetism")]
        public float magnetDistance = 5f;
        public float magnetSpeed = 18f;

        private Vector3 startLocalPos;
        private Transform playerTransform;
        private bool isCollected = false;

        // Colours for each type
        private static readonly Color ColYellow = new Color(1.00f, 0.85f, 0.10f);
        private static readonly Color ColRed    = new Color(1.00f, 0.12f, 0.25f);
        private static readonly Color ColBlue   = new Color(0.10f, 0.70f, 1.00f);

        private void Start()
        {
            startLocalPos = transform.localPosition;

            // Tint all renderers to reflect the coin type (no neon emission)
            Color c = GetTypeColor();
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                // Use MaterialPropertyBlock so we don't create unique material instances
                MaterialPropertyBlock pb = new MaterialPropertyBlock();
                r.GetPropertyBlock(pb);
                pb.SetColor("_Color", c);
                r.SetPropertyBlock(pb);
            }

            CyberPlayerController player = FindFirstObjectByType<CyberPlayerController>();
            if (player != null) playerTransform = player.transform;
        }

        private void Update()
        {
            if (isCollected) return;

            // Rotation
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // Bobbing
            float newY = startLocalPos.y + Mathf.Sin((Time.time + transform.position.z * 0.1f) * bobSpeed) * bobHeight;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);

            // Magnetic attraction — only within same lane
            if (playerTransform != null)
            {
                float lateralDist = Mathf.Abs(transform.position.x - playerTransform.position.x);
                if (lateralDist <= 0.8f)
                {
                    float forwardDist = transform.position.z - playerTransform.position.z;
                    if (forwardDist >= -0.5f && forwardDist < 2.5f)
                    {
                        Vector3 targetPos = playerTransform.position + Vector3.up * 1.0f;
                        transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetSpeed * Time.deltaTime);
                        if (Vector3.Distance(transform.position, targetPos) < 1.0f)
                            Collect();
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            CyberPlayerController player = other.GetComponent<CyberPlayerController>();
            if (player == null) player = other.GetComponentInParent<CyberPlayerController>();
            if (player != null)
            {
                if (Mathf.Abs(transform.position.x - player.transform.position.x) <= 1.0f)
                    Collect();
            }
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            if (CyberGameManager.Instance != null)
            {
                CyberGameManager.Instance.AddCoinByType(coinType, coinValue);
            }

            Destroy(gameObject);
        }

        public Color GetTypeColor()
        {
            switch (coinType)
            {
                case CoinType.Red:  return ColRed;
                case CoinType.Blue: return ColBlue;
                default:            return ColYellow;
            }
        }
    }
}
