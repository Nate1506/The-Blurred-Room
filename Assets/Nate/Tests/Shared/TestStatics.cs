using System.Collections.Generic;
using UnityEngine;

namespace BlurredRoom.Tests
{
    /// <summary>Resets every static in BlurredRoom.Nate so tests don't leak into each other.</summary>
    public static class TestStatics
    {
        public static void ResetAll()
        {
            TestReflection.SetStatic(typeof(ClueManager), "Instance", null);
            TestReflection.SetStatic(typeof(EndingManager), "Instance", null);
            TestReflection.SetStatic(typeof(TimeOfDayManager), "Instance", null);
            TestReflection.InvokeStatic(typeof(NpcRegistry), "ResetStatics");
        }

        /// <summary>Raises NpcRegistry.PlayerCaught (internal in the game assembly).</summary>
        public static void RaisePlayerCaught(EnemyNpc enemy)
        {
            TestReflection.InvokeStatic(typeof(NpcRegistry), "RaisePlayerCaught", new object[] { enemy });
        }
    }

    /// <summary>Backs up and restores the BR_* PlayerPrefs keys written by GameSettings.</summary>
    public sealed class PlayerPrefsBackup
    {
        public static readonly string[] FloatKeys = { "BR_LookSensitivity", "BR_MasterVolume", "BR_FieldOfView" };
        public static readonly string[] IntKeys = { "BR_InvertY" };

        readonly Dictionary<string, object> values = new Dictionary<string, object>();
        float audioVolume;

        public static PlayerPrefsBackup Capture()
        {
            var b = new PlayerPrefsBackup { audioVolume = AudioListener.volume };
            foreach (string k in FloatKeys) b.values[k] = PlayerPrefs.HasKey(k) ? (object)PlayerPrefs.GetFloat(k) : null;
            foreach (string k in IntKeys) b.values[k] = PlayerPrefs.HasKey(k) ? (object)PlayerPrefs.GetInt(k) : null;
            ReloadGameSettings();
            return b;
        }

        public void Restore()
        {
            foreach (KeyValuePair<string, object> kv in values)
            {
                if (kv.Value == null) PlayerPrefs.DeleteKey(kv.Key);
                else if (kv.Value is float f) PlayerPrefs.SetFloat(kv.Key, f);
                else PlayerPrefs.SetInt(kv.Key, (int)kv.Value);
            }
            PlayerPrefs.Save();
            ReloadGameSettings();
            AudioListener.volume = audioVolume;
        }

        /// <summary>Makes GameSettings read PlayerPrefs again on next access.</summary>
        public static void ReloadGameSettings() => TestReflection.SetStatic(typeof(GameSettings), "loaded", false);
    }
}
