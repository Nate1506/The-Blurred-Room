using UnityEngine;

namespace BlurredRoom.Sandbox
{
    /// <summary>Trigger box: when the player walks in, try to end the game (default reason: Solved).</summary>
    [RequireComponent(typeof(Collider))]
    public class SandboxExitTrigger : MonoBehaviour
    {
        [SerializeField] string reason = EndingReasons.Solved;

        void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<PlayerInputSource>()) return;
            if (EndingManager.Instance) EndingManager.Instance.TryEnd(reason);
            else Debug.LogWarning("[Sandbox] Exit reached but there is no EndingManager.", this);
        }
    }
}
