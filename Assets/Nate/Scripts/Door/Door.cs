using System;
using System.Collections;
using UnityEngine;

namespace BlurredRoom
{
    public enum DoorAnswer { Opened, Ignored }

    [Serializable]
    public class KnockPattern
    {
        [Min(1)] public int knocks = 3;
        [Min(0f)] public float interval = 0.6f;
        [Tooltip("Seconds the visitor waits after knocking before giving up (= Don't open).")]
        [Min(0f)] public float answerWindow = 10f;
    }

    /// <summary>
    /// Door: open/close, locked, knocking visitors, peephole.
    /// Flow: BeginVisit(npc) → knocks → player can look through the peephole →
    /// Open (Answered: Opened) or Don't open / wait it out (Answered: Ignored).
    /// </summary>
    public class Door : MonoBehaviour, IInteractable
    {
        [Header("State")]
        [SerializeField] bool startOpen;
        [SerializeField] bool locked;

        [Header("Motion (optional)")]
        [Tooltip("Rotates around local Y. Leave empty if animation is handled elsewhere (listen to Opened/Closed).")]
        [SerializeField] Transform hinge;
        [SerializeField] float openAngle = 95f;
        [SerializeField] float swingSpeed = 180f;

        [Header("Peephole")]
        [SerializeField] bool hasPeephole = true;
        [Tooltip("Optional camera outside the door. Keep its Depth higher than the player camera; it is enabled only while viewing.")]
        [SerializeField] Camera peepholeCamera;

        [Header("Visitors")]
        [Tooltip("Where a visitor appears when the door is opened.")]
        [SerializeField] Transform visitorStandPoint;
        [SerializeField] KnockPattern defaultKnock = new KnockPattern();

        [Header("Audio (optional)")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip knockClip;
        [SerializeField] AudioClip openClip;
        [SerializeField] AudioClip closeClip;
        [SerializeField] AudioClip lockedClip;

        Quaternion closedRotation;
        float angle;
        Coroutine visitRoutine;

        public bool IsOpen { get; private set; }
        public bool IsLocked => locked;
        public bool HasPeephole => hasPeephole;
        public bool IsPeepholeInUse { get; private set; }
        /// <summary>Who is outside right now (null = nobody). Check this from the peephole UI.</summary>
        public NpcBase Visitor { get; private set; }
        public bool HasPendingVisitor => Visitor != null;
        public bool IsKnocking { get; private set; }
        public Transform VisitorStandPoint => visitorStandPoint ? visitorStandPoint : transform;

        public event Action Opened;
        public event Action Closed;
        public event Action LockedAttempt;
        public event Action<bool> LockChanged;
        /// <summary>(knockIndex starting at 1, totalKnocks)</summary>
        public event Action<int, int> Knocked;
        public event Action<NpcBase> VisitStarted;
        public event Action<NpcBase, DoorAnswer> Answered;
        public event Action<bool> PeepholeChanged;

        public string Prompt
        {
            get
            {
                if (IsOpen) return "Close";
                if (locked) return "Locked";
                return HasPendingVisitor ? "Open the door" : "Open";
            }
        }

        void Awake()
        {
            if (hinge) closedRotation = hinge.localRotation;
            IsOpen = startOpen;
            angle = startOpen ? openAngle : 0f;
            ApplyHinge();
            if (peepholeCamera) peepholeCamera.enabled = false;
        }

        void Update()
        {
            if (!hinge) return;
            float target = IsOpen ? openAngle : 0f;
            if (Mathf.Approximately(angle, target)) return;
            angle = Mathf.MoveTowards(angle, target, swingSpeed * Time.deltaTime);
            ApplyHinge();
        }

        void ApplyHinge()
        {
            if (hinge) hinge.localRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
        }

        // ---------- IInteractable ----------

        public bool CanInteract(GameObject interactor) => !IsPeepholeInUse;

        public void Interact(GameObject interactor)
        {
            if (IsOpen) Close();
            else TryOpen();
        }

        // ---------- Open / close / lock ----------

        public bool TryOpen()
        {
            if (IsOpen) return true;
            if (locked)
            {
                Play(lockedClip);
                LockedAttempt?.Invoke();
                return false;
            }

            IsOpen = true;
            Play(openClip);
            Opened?.Invoke();
            if (HasPendingVisitor) Resolve(DoorAnswer.Opened);
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Play(closeClip);
            Closed?.Invoke();
        }

        public void SetLocked(bool value)
        {
            if (locked == value) return;
            locked = value;
            LockChanged?.Invoke(locked);
        }

        // ---------- Visitors / knocking ----------

        /// <summary>An NPC comes to this door and knocks. Returns false if the door is busy or open.</summary>
        public bool BeginVisit(NpcBase visitor, KnockPattern pattern = null)
        {
            if (!visitor || HasPendingVisitor || IsOpen) return false;
            Visitor = visitor;
            visitor.VisitingDoor = this;
            VisitStarted?.Invoke(visitor);
            visitRoutine = StartCoroutine(VisitRoutine(pattern ?? defaultKnock));
            return true;
        }

        /// <summary>Knock again (e.g. an impatient visitor) without starting a new visit.</summary>
        public void KnockAgain(KnockPattern pattern = null)
        {
            if (!HasPendingVisitor) return;
            if (visitRoutine != null) StopCoroutine(visitRoutine);
            visitRoutine = StartCoroutine(VisitRoutine(pattern ?? defaultKnock));
        }

        /// <summary>Player chooses "Don't open".</summary>
        public void DeclineVisitor()
        {
            if (HasPendingVisitor) Resolve(DoorAnswer.Ignored);
        }

        IEnumerator VisitRoutine(KnockPattern p)
        {
            IsKnocking = true;
            for (int i = 1; i <= p.knocks; i++)
            {
                Play(knockClip);
                Knocked?.Invoke(i, p.knocks);
                if (i < p.knocks) yield return new WaitForSeconds(p.interval);
            }
            IsKnocking = false;

            yield return new WaitForSeconds(p.answerWindow);
            visitRoutine = null;
            if (HasPendingVisitor) Resolve(DoorAnswer.Ignored);
        }

        void Resolve(DoorAnswer answer)
        {
            if (visitRoutine != null)
            {
                StopCoroutine(visitRoutine);
                visitRoutine = null;
            }
            IsKnocking = false;

            NpcBase visitor = Visitor;
            Visitor = null;
            if (visitor) visitor.VisitingDoor = null;
            Answered?.Invoke(visitor, answer);
            if (visitor) visitor.OnDoorAnswered(this, answer);
        }

        // ---------- Peephole ----------

        /// <summary>Called by PlayerPeephole.</summary>
        public void SetPeepholeInUse(bool inUse)
        {
            if (IsPeepholeInUse == inUse) return;
            IsPeepholeInUse = inUse;
            if (peepholeCamera) peepholeCamera.enabled = inUse;
            PeepholeChanged?.Invoke(inUse);
        }

        void Play(AudioClip clip)
        {
            if (audioSource && clip) audioSource.PlayOneShot(clip);
        }
    }
}
