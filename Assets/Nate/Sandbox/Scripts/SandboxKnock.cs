namespace BlurredRoom.Sandbox
{
    /// <summary>
    /// The knock pattern used everywhere in the sandbox. The scene builder writes the same values into every
    /// Door's Default Knock, and forced visits pass it to Door.BeginVisit, so the HUD knows how long a visitor waits.
    /// </summary>
    public static class SandboxKnock
    {
        public const int Knocks = 3;
        public const float Interval = 0.8f;
        public const float AnswerWindow = 12f;

        public static KnockPattern Create() => new KnockPattern
        {
            knocks = Knocks,
            interval = Interval,
            answerWindow = AnswerWindow
        };
    }
}
