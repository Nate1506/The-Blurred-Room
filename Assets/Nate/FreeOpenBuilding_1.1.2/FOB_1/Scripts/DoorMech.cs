using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DoorMech : MonoBehaviour 
{
    [Header("Door Rotation Settings")]
    [Tooltip("How many degrees the door rotates when opened (positive or negative).")]
    public float openAngle = 90f;

    [Tooltip("Speed of opening and closing transition.")]
    public float rotSpeed = 4f;

    [Tooltip("If true, automatically computes open rotation relative to initial rotation.")]
    public bool autoCalculateRotations = true;

    [Tooltip("Explicit Open Rotation Euler angles (used if autoCalculateRotations is false).")]
    public Vector3 OpenRotation;

    [Tooltip("Explicit Close Rotation Euler angles (used if autoCalculateRotations is false).")]
    public Vector3 CloseRotation;

    [Header("Door State")]
    [Tooltip("True if door is open, false if closed.")]
    public bool doorBool = false;

    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from player to door for interaction.")]
    public float interactionDistance = 2.5f;

    [Tooltip("Show an on-screen [E] prompt when player is nearby.")]
    public bool showPrompt = true;

    [Tooltip("Optional offset for door center (hinge is at 0, center of panel is at X ~ 0.7m).")]
    public Vector3 centerOffset = new Vector3(0.7f, 1.3f, 0f);

    [Header("Audio (Optional)")]
    public AudioClip openSound;
    public AudioClip closeSound;

    private Quaternion targetOpenRot;
    private Quaternion targetCloseRot;
    private bool isPlayerInTrigger = false;
    private Transform cachedPlayer;
    private AudioSource audioSource;

    void Awake()
    {
        InitializeRotations();
        audioSource = GetComponent<AudioSource>();
    }

    public void InitializeRotations()
    {
        if (autoCalculateRotations || (OpenRotation == Vector3.zero && CloseRotation == Vector3.zero))
        {
            targetCloseRot = transform.localRotation;
            targetOpenRot = targetCloseRot * Quaternion.Euler(0f, openAngle, 0f);
            CloseRotation = targetCloseRot.eulerAngles;
            OpenRotation = targetOpenRot.eulerAngles;
        }
        else
        {
            targetCloseRot = Quaternion.Euler(CloseRotation);
            targetOpenRot = Quaternion.Euler(OpenRotation);
        }
    }

    void Start()
    {
        // doorBool preserves initial state
    }

    void Update()
    {
        bool playerNearby = IsPlayerNearby();

        if (playerNearby && WasInteractPressed())
        {
            ToggleDoor();
        }

        Quaternion target = doorBool ? targetOpenRot : targetCloseRot;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, rotSpeed * Time.deltaTime);
    }

    public void ToggleDoor()
    {
        doorBool = !doorBool;
        PlaySound(doorBool ? openSound : closeSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public bool IsPlayerNearby()
    {
        if (isPlayerInTrigger)
            return true;

        Transform player = GetPlayerTransform();
        if (player != null)
        {
            Vector3 worldCenter = transform.TransformPoint(centerOffset);
            float dist = Vector3.Distance(player.position, worldCenter);
            if (dist <= interactionDistance)
            {
                return true;
            }
        }
        return false;
    }

    private Transform GetPlayerTransform()
    {
        if (cachedPlayer != null) return cachedPlayer;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            cachedPlayer = playerObj.transform;
            return cachedPlayer;
        }

        if (Camera.main != null)
        {
            cachedPlayer = Camera.main.transform;
            return cachedPlayer;
        }

        return null;
    }

    private bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.E))
        {
            return true;
        }
#endif

        return false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            isPlayerInTrigger = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            isPlayerInTrigger = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            isPlayerInTrigger = true;
        }
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        if (col.CompareTag("Player")) return true;
        if (col.GetComponentInParent<StarterAssets.FirstPersonController>() != null) return true;
        if (col.GetComponentInParent<CharacterController>() != null) return true;
        return false;
    }

    void OnGUI()
    {
        if (!showPrompt || !IsPlayerNearby()) return;

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        string prompt = doorBool ? "[E] Close Door" : "[E] Open Door";

        float width = 220f;
        float height = 40f;
        float x = (Screen.width - width) / 2f;
        float y = Screen.height - 100f;

        Color prevColor = GUI.color;
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        GUI.Box(new Rect(x, y, width, height), GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, width, height), prompt, style);
        GUI.color = prevColor;
    }
}
