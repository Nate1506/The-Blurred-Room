using System.Collections;
using BlurredRoom;
using UnityEngine;
using UnityEngine.SceneManagement;

// ประตูที่พาไปอีกซีน (ใช้ระบบ Interact ของเนท: มองแล้วกด E)
// ใช้ได้เฉพาะช่วงเวลาที่อนุญาต (ค่าเริ่มต้น: กลางวัน + เย็น)
public class SceneDoor : MonoBehaviour, IInteractable
{
    public string targetScene = "ThanYo";
    public string targetSpawnId = "FromInside";
    public string prompt = "Go outside";
    public DayPhaseMask allowedPhases = DayPhaseMask.Day | DayPhaseMask.Eve;
    public string blockedMessage = "House rule: nobody goes in or out after dark.";

    bool loading;

    public string Prompt => IsAllowed ? prompt : "Locked (night)";

    bool IsAllowed => allowedPhases.Includes(TimeOfDayManager.Instance ? TimeOfDayManager.Instance.Phase : GameState.Phase);

    public bool CanInteract(GameObject interactor) => !loading;

    public void Interact(GameObject interactor)
    {
        if (!IsAllowed) { if (GameHUD.Instance) GameHUD.Instance.ShowMessage(blockedMessage, 3f); return; }
        StartCoroutine(Go());
    }

    IEnumerator Go()
    {
        loading = true;
        GameState.SpawnId = targetSpawnId;
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(1f, 0.6f);
        SceneManager.LoadScene(targetScene);
    }
}
