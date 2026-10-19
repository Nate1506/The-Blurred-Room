using UnityEngine;

// ใส่บน "กล้อง" ที่อยู่ใต้ CameraPivot (PlayerLook หมุน pivot, สคริปต์นี้ขยับกล้องซ้อนทับอีกชั้น)
// ทำงานใน LateUpdate จะได้มาหลังระบบเดิน/มองของเนทเสมอ
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
    public float verticalBob = 0.03f;
    public float horizontalBob = 0.018f;
    public float bobRoll = 0.5f;
    public float runMultiplier = 1.7f;

    [Header("Idle breathing")]
    public float breathsPerSecond = 0.22f;
    public float breathHeight = 0.006f;
    public float breathPitch = 0.25f;

    [Header("Strafe tilt (เอียงตอนเดินข้าง)")]
    public float strafeRoll = 1.4f;

    [Header("Look sway (กล้องหน่วงตอนหันเร็ว)")]
    public float swayAmount = 0.05f;
    public float swayMax = 1.8f;

    [Header("Landing")]
    public float landingDip = 0.07f;

    [Header("Hand-held noise (สั่นเบาๆ แบบถือกล้อง)")]
    public float noiseDegrees = 0.15f;
    public float noiseSpeed = 0.35f;

    [Header("Smoothing")]
    public float smoothing = 12f;

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

    void Awake()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        noiseSeed = Random.value * 100f;
        if (!body) body = GetComponentInParent<CharacterController>();
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

        // ---- hand-held noise ----
        float t = Time.time * noiseSpeed;
        float nP = (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f * noiseDegrees * k;
        float nY = (Mathf.PerlinNoise(noiseSeed + 17.3f, t) - 0.5f) * 2f * noiseDegrees * k;
        float nR = (Mathf.PerlinNoise(noiseSeed + 41.7f, t * 0.7f) - 0.5f) * 2f * noiseDegrees * 0.6f * k;

        // ---- combine (smoothed) ----
        Vector3 targetPos = new Vector3(bobX, bobY + breathY + landOffset, 0f);
        Vector3 targetRot = new Vector3(breathP + swayPitch + nP, swayYaw + nY, bobR + strafeR + nR);
        float s = 1f - Mathf.Exp(-smoothing * dt);
        posOffset = Vector3.Lerp(posOffset, targetPos, s);
        rotOffset = Vector3.Lerp(rotOffset, targetRot, s);

        transform.localPosition = basePos + posOffset;
        transform.localRotation = baseRot * Quaternion.Euler(rotOffset);
    }
}
