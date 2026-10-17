using System;
using UnityEngine;
using UnityEngine.AI;

namespace BlurredRoom
{
    /// <summary>
    /// Enemy: idles until it sees the player, chases on the NavMesh, catches within range.
    /// Drains sanity while the player is looking at it.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyNpc : NpcBase
    {
        public enum EnemyState { Idle, Chase, Caught }

        [Header("Target")]
        [Tooltip("Player root. Empty = object tagged 'Player'.")]
        [SerializeField] Transform player;

        [Header("Detection")]
        [SerializeField] float sightRange = 14f;
        [SerializeField, Range(10f, 360f)] float sightAngle = 120f;
        [SerializeField] float eyeHeight = 1.6f;
        [SerializeField] float loseTargetAfter = 6f;

        [Header("Chase")]
        [SerializeField] float chaseSpeed = 3.6f;
        [SerializeField] float catchRange = 1.4f;
        [SerializeField] float repathInterval = 0.2f;

        [Header("Sanity")]
        [Tooltip("Sanity per second drained while the player can see this enemy.")]
        [SerializeField] float seenSanityDrain = 6f;
        [SerializeField] float seenMaxDistance = 20f;
        [Tooltip("One-off sanity loss when the player opens the door to this enemy.")]
        [SerializeField] float doorOpenedSanityHit = 15f;

        NavMeshAgent agent;
        PlayerSanity sanity;
        PlayerSight playerSight;
        float lostTimer;
        float repathTimer;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool IsSeenByPlayer { get; private set; }
        public override NpcKind Kind => NpcKind.Enemy;
        public override bool IsBusy => State != EnemyState.Idle;

        public event Action<EnemyState> StateChanged;

        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
        }

        protected override void Start()
        {
            ResolvePlayer();
            base.Start();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetSeen(false);
        }

        void ResolvePlayer()
        {
            if (!player)
            {
                GameObject go = GameObject.FindWithTag("Player");
                if (go) player = go.transform.root;
            }
            if (player)
            {
                sanity = player.FindInRoot<PlayerSanity>();
                playerSight = player.FindInRoot<PlayerSight>();
            }
        }

        void Update()
        {
            if (!IsActive || !player)
            {
                SetSeen(false);
                return;
            }

            SetSeen(CheckSeenByPlayer());

            switch (State)
            {
                case EnemyState.Idle:
                    if (CanSeePlayer()) SetState(EnemyState.Chase);
                    break;
                case EnemyState.Chase:
                    ChaseTick();
                    break;
            }
        }

        void ChaseTick()
        {
            if (CanSeePlayer()) lostTimer = 0f;
            else lostTimer += Time.deltaTime;

            if (lostTimer >= loseTargetAfter)
            {
                SetState(EnemyState.Idle);
                return;
            }

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f && agent.isOnNavMesh)
            {
                repathTimer = repathInterval;
                agent.SetDestination(player.position);
            }

            if (Vector3.Distance(transform.position, player.position) <= catchRange)
            {
                SetState(EnemyState.Caught);
                NpcRegistry.RaisePlayerCaught(this);
            }
        }

        public void SetState(EnemyState state)
        {
            if (State == state) return;
            State = state;
            lostTimer = 0f;
            repathTimer = 0f;

            if (agent.isOnNavMesh)
            {
                agent.speed = chaseSpeed;
                agent.isStopped = state != EnemyState.Chase;
                if (state != EnemyState.Chase) agent.ResetPath();
            }
            StateChanged?.Invoke(state);
        }

        /// <summary>Send the enemy back to idle (e.g. after a cutscene or respawn).</summary>
        public void ResetToIdle() => SetState(EnemyState.Idle);

        public bool CanSeePlayer()
        {
            if (!player) return false;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.position + Vector3.up * 1f;
            Vector3 toPlayer = target - eye;
            if (toPlayer.magnitude > sightRange) return false;

            Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (flat.sqrMagnitude > 0.001f && Vector3.Angle(transform.forward, flat) > sightAngle * 0.5f) return false;

            return SightUtil.HasLineOfSight(eye, target, transform, player);
        }

        bool CheckSeenByPlayer()
        {
            Camera cam = playerSight ? playerSight.ViewCamera : null;
            if (!cam || !cam.isActiveAndEnabled) return false;

            Vector3 chest = transform.position + Vector3.up * (eyeHeight * 0.75f);
            if (Vector3.Distance(cam.transform.position, chest) > seenMaxDistance) return false;

            Vector3 vp = cam.WorldToViewportPoint(chest);
            if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) return false;

            return SightUtil.HasLineOfSight(cam.transform.position, chest, player, transform);
        }

        void SetSeen(bool seen)
        {
            if (IsSeenByPlayer == seen) return;
            IsSeenByPlayer = seen;
            if (!sanity) return;
            if (seen) sanity.SetDrain(this, seenSanityDrain);
            else sanity.ClearDrain(this);
        }

        protected override void OnActiveChanged(bool active)
        {
            if (!active)
            {
                SetState(EnemyState.Idle);
                SetSeen(false);
            }
        }

        public override void Appear(Vector3 position, Quaternion rotation)
        {
            if (agent && agent.enabled) agent.Warp(position);
            transform.SetPositionAndRotation(position, rotation);
            SetActiveState(true);
        }

        public override void OnDoorAnswered(Door door, DoorAnswer answer)
        {
            if (answer != DoorAnswer.Opened) return;
            Transform p = door.VisitorStandPoint;
            Appear(p.position, p.rotation);
            if (sanity && doorOpenedSanityHit > 0f) sanity.Drain(doorOpenedSanityHit);
            SetState(EnemyState.Chase);
        }
    }
}
