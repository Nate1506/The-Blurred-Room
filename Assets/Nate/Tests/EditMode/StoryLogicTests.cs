using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BlurredRoom.Tests
{
    /// <summary>ClueManager logic. In EditMode Awake doesn't run, so Instance is not involved here.</summary>
    public class ClueManagerTests : NateTestBase
    {
        ClueManager clues;

        [SetUp]
        public void SetUp() => clues = NewGO("Clues").AddComponent<ClueManager>();

        [Test]
        public void Discover_TrueOnlyFirstTime()
        {
            ClueData c = NewClue("letter");
            var events = new List<ClueData>();
            clues.ClueDiscovered += events.Add;

            Assert.IsTrue(clues.Discover(c));
            Assert.IsFalse(clues.Discover(c));
            Assert.AreEqual(1, clues.ClueCount);
            CollectionAssert.AreEqual(new[] { c }, events);
            CollectionAssert.AreEqual(new[] { c }, clues.Discovered);
        }

        [Test]
        public void Discover_DifferentAssetSameId_IsDuplicate()
        {
            Assert.IsTrue(clues.Discover(NewClue("photo")));
            Assert.IsFalse(clues.Discover(NewClue("photo")));
            Assert.AreEqual(1, clues.ClueCount);
        }

        [Test]
        public void Discover_Null_ReturnsFalse()
        {
            Assert.IsFalse(clues.Discover(null));
            Assert.AreEqual(0, clues.ClueCount);
        }

        [Test]
        public void HasClue_ByIdAndByAsset()
        {
            ClueData c = NewClue("diary");
            Assert.IsFalse(clues.HasClue("diary"));
            Assert.IsFalse(clues.HasClue(c));
            clues.Discover(c);
            Assert.IsTrue(clues.HasClue("diary"));
            Assert.IsTrue(clues.HasClue(c));
            Assert.IsFalse(clues.HasClue("other"));
            Assert.IsFalse(clues.HasClue((string)null));
            Assert.IsFalse(clues.HasClue(""));
            Assert.IsFalse(clues.HasClue((ClueData)null));
        }

        [Test]
        public void ClueWithEmptyId_UsesAssetName()
        {
            ClueData c = NewClue("");
            c.name = "OldPhoto";
            Assert.AreEqual("OldPhoto", c.Id);
            clues.Discover(c);
            Assert.IsTrue(clues.HasClue("OldPhoto"));
        }

        [Test]
        public void Flags_FireFlagChangedOnlyOnChange()
        {
            var events = new List<string>();
            clues.FlagChanged += (f, v) => events.Add(f + "=" + v);

            clues.SetFlag("met_landlady");
            clues.SetFlag("met_landlady");
            Assert.IsTrue(clues.HasFlag("met_landlady"));
            clues.SetFlag("met_landlady", false);
            clues.SetFlag("met_landlady", false);
            clues.SetFlag("never_set", false);
            Assert.IsFalse(clues.HasFlag("met_landlady"));

            CollectionAssert.AreEqual(new[] { "met_landlady=True", "met_landlady=False" }, events);
        }

        [Test]
        public void Flags_NullOrEmpty_AreIgnored()
        {
            int count = 0;
            clues.FlagChanged += (f, v) => count++;
            clues.SetFlag(null);
            clues.SetFlag("");
            Assert.AreEqual(0, count);
            Assert.IsFalse(clues.HasFlag(null));
            Assert.IsFalse(clues.HasFlag(""));
        }

        [Test]
        public void ResetAll_ClearsCluesAndFlags()
        {
            ClueData c = NewClue("a");
            clues.Discover(c);
            clues.SetFlag("f");
            clues.ResetAll();

            Assert.AreEqual(0, clues.ClueCount);
            Assert.IsFalse(clues.HasClue(c));
            Assert.IsFalse(clues.HasFlag("f"));
            Assert.IsTrue(clues.Discover(c), "a clue can be discovered again after ResetAll");
        }
    }

    public class EndingDataTests : NateTestBase
    {
        ClueManager clues;

        [SetUp]
        public void SetUp() => clues = NewGO("Clues").AddComponent<ClueManager>();

        EndingData Ending(string reason, ClueData[] req = null, int min = 0, string[] flags = null, string[] forbidden = null)
            => Track(TestAssets.Ending("e", reason, 0, req, min, flags, forbidden));

        [TestCase("SanityLost", true)]
        [TestCase("sanitylost", true)]
        [TestCase("SANITYLOST", true)]
        [TestCase("Caught", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void Reason_IsCaseInsensitive(string reason, bool expected)
        {
            Assert.AreEqual(expected, Ending(EndingReasons.SanityLost).Matches(reason, clues));
        }

        [Test]
        public void RequiredClues_MustAllBeDiscovered()
        {
            ClueData a = NewClue("a"), b = NewClue("b");
            EndingData e = Ending(EndingReasons.Solved, req: new[] { a, b });
            Assert.IsFalse(e.Matches(EndingReasons.Solved, clues));
            clues.Discover(a);
            Assert.IsFalse(e.Matches(EndingReasons.Solved, clues));
            clues.Discover(b);
            Assert.IsTrue(e.Matches(EndingReasons.Solved, clues));
        }

        [Test]
        public void NullEntryInRequiredClues_IsIgnored()
        {
            EndingData e = Ending(EndingReasons.Solved, req: new ClueData[] { null });
            Assert.IsTrue(e.Matches(EndingReasons.Solved, clues));
        }

        [Test]
        public void MinimumClueCount()
        {
            EndingData e = Ending(EndingReasons.Solved, min: 2);
            clues.Discover(NewClue("x"));
            Assert.IsFalse(e.Matches(EndingReasons.Solved, clues));
            clues.Discover(NewClue("y"));
            Assert.IsTrue(e.Matches(EndingReasons.Solved, clues));
        }

        [Test]
        public void RequiredFlags_MustAllBeSet()
        {
            EndingData e = Ending(EndingReasons.Solved, flags: new[] { "ritual_done", "door_sealed" });
            clues.SetFlag("ritual_done");
            Assert.IsFalse(e.Matches(EndingReasons.Solved, clues));
            clues.SetFlag("door_sealed");
            Assert.IsTrue(e.Matches(EndingReasons.Solved, clues));
        }

        [Test]
        public void ForbiddenFlags_BlockMatch()
        {
            EndingData e = Ending(EndingReasons.Solved, forbidden: new[] { "let_enemy_in" });
            Assert.IsTrue(e.Matches(EndingReasons.Solved, clues));
            clues.SetFlag("let_enemy_in");
            Assert.IsFalse(e.Matches(EndingReasons.Solved, clues));
        }

        [Test]
        public void NullClueManager_MatchesOnlyWithoutRequirements()
        {
            Assert.IsTrue(Ending(EndingReasons.Caught).Matches(EndingReasons.Caught, null));
            Assert.IsTrue(Ending(EndingReasons.Caught, forbidden: new[] { "x" }).Matches(EndingReasons.Caught, null));
            Assert.IsFalse(Ending(EndingReasons.Caught, req: new[] { NewClue("a") }).Matches(EndingReasons.Caught, null));
            Assert.IsFalse(Ending(EndingReasons.Caught, min: 1).Matches(EndingReasons.Caught, null));
            Assert.IsFalse(Ending(EndingReasons.Caught, flags: new[] { "f" }).Matches(EndingReasons.Caught, null));
            Assert.IsFalse(Ending(EndingReasons.Caught).Matches(EndingReasons.TimeUp, null));
        }

        [Test]
        public void EmptyId_UsesAssetName()
        {
            EndingData e = Track(TestAssets.Ending("", EndingReasons.Solved));
            e.name = "GoodEnding";
            Assert.AreEqual("GoodEnding", e.Id);
        }
    }
}
