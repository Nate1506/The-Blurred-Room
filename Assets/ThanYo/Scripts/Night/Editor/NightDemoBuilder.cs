using System.Collections.Generic;
using BlurredRoom;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

// ThanYo > Blurred Room > 6. Build Night 1 demo
//   สร้างเดโมคืนแรกทั้งหมดในซีน Inside (และ ThanYo ถ้าเปิดอยู่) — รันซ้ำได้ (ลบ BR_Night แล้วสร้างใหม่)
//   ระบบเก่า (กระดาน/พิธี/CCTV/แอ็กชันกลางวัน) ถูกปิดไว้ ไม่ได้ลบ -> เมนู 6b เปิดกลับได้
//   ทุกค่า (ตำแหน่ง บทพูด เวลา ความเร็วผี ฯลฯ) ไปแก้ต่อใน Inspector ของออบเจกต์ใต้ BR_Night ได้เลย
public static class NightDemoBuilder
{
    const string Root = "BR_Night";
    const string MatDir = "Assets/ThanYo/Materials/Night";

    static readonly string[] OldSystems =
    {
        "Story", "BR_Dorm/Office/InvestigationBoard", "BR_Dorm/Office/OldGroupPhoto", "BR_Dorm/Office/OldRegister",
        "BR_Dorm/Lobby", "BR_Dorm/Laundry", "BR_Dorm/Floor3", "BR_Dorm/CCTV", "BR_Dorm/Anomalies",
    };

    // =====================================================================
    [MenuItem("ThanYo/Blurred Room/6. Build Night 1 demo")]
    public static void Build()
    {
        EnsureLayer("Spectral");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name == "ThanYo") { BuildOutside(); return; }
        if (scene.name != "Inside") { Debug.LogError("[Night] เปิดซีน Inside หรือ ThanYo ก่อน"); return; }
        Physics.SyncTransforms();

        var old = GameObject.Find(Root);
        if (old) Object.DestroyImmediate(old);
        var root = new GameObject(Root).transform;

        // ---- ระบบกลางวัน/เย็นเดิม (สัมภาษณ์ 4 แอ็กชัน, กระดาน, พิธี) เปิดไว้ — คืนแรกส่งต่อให้ NightDemo ----
        var disabled = new List<GameObject>();
        var il = GameObject.Find("Interior_Lighting");
        foreach (var path in OldSystems) { var go = FindPath(path); if (go) go.SetActive(true); }
        var mon = FindPath("BR_Dorm/Office/CCTV_Monitor");
        if (mon) foreach (var mb in mon.GetComponents<MonoBehaviour>()) if (mb) mb.enabled = true;
        SoundBankAsset();

        // ---- ไฟในห้องพักที่ปิดอยู่: ปิดทิ้ง (แสงลอดผนังทำให้ทางเดินไม่มืดจริง + เบาเครื่อง) ----
        var roomLights = il ? il.transform.Find("Room_Lights") : null;
        if (roomLights)
            foreach (Transform rl in roomLights)
            {
                string n = rl.name;
                bool keep = n.Contains("Caretaker") || n.Contains("Lobby") || n.StartsWith("Stair") || n.Contains("Laundry");
                if (!keep && rl.gameObject.activeSelf) { rl.gameObject.SetActive(false); disabled.Add(rl.gameObject); }
            }

        // ---- กล้องผู้เล่น ----
        var cam = GameObject.Find("Player").GetComponentInChildren<Camera>();
        if (!cam.GetComponent<Camcorder>()) cam.gameObject.AddComponent<Camcorder>();
        if (!cam.GetComponent<Flashlight>()) cam.gameObject.AddComponent<Flashlight>();
        cam.GetComponent<Camcorder>().owned = false;
        cam.GetComponent<Flashlight>().owned = false;
        EditorUtility.SetDirty(cam.gameObject);

        // ---- ระบบ ----
        var power = New<BuildingPower>(root, "Power", Vector3.zero);
        foreach (var n in new[] { "Floor_1", "Floor_2", "Floor_3", "Room_Lights" })
        {
            var f = il ? il.transform.Find(n) : null;
            if (f) foreach (var l in f.GetComponentsInChildren<Light>(true)) if (l.gameObject.activeInHierarchy) power.normalLights.Add(l);
        }
        var emergencyRoot = new GameObject("Emergency").transform; emergencyRoot.SetParent(power.transform, false);
        for (int fl = 0; fl < 3; fl++)
        {
            float y = fl * 2.8f + 2.3f;
            power.emergencyLights.Add(PointLight(emergencyRoot, "Red_F" + (fl + 1) + "_W", new Vector3(1.3f, y, 5f), 7f));
            power.emergencyLights.Add(PointLight(emergencyRoot, "Red_F" + (fl + 1) + "_E", new Vector3(20.8f, y, 5f), 7f));
        }
        power.emergencyLights.Add(PointLight(emergencyRoot, "Red_F1_Mid", new Vector3(11f, 2.3f, 5.6f), 6f));
        power.emergencyLights.Add(PointLight(emergencyRoot, "Red_Laundry", new Vector3(19.2f, 2.2f, 2f), 4f));

        // โคมไฟตั้งโต๊ะในห้องผู้ดูแล (ห้องเดียวที่อุ่นๆ ปลอดภัย — ดับตอนไฟดับ)
        var lamp = new GameObject("Office_DeskLamp").AddComponent<Light>();
        lamp.transform.SetParent(power.transform, false);
        lamp.transform.position = new Vector3(6.3f, 1.25f, 9.3f);
        lamp.type = LightType.Point; lamp.range = 5.5f; lamp.intensity = 2.6f; lamp.color = new Color(1f, 0.72f, 0.45f);
        lamp.shadows = LightShadows.Soft;
        power.normalLights.Add(lamp);

        var amb = New<AmbientLoop>(root, "Ambience", Vector3.zero);
        amb.clip = Clip("Music/leberch-horror-ambient-609124.mp3"); amb.volume = amb.clip ? 0.3f : 0.16f;

        // ---- ฉาก: ออฟฟิศ ----
        float deskTop = 0.755f;
        var desk = GameObject.Find("Office_Desk");
        if (desk) deskTop = desk.GetComponentInChildren<Renderer>().bounds.max.y;

        var tape = New<TapePlayer>(root, "Tape_Recorder", new Vector3(6.85f, deskTop, 9.45f));
        Prop(tape.transform, PrimitiveType.Cube, new Vector3(0, 0.05f, 0), new Vector3(0.28f, 0.09f, 0.18f), new Color(0.08f, 0.08f, 0.08f));
        Prop(tape.transform, PrimitiveType.Cube, new Vector3(0, 0.1f, 0.02f), new Vector3(0.12f, 0.01f, 0.07f), new Color(0.55f, 0.55f, 0.52f));
        tape.onlyDuringStep = "tape"; tape.completeStep = "tape"; tape.prompt = "เปิดเทปของพี่บิ๊ก";
        tape.lines = TapeLines();
        tape.rulesTitle = "กฎตอนกลางคืน (ลายมือพี่บิ๊ก)";
        tape.rulesBody = Rules;

        var gear = New<NightTrigger>(root, "Pickup_Camcorder_Flashlight", new Vector3(7.2f, deskTop, 9.15f));
        var gearModel = new GameObject("Model").transform; gearModel.SetParent(gear.transform, false);
        Prop(gearModel, PrimitiveType.Cube, new Vector3(0, 0.06f, 0), new Vector3(0.12f, 0.11f, 0.22f), new Color(0.1f, 0.1f, 0.1f));
        Prop(gearModel, PrimitiveType.Cylinder, new Vector3(0, 0.07f, 0.13f), new Vector3(0.07f, 0.03f, 0.07f), new Color(0.05f, 0.05f, 0.06f), new Vector3(90, 0, 0));
        Prop(gearModel, PrimitiveType.Cylinder, new Vector3(0.2f, 0.02f, 0), new Vector3(0.04f, 0.09f, 0.04f), new Color(0.15f, 0.15f, 0.17f), new Vector3(0, 0, 90));
        gear.mode = NightTrigger.Mode.PressE; gear.prompt = "หยิบกล้องวิดีโอ + ไฟฉาย"; gear.onlyDuringStep = "gear"; gear.completeStep = "gear";
        gear.hideAfter = gearModel.gameObject; gear.interactSize = new Vector3(0.45f, 0.25f, 0.35f);
        gear.message = "[คลิกขวา ค้าง] ยกกล้องดู    ·    [F] ไฟฉาย";
        UnityEventTools.AddVoidPersistentListener(gear.onTriggered, cam.GetComponent<Camcorder>().Give);
        UnityEventTools.AddVoidPersistentListener(gear.onTriggered, cam.GetComponent<Flashlight>().Give);

        var officeZone = New<NightTrigger>(root, "Zone_Office", new Vector3(5.2f, 1.2f, 7.9f));
        officeZone.mode = NightTrigger.Mode.EnterZone; officeZone.zoneSize = new Vector3(5f, 2.4f, 3.2f);
        officeZone.onlyDuringStep = "go_office"; officeZone.completeStep = "go_office";

        // ---- ฉาก: เดินปิดไฟ ----
        var groupF2 = New<LightGroup>(root, "Lights_Corridor_F2", Vector3.zero);
        var groupF3 = New<LightGroup>(root, "Lights_Corridor_F3", Vector3.zero);
        if (il)
        {
            groupF2.lights.AddRange(il.transform.Find("Floor_2").GetComponentsInChildren<Light>(true));
            groupF3.lights.AddRange(il.transform.Find("Floor_3").GetComponentsInChildren<Light>(true));
        }
        groupF2.offAfterStep = "switch_f2"; groupF3.offAfterStep = "switch_f3";

        var swF2 = Switch(root, "Switch_F2_East", WallPoint(new Vector3(19f, 2.8f + 1.3f, 5f), Vector3.right), Vector3.left);
        swF2.onlyDuringStep = "switch_f2"; swF2.completeStep = "switch_f2"; swF2.prompt = "ปิดไฟทางเดินชั้น 2";
        UnityEventTools.AddVoidPersistentListener(swF2.onTriggered, groupF2.TurnOff);

        var swF3 = Switch(root, "Switch_F3_West", WallPoint(new Vector3(3f, 5.6f + 1.3f, 5f), Vector3.left), Vector3.right);
        swF3.onlyDuringStep = "switch_f3"; swF3.completeStep = "switch_f3"; swF3.prompt = "ปิดไฟทางเดินชั้น 3";
        UnityEventTools.AddVoidPersistentListener(swF3.onTriggered, groupF3.TurnOff);

        // เหตุการณ์: ปิดไฟชั้น 2 แล้วประตูห้องเก็บของข้างๆ กระแทกปิด
        var slam = New<ScareEvent>(root, "Scare_F2_Slam", new Vector3(20.75f, 2.8f + 1.2f, 4.1f));
        slam.triggerByZone = false; slam.delay = 2.2f; slam.builtInSound = ScareEvent.BuiltIn.None; slam.volume = 1f;
        slam.sound = Clip("SFX/dragon-studio-door-slam-478362.mp3");
        slam.cameraShake = 0.25f; slam.flashlightFlicker = 0.8f;
        UnityEventTools.AddVoidPersistentListener(swF2.onTriggered, slam.Fire);

        // เหตุการณ์: ชั้น 3 มีอะไรยืนอยู่สุดทางเดิน (เห็นเฉพาะผ่านกล้อง)
        var watcher = New<ScareEvent>(root, "Scare_F3_Watcher", new Vector3(20f, 5.6f, 5f));
        watcher.triggerByZone = false; watcher.delay = 1.5f; watcher.builtInSound = ScareEvent.BuiltIn.None; watcher.sound = Clip("SFX/Trim/wail_distant.wav"); watcher.volume = 0.5f;
        watcher.message = "รู้สึกเหมือนมีใครยืนมองอยู่ที่สุดทางเดิน...  [คลิกขวา] ยกกล้องดู";
        watcher.apparitionSeconds = 6f; watcher.vanishWhenFilmed = true;
        watcher.apparition = Apparition(watcher.transform, Quaternion.Euler(0, -90, 0), "Mew");
        UnityEventTools.AddVoidPersistentListener(swF3.onTriggered, watcher.Fire);

        // ---- ฉาก: ไฟดับ ----
        var floor1 = New<NightTrigger>(root, "Zone_Floor1_Blackout", new Vector3(11f, 1.2f, 5f));
        floor1.mode = NightTrigger.Mode.EnterZone; floor1.zoneSize = new Vector3(22f, 2.4f, 2.2f);
        floor1.onlyDuringStep = "back_down"; floor1.completeStep = "back_down";

        var breaker = New<HoldInteract>(root, "Breaker_Laundry", WallPoint(new Vector3(19f, 1.4f, 2f), Vector3.right) + Vector3.left * 0.06f);
        breaker.transform.rotation = Quaternion.LookRotation(Vector3.left);
        Prop(breaker.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.45f, 0.6f, 0.12f), new Color(0.32f, 0.33f, 0.3f));
        Prop(breaker.transform, PrimitiveType.Cube, new Vector3(0, 0.05f, 0.09f), new Vector3(0.06f, 0.18f, 0.06f), new Color(0.6f, 0.08f, 0.05f));
        breaker.onlyDuringStep = "breaker"; breaker.completeStep = "breaker"; breaker.holdSeconds = 3.5f;
        breaker.prompt = "ค้าง [E] ดันคันโยกเบรกเกอร์"; breaker.message = "ไฟกลับมาแล้ว... ";
        breaker.interactSize = new Vector3(0.5f, 0.7f, 0.3f);
        breaker.firstHoldClip = Clip("SFX/freesound_community-run-up-stairs-open-and-close-door-82097.mp3");
        breaker.firstHoldClipAt = Point(root, "SFX_Outside_Laundry", new Vector3(15.5f, 2.2f, 5.2f));

        var whisper = New<ScareEvent>(root, "Scare_F1_Whisper", new Vector3(14.5f, 1.2f, 5f));
        whisper.zoneSize = new Vector3(1.5f, 2.4f, 2.2f); whisper.onlyDuringStep = "breaker";
        whisper.builtInSound = ScareEvent.BuiltIn.Whisper; whisper.sound = Clip("SFX/Trim/wail_distant.wav"); whisper.volume = 0.6f; whisper.subtitle = "(เสียงกระซิบ) ...ห้อง... หนึ่ง... ศูนย์... หนึ่ง..."; whisper.flashlightFlicker = 1.2f;

        // ---- สิ่งที่อยู่ตรงนั้น (ไม่มีผีไล่ เน้นเสียง/เงา/ความรู้สึกว่าถูกตาม) ----
        var presence = New<PresenceDirector>(root, "Presence", Vector3.zero);
        presence.activeDuringSteps = new[] { "breaker" };
        presence.followStepClips = Clips("SFX/freesound_community-footsteps-stairs-fast-90220.mp3");
        presence.breathClip = null;   // เสียงหอบใช้กับสเตมิน่าหมดแล้ว
        presence.doorClips = Clips("SFX/dragon-studio-open-door-stock-sfx-454246.mp3", "SFX/dragon-studio-close-door-382723.mp3", "SFX/universfield-fast-knocking-on-door-352704.mp3");
        presence.distantClips = Clips("SFX/Trim/wail_distant.wav", "SFX/dragon-studio-door-slam-478362.mp3");
        New<AudioHooks>(root, "AudioHooks", Vector3.zero);
        var glimpseRoot = new GameObject("Glimpses").transform; glimpseRoot.SetParent(presence.transform, false);
        presence.glimpses = new[]
        {
            Glimpse(glimpseRoot, "Glimpse_West_Stair", new Vector3(1.3f, 0f, 6.6f), 160f),
            Glimpse(glimpseRoot, "Glimpse_East_End", new Vector3(20.6f, 0f, 5.2f), -90f),
            Glimpse(glimpseRoot, "Glimpse_Laundry_Door", new Vector3(17.65f, 0f, 3.5f), 0f),
            Glimpse(glimpseRoot, "Glimpse_Mid_Door", new Vector3(13.1f, 0f, 5.75f), 180f),
        };

        // ---- ฉาก: เคาะประตู ----
        var visits = New<NightVisits>(root, "Visits", Vector3.zero);
        visits.door = FindPath("Dorm_Interior/Floor_1/Doors/Door_Caretaker")?.GetComponent<Door>();
        visits.fake = Object.FindFirstObjectByType<FakeVisitor>(FindObjectsInactive.Include);
        visits.revealMode = NightVisits.RevealMode.Unseen;
        visits.revealScream = Clip("SFX/Trim/scream_short.wav");
        visits.closeBreath = Clip("SFX/hasin2004-breathing-fast-247449.mp3");
        visits.visits = Visits();

        var ending = New<NightEnding>(root, "Ending", Vector3.zero);

        // ---- จุดเกิดใหม่ ----
        var respawnBreaker = Point(root, "Respawn_Breaker", new Vector3(2.4f, 0.05f, 5.2f), 90f);
        var respawnVisits = Point(root, "Respawn_Visits", new Vector3(5.2f, 0.05f, 8.2f), 180f);

        // ---- ตัวกำกับ ----
        var demo = New<NightDemo>(root, "Director", Vector3.zero);
        demo.header = "คืนที่ 1 · กะดึก";
        demo.setPhaseOnStart = false;   // เวลาเป็นของระบบเดิม (เช้า -> เย็น -> คืน)
        demo.autoStart = false;         // StoryDirector เรียก StartDemo() ตอนเข้ากลางคืน
        demo.disableOnStart = disabled.ToArray();
        demo.deathText = "...";
        demo.steps = new List<NightDemo.Step>
        {
            Step("go_office", "ไปห้องผู้ดูแล (ประตูทางซ้าย ข้างบันได)"),
            Step("tape", "เปิดเทปที่พี่บิ๊กทิ้งไว้บนโต๊ะ"),
            Step("gear", "หยิบกล้องวิดีโอกับไฟฉายบนโต๊ะ"),
            Step("switch_f2", "ขึ้นชั้น 2 ไปปิดไฟทางเดิน (สวิตช์สุดทางฝั่งขวา)"),
            Step("switch_f3", "ขึ้นชั้น 3 ไปปิดไฟทางเดิน (สวิตช์สุดทางฝั่งซ้าย)"),
            Step("back_down", "กลับลงไปห้องผู้ดูแลที่ชั้น 1"),
            Step("breaker", "ไฟดับ! ไปดันเบรกเกอร์ในห้องซักผ้า สุดทางเดินชั้น 1", checkpoint: true, respawn: respawnBreaker,
                 msg: "มืดสนิท... [คลิกขวา] ยกกล้อง จะมองเห็นในที่มืด\nอะไรก็ตามที่ได้ยิน... อย่าหันไปดูนานนัก"),
            Step("safe_room", "กลับห้องผู้ดูแล แล้วปิดประตู"),
            Step("visits", "มีคนมาเคาะ... ค้าง [Q] ส่องตาแมว แล้วเทียบกับกฎในกระดาษ", checkpoint: true, respawn: respawnVisits),
            Step("dawn", ""),
        };
        demo.steps[5].delayAfter = 3.2f;   // รอไฟดับเสร็จก่อนเข้าช่วงเบรกเกอร์

        // events ของแต่ละขั้น
        UnityEventTools.AddVoidPersistentListener(demo.steps[5].onComplete, power.Blackout);
        UnityEventTools.AddBoolPersistentListener(demo.steps[6].onBegin, power.SetPowerInstant, false);
        UnityEventTools.AddVoidPersistentListener(demo.steps[6].onComplete, power.Restore);
        UnityEventTools.AddVoidPersistentListener(demo.steps[8].onBegin, visits.Begin);

        // ปุ่มลัด "ข้ามไปฉากเคาะประตู" ในเมนู: เกิดในห้องผู้ดูแล + คืนสภาพของขั้นที่ข้ามไป
        var demoSpawn = Point(root, "Spawn_DemoKnock", new Vector3(5.2f, 0.05f, 8.2f), 180f).gameObject.AddComponent<SpawnPoint>();
        demoSpawn.id = "DemoKnock";
        UnityEventTools.AddVoidPersistentListener(demo.steps[2].onRestore, cam.GetComponent<Camcorder>().Give);
        UnityEventTools.AddVoidPersistentListener(demo.steps[2].onRestore, cam.GetComponent<Flashlight>().Give);
        UnityEventTools.AddVoidPersistentListener(demo.steps[3].onRestore, groupF2.TurnOff);
        UnityEventTools.AddVoidPersistentListener(demo.steps[4].onRestore, groupF3.TurnOff);
        UnityEventTools.AddVoidPersistentListener(demo.steps[9].onBegin, ending.Play);

        // ArrivalFocus เดิม (แพนไปรูปพี่บิ๊ก) ให้ล็อกก่อนเริ่มขั้นแรก
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Night] Inside: เดโมคืนที่ 1 พร้อม (" + demo.steps.Count + " ขั้น, ปิดระบบเก่า " + disabled.Count + " อย่าง)");
    }

    [MenuItem("ThanYo/Blurred Room/6b. Remove Night 1 demo (back to old loop)")]
    public static void Remove()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var r = GameObject.Find(Root);
        if (r) Object.DestroyImmediate(r);
        foreach (var path in OldSystems) { var go = FindPath(path, true); if (go) go.SetActive(true); }
        var rls = FindPath("Interior_Lighting/Room_Lights", true);
        if (rls) foreach (Transform rl in rls.transform) rl.gameObject.SetActive(true);
        var mon = FindPath("BR_Dorm/Office/CCTV_Monitor", true);
        if (mon) foreach (var mb in mon.GetComponents<MonoBehaviour>()) if (mb) mb.enabled = true;
        var cam = GameObject.Find("Player")?.GetComponentInChildren<Camera>();
        if (cam)
        {
            var c = cam.GetComponent<Camcorder>(); if (c) Object.DestroyImmediate(c);
            var f = cam.GetComponent<Flashlight>(); if (f) Object.DestroyImmediate(f);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // =====================================================================
    static void BuildOutside()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var old = GameObject.Find(Root);
        if (old) Object.DestroyImmediate(old);
        var root = new GameObject(Root).transform;
        var story = FindPath("Story");
        if (story) story.SetActive(true);   // ลานหน้าหอใช้ระบบเวลา/เควสเดิม
        var amb = New<AmbientLoop>(root, "Ambience", Vector3.zero);
        amb.clip = Clip("Music/universfield-dark-horror-soundscape-345814.mp3"); amb.volume = amb.clip ? 0.25f : 0f;
        var birds = New<OutdoorAmbience>(root, "Outdoor_Birds", Vector3.zero);
        birds.clip = Clip("dany_photo-forestbirds-319791.mp3");
        New<AudioHooks>(root, "AudioHooks", Vector3.zero);
        var cam = GameObject.Find("Player")?.GetComponentInChildren<Camera>();
        if (cam && !cam.GetComponent<Camcorder>()) { var c = cam.gameObject.AddComponent<Camcorder>(); c.owned = false; }
        SoundBankAsset();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Night] ThanYo: ใช้ระบบเดิม + เสียงจริง");
    }

    /// <summary>สร้าง/อัปเดต Resources/SoundBank.asset จากไฟล์เสียงในโฟลเดอร์ Sounds (ช่องที่มีค่าแล้วไม่ทับ)</summary>
    [MenuItem("ThanYo/Blurred Room/7. Create or fill Sound Bank")]
    public static void SoundBankAsset()
    {
        const string path = "Assets/ThanYo/Resources/SoundBank.asset";
        var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(path);
        if (!bank) { bank = ScriptableObject.CreateInstance<SoundBank>(); AssetDatabase.CreateAsset(bank, path); }
        AudioClip Fill(AudioClip cur, string rel) => cur ? cur : Clip(rel);
        bank.knock = Fill(bank.knock, "SFX/Trim/knock_single.wav");
        bank.knockHard = Fill(bank.knockHard, "SFX/Trim/knock_hard.wav");
        bank.doorOpen = Fill(bank.doorOpen, "SFX/dragon-studio-open-door-stock-sfx-454246.mp3");
        bank.doorClose = Fill(bank.doorClose, "SFX/dragon-studio-close-door-382723.mp3");
        bank.walkLoop = Fill(bank.walkLoop, "SFX/freesound_community-footsteps-stairs-fast-90220.mp3");
        bank.stairsLoop = Fill(bank.stairsLoop, "SFX/freesound_community-footsteps-stairs-slow-106711.mp3");
        bank.staminaBreath = Fill(bank.staminaBreath, "SFX/hasin2004-breathing-fast-247449.mp3");
        bank.omen = Fill(bank.omen, "SFX/Trim/wail_distant.wav");
        bank.runUpStairs = Fill(bank.runUpStairs, "SFX/freesound_community-run-up-stairs-open-and-close-door-82097.mp3");
        bank.scream = Fill(bank.scream, "SFX/Trim/scream_short.wav");
        bank.jumpscare = Fill(bank.jumpscare, "SFX/sound_effects75-robotic_jumpscare-485532.mp3");
        bank.menuMusic = Fill(bank.menuMusic, "Music/leberch-most-fear-horror-609123.mp3");
        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
    }

    // =====================================================================
    //  content
    // =====================================================================
    const string Rules =
        "1. คนจริงเคาะ 3 ครั้ง  หรือ  3 ครั้ง เว้นแป๊บ แล้วอีก 1 ครั้ง\n" +
        "    เคาะมากกว่านั้น หรือเคาะรัวยาวๆ = <b>ห้ามเปิด</b>\n\n" +
        "2. ก่อนเปิด ค้าง [Q] ส่องตาแมวทุกครั้ง\n" +
        "    ไม่เห็นใครยืนอยู่ แต่ยังมีเสียงเคาะ = <b>ห้ามเปิด</b>\n\n" +
        "3. คืนนี้ห้อง 101 ไม่มีคนอยู่ (ลุงสมพงษ์กลับต่างจังหวัด)\n" +
        "    ใครอ้างว่ามาจากห้อง 101 = <b>ห้ามเปิด</b>\n\n" +
        "เปิดผิดครั้งเดียว... ไม่ต้องรอเช้า";

    static TapePlayer.Line[] TapeLines() => new[]
    {
        L("ฮัลโหล... ถ้าได้ฟังเทปนี้ แปลว่าน้องมารับกะแทนพี่แล้วนะ", 4f),
        L("งานไม่มีอะไรมาก ขึ้นไปปิดไฟทางเดินชั้น 2 กับชั้น 3 แล้วกลับมานั่งในห้องนี้", 5f),
        L("กล้องวิดีโอบนโต๊ะ พกไว้ตลอด... มันเห็นบางอย่างที่ตาเรามองไม่เห็น", 4.5f),
        L("ถ้าได้ยินเสียงหายใจ แต่ไม่เห็นใคร... ยกกล้องขึ้นดู มันไม่ชอบโดนถ่าย จะช้าลง", 5f),
        L("ถ้าไฟดับ เบรกเกอร์อยู่ในห้องซักผ้า สุดทางเดินชั้น 1 ดันคันโยกค้างไว้จนไฟติด", 5f),
        L("อ้อ... ลุงสมพงษ์ห้อง 101 กลับต่างจังหวัด คืนนี้ห้อง 101 ไม่มีใครอยู่นะ", 4.5f),
        L("กฎตอนกลางคืน พี่จดไว้ให้แล้ว อ่านให้ขึ้นใจ... แล้วก็ อย่าเปิดประตูง่ายๆ", 4.5f),
    };

    static TapePlayer.Line L(string t, float s) => new TapePlayer.Line { speaker = "พี่บิ๊ก (เทป)", text = t, seconds = s };

    static List<NightVisits.Visit> Visits() => new List<NightVisits.Visit>
    {
        new NightVisits.Visit
        {
            label = "ป้าบุญมี (จริง)", tenantId = "boonmee", isFake = false, knock = KnockStyle.Three, delayBefore = 6f,
            line = "ป้าบุญมี ห้อง 104: น้องๆ ป้าเองจ้ะ... ไฟดับเมื่อกี้ป้ากลัว ขอเทียนสักเล่มได้ไหม",
            openedRealText = "ป้าบุญมี: ขอบใจจ้ะ... คืนนี้อย่าเปิดประตูให้ใครง่ายๆ นะลูก",
            ignoredRealText = "เสียงฝีเท้าป้าค่อยๆ เดินห่างออกไป...",
        },
        new NightVisits.Visit
        {
            label = "ลุงสมพงษ์ (ปลอม - กฎข้อ 3)", tenantId = "sompong", isFake = true, knock = KnockStyle.ThreePlusOne, delayBefore = 10f,
            line = "ลุงสมพงษ์ ห้อง 101: ลุงเองๆ เปิดหน่อย! ข้างนอกมันมีอะไรเดินตามลุงมา!",
            ignoredFakeText = "เสียงเคาะหยุด... แล้วมีเสียงหัวเราะเบาๆ อยู่หน้าประตู",
            deathText = "ลุงสมพงษ์... ไม่ได้อยู่ที่นี่คืนนี้",
        },
        FonVisit(),
    };

    /// <summary>คนสุดท้าย: ฝน — เล่นเสียงตามลำดับ ประตูล็อกเปิดไม่ได้</summary>
    public static NightVisits.Visit FonVisit() => new NightVisits.Visit
    {
        label = "ฝน (สคริปต์ - เปิด = จบอีกแบบ)", tenantId = "fon", isFake = true, delayBefore = 12f,
        scripted = true, lockDoor = false, standAtDoor = true,
        afterScriptText = "...เงียบไปแล้ว",
        beats = new List<NightVisits.Beat>
        {
            new NightVisits.Beat { label = "เคาะเปิด", knock = NightVisits.BeatKnock.Normal, knockCount = 3, knockInterval = 0.55f },
            new NightVisits.Beat { label = "sound_1", delay = 0.8f, voice = Clip("Voice/sound_1.wav") },
            new NightVisits.Beat { label = "sound_2 + เคาะเบาช้า 3", delay = 2f, voice = Clip("Voice/sound_2.wav"),
                                   knock = NightVisits.BeatKnock.SoftSlow, knockCount = 3, knockInterval = 1.4f, knockOffset = 0.8f },
            new NightVisits.Beat { label = "sound_3 + เคาะแรงมาก", delay = 3f, voice = Clip("Voice/sound_3.wav"),
                                   knock = NightVisits.BeatKnock.Hard, knockCount = 8, knockInterval = 0.32f, knockOffset = 0.2f },
        },
        openedVoice = Clip("Voice/sound_4.wav"),
        jumpscareImage = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThanYo/Resources/Dorm/jumpscare_fon.png"),
        jumpscareSeconds = 1.1f,
        openedEndsGame = true,
        openedEndingTitle = "ฝนเข้ามาแล้ว",
        openedEndingBody = "เช้าวันต่อมา ห้องผู้ดูแลว่างเปล่า\nประตูเปิดแง้มอยู่... มีรอยเท้าเปียกเดินเข้าไป แต่ไม่มีรอยเดินออกมา\n\n<size=24>— ฉากจบ: เปิดประตู —</size>",
    };

    static NightDemo.Step Step(string id, string objective, bool checkpoint = false, Transform respawn = null, string msg = "")
        => new NightDemo.Step { id = id, objective = objective, checkpoint = checkpoint, respawnPoint = respawn, messageOnBegin = msg, onBegin = new UnityEvent(), onComplete = new UnityEvent(), onRestore = new UnityEvent() };

    // =====================================================================
    //  helpers
    // =====================================================================
    static T New<T>(Transform parent, string name, Vector3 pos) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        return go.AddComponent<T>();
    }

    static Transform Point(Transform parent, string name, Vector3 pos, float yaw = 0f)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        return t;
    }

    static Light PointLight(Transform parent, string name, Vector3 pos, float range)
    {
        var l = new GameObject(name).AddComponent<Light>();
        l.transform.SetParent(parent, false);
        l.transform.position = pos;
        l.type = LightType.Point; l.range = range; l.intensity = 2.2f; l.color = new Color(1f, 0.08f, 0.04f);
        l.shadows = LightShadows.None; l.enabled = false;
        return l;
    }

    static NightTrigger Switch(Transform root, string name, Vector3 pos, Vector3 facing)
    {
        var sw = New<NightTrigger>(root, name, pos);
        sw.transform.rotation = Quaternion.LookRotation(facing);
        sw.mode = NightTrigger.Mode.PressE;
        sw.interactSize = new Vector3(0.3f, 0.35f, 0.2f);
        Prop(sw.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.12f, 0.18f, 0.03f), new Color(0.82f, 0.8f, 0.72f));
        Prop(sw.transform, PrimitiveType.Cube, new Vector3(0, 0.02f, 0.02f), new Vector3(0.025f, 0.05f, 0.025f), new Color(0.3f, 0.28f, 0.25f));
        return sw;
    }

    /// <summary>หาผนังจากจุด from ไปทาง dir แล้วคืนจุดบนผนัง (ถอยออกมานิดนึง)</summary>
    static Vector3 WallPoint(Vector3 from, Vector3 dir)
    {
        if (Physics.Raycast(from, dir, out var hit, 8f, ~(1 << 2), QueryTriggerInteraction.Ignore)) return hit.point - dir * 0.03f;
        return from + dir * 2f;
    }

    static GameObject Prop(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 size, Color c, Vector3 euler = default)
    {
        var g = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos + Vector3.up * 0f;
        g.transform.localRotation = Quaternion.Euler(euler);
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = Mat("Prop_" + ColorUtility.ToHtmlStringRGB(c), c, 0.3f);
        return g;
    }

    /// <summary>เงาคนดำๆ (เห็นเฉพาะผ่านกล้อง, ไม่เห็นหน้า)</summary>
    static GameObject Glimpse(Transform parent, string name, Vector3 pos, float yaw)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        var g = Apparition(t, Quaternion.identity, "Phum");
        g.name = name + "_Shape";
        // ไฟสลัวข้างหลัง ให้เห็นเป็นเงาดำตัดกับผนัง (ติด/ดับพร้อมเงา — ตาเปล่าจะเห็นแค่ไฟวูบที่ปลายทาง)
        var l = new GameObject("Backlight").AddComponent<Light>();
        l.transform.SetParent(g.transform, false);
        l.transform.localPosition = new Vector3(0f, 1.4f, -0.9f);
        l.type = LightType.Point; l.range = 3.5f; l.intensity = 2.5f; l.color = new Color(1f, 0.85f, 0.7f); l.shadows = LightShadows.None;
        return g;
    }

    static GameObject Apparition(Transform parent, Quaternion rot, string tenant = "Mew")
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThanYo/Prefabs/Tenants/Tenant_" + tenant.ToLower() + ".prefab");
        var holder = new GameObject("Apparition");
        holder.transform.SetParent(parent, false);
        holder.transform.localRotation = rot;
        if (prefab)
        {
            var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(m, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            m.transform.SetParent(holder.transform, false);
            m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity;
            m.transform.localScale = Vector3.Scale(m.transform.localScale, new Vector3(0.9f, 1.15f, 0.9f));
            foreach (var c in m.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            var skin = Mat("Shadow_Skin", new Color(0.015f, 0.015f, 0.015f), 0.1f);   // ดำสนิท เห็นแค่เค้าโครง
            foreach (var r in m.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = skin;
                r.sharedMaterials = mats;
            }
        }
        int layer = LayerMask.NameToLayer("Spectral");
        foreach (var t in holder.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer < 0 ? 0 : layer;
        holder.SetActive(false);
        return holder;
    }

    const string SoundDir = "Assets/ThanYo/Sounds/";

    static AudioClip Clip(string rel)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundDir + rel);
        if (!c) Debug.LogWarning("[Night] ไม่เจอเสียง " + SoundDir + rel + " (จะใช้เสียงสำรองจากโค้ด)");
        return c;
    }

    static AudioClip[] Clips(params string[] rels)
    {
        var l = new List<AudioClip>();
        foreach (var r in rels) { var c = Clip(r); if (c) l.Add(c); }
        return l.ToArray();
    }

    static Material Mat(string name, Color c, float smooth)
    {
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/ThanYo/Materials", "Night");
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject FindPath(string path, bool includeInactive = true)
    {
        string[] parts = path.Split('/');
        GameObject rootGo = null;
        foreach (var r in EditorSceneManager.GetActiveScene().GetRootGameObjects()) if (r.name == parts[0]) { rootGo = r; break; }
        if (!rootGo) return null;
        Transform t = rootGo.transform;
        for (int i = 1; i < parts.Length && t; i++) t = t.Find(parts[i]);
        return t ? t.gameObject : null;
    }

    static void EnsureLayer(string name)
    {
        if (LayerMask.NameToLayer(name) >= 0) return;
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        for (int i = 8; i < 32; i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue)) { sp.stringValue = name; tm.ApplyModifiedProperties(); Debug.Log("[Night] เพิ่ม layer " + name + " = " + i); return; }
        }
    }
}
