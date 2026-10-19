using UnityEngine;

namespace CyberpunkSubwaySurfer
{
    /// <summary>
    /// Cyberpunk drone enemy that runs on the same track as the player.
    /// It chooses a random lane and speeds toward the player. If the player is
    /// immortal the enemy is only slowed; otherwise touching it kills the player.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CyberEnemy : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 10f;
        public float laneDistance = 2.8f;
        public int lane = 1; // 0-Left, 1-Center, 2-Right

        [Header("Visuals")]
        public float hoverAmplitude = 0.25f;
        public float hoverFrequency = 3f;
        public Color glowColor = new Color(1f, 0.1f, 0.1f);

        private CharacterController controller;
        private float baseY;
        private bool isDead = false;
        private float lifetime = 20f; // Auto-destroy after 20 s

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            baseY = transform.position.y;
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f) { Destroy(gameObject); return; }
            if (isDead) return;

            // Hover up/down
            float hoverY = baseY + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            float targetX = (lane - 1) * laneDistance;
            Vector3 pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, targetX, 8f * Time.deltaTime);
            pos.y = hoverY;

            // Move backward (toward player) — enemy travels in -Z direction relative to its spawn
            Vector3 motion = new Vector3(pos.x - transform.position.x, pos.y - transform.position.y,
                                         -speed * Time.deltaTime);
            controller.Move(motion);

            // Kill enemy that goes way past the player
            CyberPlayerController player = FindFirstObjectByType<CyberPlayerController>();
            if (player != null && transform.position.z < player.transform.position.z - 30f)
            {
                Destroy(gameObject);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (isDead) return;
            HandlePlayerContact(hit.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDead) return;
            HandlePlayerContact(other.gameObject);
        }

        private void HandlePlayerContact(GameObject go)
        {
            CyberPlayerController player = go.GetComponent<CyberPlayerController>();
            if (player == null) player = go.GetComponentInParent<CyberPlayerController>();
            if (player == null) return;

            if (CyberGameManager.Instance != null && CyberGameManager.Instance.isImmortal)
            {
                // Immortal player destroys the enemy
                Die();
            }
            else
            {
                // Enemy kills the player
                player.Crash();
                Die();
            }
        }

        public void SlowDown(float duration)
        {
            if (gameObject.activeSelf)
                StartCoroutine(SlowCoroutine(duration));
        }

        private System.Collections.IEnumerator SlowCoroutine(float duration)
        {
            float original = speed;
            speed = original * 0.3f;
            yield return new WaitForSeconds(duration);
            speed = original;
        }

        public void Die()
        {
            isDead = true;
            Destroy(gameObject);
        }

        /// <summary>Build the enemy's visual body at runtime.</summary>
        public static CyberEnemy SpawnAt(Vector3 position, int lane)
        {
            // Root
            GameObject root = new GameObject("CyberEnemy");
            root.transform.position = position;
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 0.6f, 0);

            CyberEnemy enemy = root.AddComponent<CyberEnemy>();
            enemy.lane = lane;
            enemy.speed = 10f;

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "EnemyBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 0.7f, 0);
            body.transform.localScale = new Vector3(1.0f, 0.55f, 1.6f);
            Destroy(body.GetComponent<BoxCollider>());

            // Red glow material
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.9f, 0.05f, 0.1f);
            mat.SetColor("_EmissionColor", new Color(1f, 0.05f, 0.1f) * 2f);
            mat.EnableKeyword("_EMISSION");
            body.GetComponent<Renderer>().material = mat;

            // Wings
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wing.name = "EnemyWing";
                wing.transform.SetParent(root.transform);
                wing.transform.localPosition = new Vector3(s * 0.75f, 0.7f, 0f);
                wing.transform.localScale = new Vector3(0.5f, 0.08f, 0.9f);
                wing.transform.localEulerAngles = new Vector3(0, 0, s * 12f);
                Destroy(wing.GetComponent<BoxCollider>());

                Material wm = new Material(mat);
                wing.GetComponent<Renderer>().material = wm;
            }

            // Eye (front light)
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "EnemyEye";
            eye.transform.SetParent(root.transform);
            eye.transform.localPosition = new Vector3(0, 0.72f, 0.82f);
            eye.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Destroy(eye.GetComponent<Collider>());
            Material em = new Material(Shader.Find("Standard"));
            em.color = Color.white;
            em.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.3f) * 4f);
            em.EnableKeyword("_EMISSION");
            eye.GetComponent<Renderer>().material = em;

            // Trigger so it can touch the player
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;
            trigger.center = new Vector3(0, 0.7f, 0);

            return enemy;
        }
    }
}
