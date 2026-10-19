using BlurredRoom;
using UnityEngine;

// ใส่บน "กล้อง" ที่อยู่ใต้ CameraPivot (PlayerLook หมุน pivot, สคริปต์นี้ขยับกล้องซ้อนทับอีกชั้น)
// ทำงานใน LateUpdate จะได้มาหลังระบบเดิน/มองของเนทเสมอ
//
// ของเดิม: ส่ายตามก้าว, หายใจตอนยืน, เอียงตอนเดินข้าง, กล้องหน่วงตอนหันเร็ว, ยุบตอนลงพื้น, สั่นแบบถือกล้อง
// เพิ่มใหม่: FOV ขยายตอนวิ่ง / หดตอนส่องตาแมว, เสียงฝีเท้าตามจังหวะก้าว, กล้องโคลงตามสติที่เหลือ (+หัวใจเต้น),
//           กล้องสั่นจากเหตุการณ์ (CameraDynamics.Shake — เคาะดัง, จัมป์สแกร์, ภาพแฟลช)
[DisallowMultipleComponent]
public class CameraDynamics : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("CharacterController ของผู้เล่น ใช้อ่านความเร็วกับสถานะยืนบนพื้น")]
    public CharacterController body;
    [Range(0f, 2f)] public float masterIntensity = 1f;

    [Header("Head bob (ตามจังหวะก้าว)")]
    public float walkSpeedRef = 2.2f;
    public float runSpeedRef = 4.5f;
    [Tooltip("ก้าวต่อวินาทีตอนเดิน / วิ่ง")]
    public float walkStepsPerSecond = 1.8f;
    public float runStepsPerSecond = 2.7f;
    public float verticalBob = 0.035f;
    public float horizontalBob = 0.02f;
    public float bobRoll = 0.6f;
    public float runMultiplier = 1.8f;

    [Header("Idle breathing")]
    public float breathsPerSecond = 0.22f;
    public float breathHeight = 0.007f;
    public float breathPitch = 0.3f;

    [Header("Strafe tilt (เอียงตอนเดินข้าง)")]
    public float strafeRoll = 1.6f;

    [Header("Look sway (กล้องหน่วงตอนหันเร็ว)")]
    public float swayAmount = 0.06f;
    public float swayMax = 2.2f;

    [Header("Landing")]
    public float landingDip = 0.07f;

    [Header("Hand-held noise (สั่นเบาๆ แบบถือกล้อง)")]
    public float noiseDegrees = 0.18f;
    public float noiseSpeed = 0.35f;

    [Header("FOV")]
    [Tooltip("FOV เพิ่มตอนวิ่งสุด (องศา)")]
    public float runFovKick = 7f;
    [Tooltip("FOV ตอนส่องตาแมว (ซูมเข้า)")]
    public float peepholeFov = 48f;
    public float fovSmoothing = 6f;

    [Header("Footsteps (เสียงสร้างจากโค้ด)")]
    public bool footsteps = true;
    [Range(0f, 1f)] public float footstepVolume = 0.35f;

    [Header("Sanity (ยิ่งสติน้อย กล้องยิ่งโคลง)")]
    [Tooltip("ต่ำกว่านี้เริ่มโคลง (0..1)")]
    public float sanityWobbleBelow = 0.5f;
    public float sanityWobbleDegrees = 2.5f;
    [Tooltip("ต่ำกว่านี้ได้ยินเสียงหัวใจ + FOV เต้นตาม")]
    public float heartbeatBelow = 0.3f;

    [Header("Smoothing")]
    public float smoothing = 12f;

    // ---------- event shake (เรียกจากที่ไหนก็ได้) ----------
    static float trauma;
    /// <summary>สั่นกล้อง: amount 0..1 (0.2 = เคาะดัง, 1 = จัมป์สแกร์)</summary>
    public static void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() => trauma = 0f;

    Vector3 basePos;
    Quaternion baseRot;
    float stepPhase, breathPhase, bobBlend;
    float landOffset, landVelocity;
    bool wasGrounded = true;
    float lastVerticalSpeed;
    float lastYaw, lastPitch;
    Vector3 rotOffset;    // pitch, yaw, roll (degrees)
    Vector3 posOffset;
    float noiseSeed;
    float lastStepSign = 1f;
    float heartTimer;
    float fovPulse;

    Camera cam;
    PlayerSanity sanity;
    PlayerPeephole peephole;

    void Awake()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        noiseSeed = Random.value * 100f;
        if (!body) body = GetComponentInParent<CharacterController>();
        cam = GetComponent<Camera>();
        sanity = GetComponentInParent<PlayerSanity>();
        peephole = GetComponentInParent<PlayerPeephole>();
    }

    void OnEnable()
    {
        lastYaw = body ? body.transform.eulerAngles.y : 0f;
        lastPitch = transform.parent ? transform.parent.localEulerAngles.x : 0f;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float k = masterIntensity;

        // ---- movement state ----
        Vector3 vel = body ? body.velocity : Vector3.zero;
        Vector3 planar = new Vector3(vel.x, 0f, vel.z);
        float speed = planar.magnitude;
        bool grounded = !body || body.isGrounded;

        float move01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, walkSpeedRef));
        float run01 = Mathf.Clamp01((speed - walkSpeedRef) / Mathf.Max(0.01f, runSpeedRef - walkSpeedRef));
        bobBlend = Mathf.MoveTowards(bobBlend, grounded ? move01 : 0f, dt * 5f);

        // ---- head bob: vertical dips once per step, sideways sway once per two steps ----
        float steps = Mathf.Lerp(walkStepsPerSecond, runStepsPerSecond, run01);
        if (bobBlend > 0.001f) stepPhase += dt * steps * Mathf.PI;
        float amp = bobBlend * Mathf.Lerp(1f, runMultiplier, run01) * k;
        float bobY = (Mathf.Abs(Mathf.Sin(stepPhase)) - 0.64f) * verticalBob * amp;
        float bobX = Mathf.Sin(stepPhase) * horizontalBob * amp;
        float bobR = Mathf.Sin(stepPhase) * bobRoll * amp;

        // ---- footsteps: ทุกครั้งที่ sin เปลี่ยนเครื่องหมาย = เท้าแตะพื้น ----
        float sign = Mathf.Sign(Mathf.Sin(stepPhase));
        if (footsteps && grounded && bobBlend > 0.35f && sign != lastStepSign)
            DormFX.Play2D(DormFX.FootClip, footstepVolume * Mathf.Lerp(0.6f, 1f, run01), Random.Range(0.85f, 1.1f));
        lastStepSign = sign;

        // ---- breathing when standing still ----
        breathPhase += dt * breathsPerSecond * Mathf.PI * 2f;
        float idle = (1f - bobBlend) * k;
        float breathY = Mathf.Sin(breathPhase) * breathHeight * idle;
        float breathP = Mathf.Sin(breathPhase + 0.6f) * breathPitch * idle;

        // ---- strafe tilt ----
        float lateral = body ? Vector3.Dot(planar, body.transform.right) : 0f;
        float strafeR = -lateral / Mathf.Max(0.01f, runSpeedRef) * strafeRoll * k;

        // ---- look sway: camera lags behind fast turns ----
        float yaw = body ? body.transform.eulerAngles.y : 0f;
        float pitch = transform.parent ? transform.parent.localEulerAngles.x : 0f;
        float dYaw = Mathf.DeltaAngle(lastYaw, yaw);
        float dPitch = Mathf.DeltaAngle(lastPitch, pitch);
        lastYaw = yaw; lastPitch = pitch;
        float swayYaw = Mathf.Clamp(-dYaw * swayAmount / dt * 0.016f, -swayMax, swayMax) * k;
        float swayPitch = Mathf.Clamp(-dPitch * swayAmount / dt * 0.016f, -swayMax, swayMax) * k;

        // ---- landing: spring that dips and recovers ----
        if (grounded && !wasGrounded && lastVerticalSpeed < -2f)
            landVelocity -= Mathf.Min(-lastVerticalSpeed * 0.05f, 1f) * landingDip * 20f * k;
        wasGrounded = grounded;
        lastVerticalSpeed = vel.y;
        landVelocity += (-landOffset * 120f - landVelocity * 14f) * dt;
        landOffset += landVelocity * dt;

        // ---- sanity: โคลงช้าๆ + สั่นมากขึ้น + หัวใจเต้น ----
        float s01 = sanity ? sanity.Normalized : 1f;
        float fear = Mathf.Clamp01(Mathf.InverseLerp(sanityWobbleBelow, 0f, s01));
        float t = Time.time * noiseSpeed;
        float wobR = (Mathf.PerlinNoise(noiseSeed + 3.1f, Time.time * 0.25f) - 0.5f) * 2f * sanityWobbleDegrees * fear * k;
        float wobY = (Mathf.PerlinNoise(noiseSeed + 9.7f, Time.time * 0.2f) - 0.5f) * 2f * sanityWobbleDegrees * 0.6f * fear * k;
        if (s01 < heartbeatBelow && s01 > 0f)
        {
            heartTimer -= dt;
            if (heartTimer <= 0f)
            {
                float bpm = Mathf.Lerp(80f, 140f, Mathf.InverseLerp(heartbeatBelow, 0f, s01));
                heartTimer = 60f / bpm;
                DormFX.Play2D(DormFX.HeartClip, 0.55f);
                fovPulse = 2.2f;
            }
        }
        fovPulse = Mathf.MoveTowards(fovPulse, 0f, dt * 12f);

        // ---- hand-held noise (แรงขึ้นตามความกลัว) ----
        float noiseMul = 1f + fear * 1.5f;
        float nP = (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f * noiseDegrees * noiseMul * k;
        float nY = (Mathf.PerlinNoise(noiseSeed + 17.3f, t) - 0.5f) * 2f * noiseDegrees * noiseMul * k;
        float nR = (Mathf.PerlinNoise(noiseSeed + 41.7f, t * 0.7f) - 0.5f) * 2f * noiseDegrees * 0.6f * noiseMul * k;

        // ---- event shake (trauma^2 ให้สั่นแรงตอนต้นแล้วค่อยๆ หาย) ----
        trauma = Mathf.MoveTowards(trauma, 0f, dt * 1.4f);
        float sh = trauma * trauma * k;
        float tt = Time.time * 22f;
        Vector3 shakeRot = new Vector3(
            (Mathf.PerlinNoise(noiseSeed + 5f, tt) - 0.5f) * 2f * 6f * sh,
            (Mathf.PerlinNoise(noiseSeed + 6f, tt) - 0.5f) * 2f * 6f * sh,
            (Mathf.PerlinNoise(noiseSeed + 7f, tt) - 0.5f) * 2f * 8f * sh);
        Vector3 shakePos = new Vector3(
            (Mathf.PerlinNoise(noiseSeed + 8f, tt) - 0.5f) * 0.06f * sh,
            (Mathf.PerlinNoise(noiseSeed + 9f, tt) - 0.5f) * 0.06f * sh, 0f);

        // ---- combine (smoothed) + shake ไม่ smooth ให้กระชาก ----
        Vector3 targetPos = new Vector3(bobX, bobY + breathY + landOffset, 0f);
        Vector3 targetRot = new Vector3(breathP + swayPitch + nP, swayYaw + nY + wobY, bobR + strafeR + nR + wobR);
        float s = 1f - Mathf.Exp(-smoothing * dt);
        posOffset = Vector3.Lerp(posOffset, targetPos, s);
        rotOffset = Vector3.Lerp(rotOffset, targetRot, s);

        transform.localPosition = basePos + posOffset + shakePos;
        transform.localRotation = baseRot * Quaternion.Euler(rotOffset + shakeRot);

        // ---- FOV ----
        if (cam)
        {
            bool peeping = peephole && peephole.IsViewing;
            float target = peeping ? peepholeFov : GameSettings.FieldOfView + runFovKick * run01 * bobBlend * k;
            target += fovPulse + trauma * 4f;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target, 1f - Mathf.Exp(-fovSmoothing * dt));
        }
    }
}
