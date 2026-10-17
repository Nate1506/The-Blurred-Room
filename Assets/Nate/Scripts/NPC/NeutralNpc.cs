using System;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>
    /// Neutral NPC (residents, visitors). Talk with Interact to go through lines; can hand out clues.
    /// Dialogue UI listens to LineSpoken.
    /// </summary>
    public class NeutralNpc : NpcBase, IInteractable
    {
        [SerializeField, TextArea] string[] lines = new string[0];
        [Tooltip("Clues given after the last line has been spoken.")]
        [SerializeField] ClueData[] cluesToGive = new ClueData[0];
        [SerializeField] bool loopLines = false;
        [Tooltip("Hide again after the player ignores the door.")]
        [SerializeField] bool leaveIfIgnored = true;

        int lineIndex;

        public override NpcKind Kind => NpcKind.Neutral;
        public string Prompt => "Talk";
        public bool HasMoreLines => lines.Length > 0 && (loopLines || lineIndex < lines.Length);

        /// <summary>(npc, line)</summary>
        public event Action<NeutralNpc, string> LineSpoken;
        public event Action<NeutralNpc> ConversationFinished;
        /// <summary>Raised when the player ignored this NPC at the door.</summary>
        public event Action<NeutralNpc, Door> Ignored;

        public bool CanInteract(GameObject interactor) => IsActive;

        public void Interact(GameObject interactor) => SpeakNext();

        public void SpeakNext()
        {
            if (lines.Length == 0)
            {
                Finish();
                return;
            }

            if (lineIndex >= lines.Length)
            {
                if (!loopLines) return;
                lineIndex = 0;
            }

            LineSpoken?.Invoke(this, lines[lineIndex]);
            lineIndex++;
            if (lineIndex >= lines.Length) Finish();
        }

        public void ResetConversation() => lineIndex = 0;

        void Finish()
        {
            if (ClueManager.Instance)
                foreach (ClueData clue in cluesToGive)
                    if (clue) ClueManager.Instance.Discover(clue);
            ConversationFinished?.Invoke(this);
        }

        public override void OnDoorAnswered(Door door, DoorAnswer answer)
        {
            if (answer == DoorAnswer.Opened)
            {
                Transform p = door.VisitorStandPoint;
                Appear(p.position, p.rotation);
                SpeakNext();
            }
            else
            {
                Ignored?.Invoke(this, door);
                if (leaveIfIgnored) SetActiveState(false);
            }
        }
    }
}
