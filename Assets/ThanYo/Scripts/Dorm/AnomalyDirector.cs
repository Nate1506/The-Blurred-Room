using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;

// ความผิดปกติตอนกลางคืน (มองเห็นผ่านกล้องวงจรปิด และเห็นได้จริงในทางเดิน):
//   - ห้องย้อนอดีต: สุ่มห้องใหม่ทุกคืน แสงสีส้มรั่วออกจากประตู
//   - คนยืนนิ่งผิดปกติ (ถ้ามีผู้เช่าหายไปแล้ว จะเป็นเงาของคนนั้น)
//   - ของย้ายที่ (เก้าอี้ติดเพดาน)
//   - ใบหน้าที่หน้าต่างปลายทางเดิน
// + จัมป์สแกร์ที่หน้าต่างห้องออฟฟิศ 1 ครั้ง/คืน (ถ้าหันไปมอง)
public class AnomalyDirector : MonoBehaviour
{
    public class Anomaly
    {
        public int floor;          // 1..3
        public string desc;
        public GameObject go;
        public float until;
        public bool observed;
    }

    [Tooltip("หน้าต่างห้องออฟฟิศ (Win_Caretaker_1) ใช้กับจัมป์สแกร์")]
    public Transform officeWindow;
    public Vector3 officeCenter = new Vector3(5.2f, 1.3f, 7.9f);
    public float floorHeight = 2.8f;
    public Vector2 corridorX = new Vector2(3.5f, 18.5f);
    public float corridorZ = 5.0f;

    public readonly List<Anomaly> Active = new List<Anomaly>();
    Coroutine routine;
    readonly List<Door> roomDoors = new List<Door>();

    void Start()
    {
        foreach (var d in FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            string n = d.name;   // Door_101 .. Door_312
            if (n.Length == 8 && n.StartsWith("Door_") && n[5] >= '1' && n[5] <= '3' && char.IsDigit(n[6])) roomDoors.Add(d);
        }
    }

    public void BeginNight(int night, bool playerInOffice)
    {
        EndNight();
        routine = StartCoroutine(Run(night, playerInOffice));
    }

    public void EndNight()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        foreach (var a in Active) if (a.go) Destroy(a.go);
        Active.Clear();
    }

    /// <summary>ความผิดปกติที่ยังอยู่บนชั้นนี้ (ให้กล้องเช็ก)</summary>
    public Anomaly OnFloor(int floor)
    {
        foreach (var a in Active) if (a.floor == floor && a.go) return a;
        return null;
    }

    void Update()
    {
        for (int i = Active.Count - 1; i >= 0; i--)
            if (Time.time > Active[i].until) { if (Active[i].go) Destroy(Active[i].go); Active.RemoveAt(i); }
    }

    IEnumerator Run(int night, bool office)
    {
        yield return new WaitForSeconds(10f);
        SpawnPastRoom();
        var kinds = new List<int> { 0, 1, 2 };
        DormFX.Shuffle(kinds);
        for (int i = 0; i < kinds.Count; i++)
        {
            yield return new WaitForSeconds(Random.Range(20f, 30f));
            int floor = Random.Range(1, 4);
            if (kinds[i] == 0) SpawnFigure(floor);
            else if (kinds[i] == 1) SpawnChair(floor);
            else SpawnWindowFace(floor);
            if (i == 1 && office) StartCoroutine(OfficeWindowScare());
        }
    }

    float Y(int floor) => (floor - 1) * floorHeight;

    Anomaly Add(int floor, string desc, GameObject go, float seconds)
    {
        var a = new Anomaly { floor = floor, desc = desc, go = go, until = Time.time + seconds };
        Active.Add(a);
        return a;
    }

    void SpawnPastRoom()
    {
        if (roomDoors.Count == 0) return;
        Door d = roomDoors[Random.Range(0, roomDoors.Count)];
        int floor = d.name[5] - '0';
        var go = new GameObject("Anomaly_PastRoom_" + d.name);
        Transform sp = d.VisitorStandPoint;
        Vector3 f = sp.forward; f.y = 0; f.Normalize();
        go.transform.position = d.transform.position - f * 0.15f + Vector3.up * 1.1f;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.2f); l.range = 4f; l.intensity = 3f;
        go.AddComponent<Flicker>();
        var glow = DormFX.Box(go.transform, "Glow", Vector3.zero, new Vector3(1.1f, 2.2f, 0.02f), DormFX.Glow(new Color(1f, 0.6f, 0.25f, 1f)));
        glow.transform.rotation = Quaternion.LookRotation(f);
        glow.transform.localScale = new Vector3(0.05f, 2.2f, 0.05f);   // แสงเป็นเส้นบางๆ ตามขอบประตู
        Add(floor, "ห้อง " + d.name.Substring(5) + " มีแสงสีส้มรั่วออกมา (ห้องย้อนอดีตคืนนี้)", go, 9999f);
    }

    void SpawnFigure(int floor)
    {
        var go = new GameObject("Anomaly_Figure");
        go.transform.position = new Vector3(Random.Range(corridorX.x, corridorX.y), Y(floor), corridorZ);
        go.transform.rotation = Quaternion.LookRotation(Vector3.right);   // หันไปทางกล้อง (ปลายทางเดินด้านตะวันออก)
        string desc = "มีคนยืนนิ่งอยู่กลางทางเดิน";
        TenantInfo lost = null;
        foreach (var t in DormData.Tenants) if (GameState.Missing.Contains(t.id)) { lost = t; break; }
        GameObject lostModel = null;
        if (lost != null)
            foreach (var tn in FindObjectsByType<TenantNpc>(FindObjectsSortMode.None)) if (tn.tenantId == lost.id) lostModel = tn.model;
        if (lost != null && lostModel) { DormFX.SpawnModel(lostModel, go.transform, 0.12f); desc = "เงาที่หน้าเหมือน" + lost.name + " ยืนนิ่งอยู่กลางทางเดิน"; }
        else if (lost != null) { DormFX.BuildPerson(go.transform, lost, 0.25f); desc = "เงาที่หน้าเหมือน" + lost.name + " ยืนนิ่งอยู่กลางทางเดิน"; }
        else DormFX.BuildCreature(go.transform);
        Add(floor, desc, go, 45f);
    }

    void SpawnChair(int floor)
    {
        var go = new GameObject("Anomaly_Chair");
        go.transform.position = new Vector3(Random.Range(corridorX.x, corridorX.y), Y(floor) + 2.55f, corridorZ);
        go.transform.rotation = Quaternion.Euler(180f, Random.Range(0f, 360f), 0f);   // คว่ำติดเพดาน
        var wood = DormFX.Mat(new Color(0.35f, 0.22f, 0.12f));
        DormFX.Box(go.transform, "Seat", new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), wood);
        DormFX.Box(go.transform, "Back", new Vector3(0, 0.75f, -0.2f), new Vector3(0.45f, 0.6f, 0.05f), wood);
        for (int i = 0; i < 4; i++)
            DormFX.Box(go.transform, "Leg" + i, new Vector3(i % 2 == 0 ? -0.2f : 0.2f, 0.22f, i < 2 ? -0.2f : 0.2f), new Vector3(0.05f, 0.45f, 0.05f), wood);
        Add(floor, "เก้าอี้คว่ำติดอยู่บนเพดานทางเดิน", go, 45f);
    }

    void SpawnWindowFace(int floor)
    {
        var go = new GameObject("Anomaly_WindowFace");
        go.transform.position = new Vector3(0.45f, Y(floor) + 1.55f, corridorZ);
        go.transform.rotation = Quaternion.LookRotation(Vector3.right);
        BuildFace(go.transform);
        Add(floor, "ใบหน้าซีดขาวแนบอยู่ที่หน้าต่างปลายทางเดิน", go, 45f);
    }

    static void BuildFace(Transform t)
    {
        var pale = DormFX.Glow(new Color(0.85f, 0.85f, 0.8f));
        var black = DormFX.Mat(Color.black);
        DormFX.Box(t, "Face", Vector3.zero, new Vector3(0.26f, 0.34f, 0.04f), pale);
        DormFX.Box(t, "EyeL", new Vector3(-0.06f, 0.05f, 0.025f), new Vector3(0.06f, 0.04f, 0.01f), black);
        DormFX.Box(t, "EyeR", new Vector3(0.06f, 0.05f, 0.025f), new Vector3(0.06f, 0.04f, 0.01f), black);
        DormFX.Box(t, "Mouth", new Vector3(0f, -0.09f, 0.025f), new Vector3(0.09f, 0.08f, 0.01f), black);
    }

    IEnumerator OfficeWindowScare()
    {
        if (!officeWindow) yield break;
        Transform player = DormFX.Player;
        if (!player || !new Bounds(officeCenter, new Vector3(6f, 3f, 4f)).Contains(player.position + Vector3.up)) yield break;

        Vector3 outward = officeWindow.position - officeCenter; outward.y = 0; outward.Normalize();
        var go = new GameObject("Anomaly_OfficeWindow");
        go.transform.position = officeWindow.position + outward * 0.12f;
        go.transform.rotation = Quaternion.LookRotation(-outward);
        BuildFace(go.transform);
        for (int i = 0; i < 3; i++) { DormFX.Play(DormFX.TapClip, go.transform.position, 1f); yield return new WaitForSeconds(0.35f); }

        Camera cam = DormFX.PlayerCamera;
        for (float t = 0f; t < 10f; t += Time.deltaTime)
        {
            if (cam)
            {
                Vector3 to = go.transform.position - cam.transform.position;
                if (to.magnitude < 9f && Vector3.Angle(cam.transform.forward, to) < 14f)
                {
                    yield return DormOverlay.I.JumpScare(DormData.JumpScareSanity);
                    break;
                }
            }
            yield return null;
        }
        Destroy(go);
    }

    // ไฟกระพริบเบาๆ สำหรับห้องย้อนอดีต
    class Flicker : MonoBehaviour
    {
        Light l; float baseI;
        void Awake() { l = GetComponent<Light>(); baseI = l.intensity; }
        void Update() { l.intensity = baseI * (0.7f + Mathf.PerlinNoise(Time.time * 6f, 0f) * 0.6f); }
    }
}
