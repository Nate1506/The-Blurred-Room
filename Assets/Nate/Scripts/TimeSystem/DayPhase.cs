using System;

namespace BlurredRoom
{
    public enum DayPhase { Day = 0, Eve = 1, Night = 2 }

    [Flags]
    public enum DayPhaseMask
    {
        None = 0,
        Day = 1 << 0,
        Eve = 1 << 1,
        Night = 1 << 2,
        All = Day | Eve | Night
    }

    public static class DayPhaseExtensions
    {
        public static bool Includes(this DayPhaseMask mask, DayPhase phase)
        {
            return (mask & (DayPhaseMask)(1 << (int)phase)) != 0;
        }
    }
}
