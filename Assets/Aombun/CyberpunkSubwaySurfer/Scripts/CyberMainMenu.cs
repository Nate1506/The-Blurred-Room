using UnityEngine;

namespace CyberpunkSubwaySurfer
{
    /// <summary>
    /// Cyberpunk Game Menu helper that exposes menu actions for UI buttons or event triggers.
    /// </summary>
    public class CyberMainMenu : MonoBehaviour
    {
        public void StartGame()
        {
            if (CyberGameManager.Instance != null)
                CyberGameManager.Instance.StartGame();
        }

        public void PauseGame()
        {
            if (CyberGameManager.Instance != null)
                CyberGameManager.Instance.PauseGame();
        }

        public void ResumeGame()
        {
            if (CyberGameManager.Instance != null)
                CyberGameManager.Instance.ResumeGame();
        }

        public void OpenHowToPlay()
        {
            if (CyberGameManager.Instance != null)
                CyberGameManager.Instance.OpenHowToPlay(true);
        }

        public void CloseHowToPlay()
        {
            if (CyberGameManager.Instance != null)
                CyberGameManager.Instance.CloseHowToPlay();
        }

        public void ToggleAudio()
        {
            if (CyberGameManager.Instance != null)
                CyberGameManager.Instance.ToggleAudio();
        }

        public void ReturnToMainMenu()
        {
            if (CyberGameManager.Instance != null)
                CyberGameManager.Instance.ReturnToMainMenu();
        }

        public void QuitGame()
        {
            if (CyberGameManager.Instance != null)
                CyberGameManager.Instance.QuitGame();
        }
    }
}
