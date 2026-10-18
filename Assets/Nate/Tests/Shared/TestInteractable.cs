using UnityEngine;

namespace BlurredRoom.Tests
{
    public class TestInteractable : MonoBehaviour, IInteractable
    {
        public bool canInteract = true;
        public int interactCount;
        public GameObject lastInteractor;

        public string Prompt => "Test";
        public bool CanInteract(GameObject interactor) => canInteract;

        public void Interact(GameObject interactor)
        {
            interactCount++;
            lastInteractor = interactor;
        }
    }
}
