using NUnit.Framework;
using UnityEngine;

namespace BlurredRoom.Tests
{
    /// <summary>NateTestBase backs up and restores the BR_* PlayerPrefs keys around every test.</summary>
    public class GameSettingsTests : NateTestBase
    {
        const float Eps = 1e-5f;

        [TestCase(0f, 0.1f)]
        [TestCase(-3f, 0.1f)]
        [TestCase(2.5f, 2.5f)]
        [TestCase(10f, 5f)]
        public void LookSensitivity_Clamps(float input, float expected)
        {
            GameSettings.LookSensitivity = input;
            Assert.That(GameSettings.LookSensitivity, Is.EqualTo(expected).Within(Eps));
        }

        [TestCase(-1f, 0f)]
        [TestCase(0.25f, 0.25f)]
        [TestCase(3f, 1f)]
        public void MasterVolume_Clamps(float input, float expected)
        {
            GameSettings.MasterVolume = input;
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(expected).Within(Eps));
        }

        [TestCase(10f, 50f)]
        [TestCase(75f, 75f)]
        [TestCase(150f, 100f)]
        public void FieldOfView_Clamps(float input, float expected)
        {
            GameSettings.FieldOfView = input;
            Assert.That(GameSettings.FieldOfView, Is.EqualTo(expected).Within(Eps));
        }

        [Test]
        public void InvertY_RoundTrips()
        {
            GameSettings.InvertY = true;
            Assert.IsTrue(GameSettings.InvertY);
            GameSettings.InvertY = false;
            Assert.IsFalse(GameSettings.InvertY);
        }

        [Test]
        public void Changed_FiresForEverySetterAndReset()
        {
            int count = 0;
            void OnChanged() => count++;
            GameSettings.Changed += OnChanged;
            try
            {
                GameSettings.LookSensitivity = 2f;
                GameSettings.InvertY = true;
                GameSettings.MasterVolume = 0.5f;
                GameSettings.FieldOfView = 80f;
                Assert.AreEqual(4, count);
                GameSettings.ResetToDefaults();
                Assert.AreEqual(5, count);
            }
            finally
            {
                GameSettings.Changed -= OnChanged;
            }
        }

        [Test]
        public void Values_AreSavedToPlayerPrefs()
        {
            GameSettings.LookSensitivity = 2f;
            GameSettings.InvertY = true;
            GameSettings.MasterVolume = 0.4f;
            GameSettings.FieldOfView = 90f;

            Assert.That(PlayerPrefs.GetFloat("BR_LookSensitivity"), Is.EqualTo(2f).Within(Eps));
            Assert.AreEqual(1, PlayerPrefs.GetInt("BR_InvertY"));
            Assert.That(PlayerPrefs.GetFloat("BR_MasterVolume"), Is.EqualTo(0.4f).Within(Eps));
            Assert.That(PlayerPrefs.GetFloat("BR_FieldOfView"), Is.EqualTo(90f).Within(Eps));
        }

        [Test]
        public void Values_AreLoadedFromPlayerPrefs()
        {
            PlayerPrefs.SetFloat("BR_LookSensitivity", 3f);
            PlayerPrefs.SetInt("BR_InvertY", 1);
            PlayerPrefs.SetFloat("BR_MasterVolume", 0.2f);
            PlayerPrefs.SetFloat("BR_FieldOfView", 60f);
            PlayerPrefsBackup.ReloadGameSettings();

            Assert.That(GameSettings.LookSensitivity, Is.EqualTo(3f).Within(Eps));
            Assert.IsTrue(GameSettings.InvertY);
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(0.2f).Within(Eps));
            Assert.That(GameSettings.FieldOfView, Is.EqualTo(60f).Within(Eps));
        }

        [Test]
        public void MissingPlayerPrefs_UseDefaults()
        {
            foreach (string k in PlayerPrefsBackup.FloatKeys) PlayerPrefs.DeleteKey(k);
            foreach (string k in PlayerPrefsBackup.IntKeys) PlayerPrefs.DeleteKey(k);
            PlayerPrefsBackup.ReloadGameSettings();

            Assert.That(GameSettings.LookSensitivity, Is.EqualTo(1f).Within(Eps));
            Assert.IsFalse(GameSettings.InvertY);
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(1f).Within(Eps));
            Assert.That(GameSettings.FieldOfView, Is.EqualTo(70f).Within(Eps));
        }

        [Test]
        public void OutOfRangePlayerPrefs_AreClampedOnLoad()
        {
            PlayerPrefs.SetFloat("BR_LookSensitivity", 50f);
            PlayerPrefs.SetFloat("BR_MasterVolume", -2f);
            PlayerPrefs.SetFloat("BR_FieldOfView", 0f);
            PlayerPrefsBackup.ReloadGameSettings();

            Assert.That(GameSettings.LookSensitivity, Is.EqualTo(5f).Within(Eps));
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(0f).Within(Eps));
            Assert.That(GameSettings.FieldOfView, Is.EqualTo(50f).Within(Eps));
        }

        [Test]
        public void ResetToDefaults_RestoresDefaultsAndSaves()
        {
            GameSettings.LookSensitivity = 4f;
            GameSettings.InvertY = true;
            GameSettings.MasterVolume = 0.1f;
            GameSettings.FieldOfView = 95f;

            GameSettings.ResetToDefaults();

            Assert.That(GameSettings.LookSensitivity, Is.EqualTo(1f).Within(Eps));
            Assert.IsFalse(GameSettings.InvertY);
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(1f).Within(Eps));
            Assert.That(GameSettings.FieldOfView, Is.EqualTo(70f).Within(Eps));
            Assert.That(PlayerPrefs.GetFloat("BR_FieldOfView"), Is.EqualTo(70f).Within(Eps));
            Assert.AreEqual(0, PlayerPrefs.GetInt("BR_InvertY"));
        }

        [Test]
        public void MasterVolume_IsAppliedToAudioListener()
        {
            GameSettings.MasterVolume = 0.3f;
            Assert.That(AudioListener.volume, Is.EqualTo(0.3f).Within(Eps));
            GameSettings.MasterVolume = 5f;
            Assert.That(AudioListener.volume, Is.EqualTo(1f).Within(Eps));
        }
    }
}
