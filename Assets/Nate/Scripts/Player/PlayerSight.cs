using UnityEngine;

namespace BlurredRoom
{
    /// <summary>One raycast per frame from the player camera, shared by interact / pickup / ritual / peephole.</summary>
    public class PlayerSight : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] float range = 2.5f;
        [SerializeField] LayerMask layerMask = ~0;

        int lastFrame = -1;
        bool hasHit;
        RaycastHit hit;

        public Camera ViewCamera => viewCamera;
        public float Range => range;
        public bool HasHit { get { Refresh(); return hasHit; } }
        public RaycastHit Hit { get { Refresh(); return hit; } }

        void Awake()
        {
            if (!viewCamera) viewCamera = GetComponentInChildren<Camera>();
        }

        /// <summary>Component of type T (class or interface) on whatever the player is looking at, or null.</summary>
        public T Get<T>() where T : class
        {
            Refresh();
            return hasHit && hit.collider ? hit.collider.GetComponentInParent<T>() : null;
        }

        void Refresh()
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (!viewCamera) { hasHit = false; return; }
            Transform t = viewCamera.transform;
            hasHit = Physics.Raycast(t.position, t.forward, out hit, range, layerMask, QueryTriggerInteraction.Ignore);
        }
    }
}
