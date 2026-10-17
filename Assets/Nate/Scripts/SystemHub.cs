using UnityEngine;

namespace BlurredRoom
{
    /// <summary>
    /// The "System" node: connects the separate systems without them knowing about each other.
    /// Sanity 0 → ending, enemy catches player → ending, last day passed → ending, ending → lock player.
    /// Every link is optional; anything not assigned is skipped.
    /// </summary>
    public class SystemHub : MonoBehaviour
    {
        [SerializeField] PlayerInputSource playerInput;
        [SerializeField] PlayerSanity playerSanity;
        [Tooltip("After this day ends the game ends with TimeUp. 0 = no limit.")]
        [SerializeField] int lastDay = 0;
        [SerializeField] bool lockPlayerOnEnding = true;

        TimeOfDayManager time;
        EndingManager endings;

        void Start()
        {
            time = TimeOfDayManager.Instance;
            endings = EndingManager.Instance;

            if (playerSanity) playerSanity.Depleted += OnSanityDepleted;
            NpcRegistry.PlayerCaught += OnPlayerCaught;
            if (time) time.DayChanged += OnDayChanged;
            if (endings) endings.EndingReached += OnEndingReached;
        }

        void OnDestroy()
        {
            if (playerSanity) playerSanity.Depleted -= OnSanityDepleted;
            NpcRegistry.PlayerCaught -= OnPlayerCaught;
            if (time) time.DayChanged -= OnDayChanged;
            if (endings) endings.EndingReached -= OnEndingReached;
        }

        void OnSanityDepleted() => End(EndingReasons.SanityLost);
        void OnPlayerCaught(EnemyNpc enemy) => End(EndingReasons.Caught);

        void OnDayChanged(int day)
        {
            if (lastDay > 0 && day > lastDay) End(EndingReasons.TimeUp);
        }

        void OnEndingReached(EndingData ending, string reason)
        {
            if (lockPlayerOnEnding && playerInput) playerInput.LockMovement(this);
            if (time) time.Paused = true;
        }

        void End(string reason)
        {
            if (endings) endings.TryEnd(reason);
        }
    }
}
