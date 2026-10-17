using System;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>
    /// Player settings (the "setting" node). Saved in PlayerPrefs. A settings menu just reads/writes these properties.
    /// </summary>
    public static class GameSettings
    {
        const string KeySensitivity = "BR_LookSensitivity";
        const string KeyInvertY = "BR_InvertY";
        const string KeyVolume = "BR_MasterVolume";
        const string KeyFov = "BR_FieldOfView";

        static bool loaded;
        static float lookSensitivity = 1f;
        static bool invertY;
        static float masterVolume = 1f;
        static float fieldOfView = 70f;

        /// <summary>Raised whenever any setting changes.</summary>
        public static event Action Changed;

        public static float LookSensitivity
        {
            get { Load(); return lookSensitivity; }
            set { Load(); lookSensitivity = Mathf.Clamp(value, 0.1f, 5f); Commit(); }
        }

        public static bool InvertY
        {
            get { Load(); return invertY; }
            set { Load(); invertY = value; Commit(); }
        }

        public static float MasterVolume
        {
            get { Load(); return masterVolume; }
            set { Load(); masterVolume = Mathf.Clamp01(value); Commit(); }
        }

        public static float FieldOfView
        {
            get { Load(); return fieldOfView; }
            set { Load(); fieldOfView = Mathf.Clamp(value, 50f, 100f); Commit(); }
        }

        public static void ResetToDefaults()
        {
            Load();
            lookSensitivity = 1f;
            invertY = false;
            masterVolume = 1f;
            fieldOfView = 70f;
            Commit();
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            lookSensitivity = PlayerPrefs.GetFloat(KeySensitivity, 1f);
            invertY = PlayerPrefs.GetInt(KeyInvertY, 0) == 1;
            masterVolume = PlayerPrefs.GetFloat(KeyVolume, 1f);
            fieldOfView = PlayerPrefs.GetFloat(KeyFov, 70f);
        }

        static void Commit()
        {
            PlayerPrefs.SetFloat(KeySensitivity, lookSensitivity);
            PlayerPrefs.SetInt(KeyInvertY, invertY ? 1 : 0);
            PlayerPrefs.SetFloat(KeyVolume, masterVolume);
            PlayerPrefs.SetFloat(KeyFov, fieldOfView);
            PlayerPrefs.Save();
            AudioListener.volume = masterVolume;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Changed = null;
            loaded = false;
            Load();
            AudioListener.volume = masterVolume;
        }
    }
}
