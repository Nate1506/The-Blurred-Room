using UnityEngine;

namespace BlurredRoom.Tests
{
    /// <summary>Creates in-memory ClueData / EndingData (never saved). Destroy them after the test.</summary>
    public static class TestAssets
    {
        public static ClueData Clue(string id)
        {
            var clue = ScriptableObject.CreateInstance<ClueData>();
            clue.name = "Clue_" + id;
            TestReflection.Set(clue, "id", id);
            return clue;
        }

        public static EndingData Ending(string id, string reason, int priority = 0, ClueData[] requiredClues = null,
                                        int minimumClueCount = 0, string[] requiredFlags = null,
                                        string[] forbiddenFlags = null)
        {
            var e = ScriptableObject.CreateInstance<EndingData>();
            e.name = "Ending_" + id;
            TestReflection.Set(e, "id", id);
            TestReflection.Set(e, "triggerReason", reason);
            TestReflection.Set(e, "priority", priority);
            TestReflection.Set(e, "requiredClues", requiredClues ?? new ClueData[0]);
            TestReflection.Set(e, "minimumClueCount", minimumClueCount);
            TestReflection.Set(e, "requiredFlags", requiredFlags ?? new string[0]);
            TestReflection.Set(e, "forbiddenFlags", forbiddenFlags ?? new string[0]);
            return e;
        }
    }
}
