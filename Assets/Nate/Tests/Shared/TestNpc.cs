using System.Collections.Generic;

namespace BlurredRoom.Tests
{
    /// <summary>Concrete NpcBase for tests. Records door answers.</summary>
    public class TestNpc : NpcBase
    {
        public NpcKind kind = NpcKind.Neutral;
        public bool busy;
        public readonly List<DoorAnswer> answers = new List<DoorAnswer>();
        public readonly List<Door> answeredDoors = new List<Door>();

        public override NpcKind Kind => kind;
        public override bool IsBusy => busy;

        public override void OnDoorAnswered(Door door, DoorAnswer answer)
        {
            answers.Add(answer);
            answeredDoors.Add(door);
            base.OnDoorAnswered(door, answer);
        }
    }
}
