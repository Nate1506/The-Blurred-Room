using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CyberpunkSubwaySurfer
{
    [RequireComponent(typeof(CharacterController))]
    public class CyberPlayerController : MonoBehaviour
    {
        [Header("Lane Settings")]
        public float laneDistance = 2.8f;
        public float laneChangeSpeed = 22f;
        public int currentLane = 1; // 0 = Left, 1 = Center, 2 = Right

        [Header("Movement Settings")]
        public float baseForwardSpeed = 16f;
        public float maxForwardSpeed = 34f;
        public float speedAcceleration = 0.2f; // Increases forward speed per second
        public float currentForwardSpeed;

        [Header("Jump & Gravity")]
        public float jumpForce = 13f;
        public float gravity = 36f;
        public float diveGravity = 50f;
        private float verticalVelocity = 0f;

        [Header("Slide Settings")]
        public float slideDuration = 0.7f;
        public float normalHeight = 2.0f;
        public float slideHeight = 0.9f;
        public Vector3 normalCenter = new Vector3(0f, 1.0f, 0f);
        public Vector3 slideCenter = new Vector3(0f, 0.45f, 0f);
        public bool isSliding = false;
        private float slideTimer = 0f;

        [Header("Visual Model & Procedural Animation")]
        public Transform visualRoot;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform leftArm;
        public Transform rightArm;
        public Transform thrusterLeft;
        public Transform thrusterRight;

        [Header("State")]
        public bool isDead = false;

        private CharacterController controller;
        private Vector2 touchStartPos;
        private bool isSwiping = false;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentForwardSpeed = baseForwardSpeed;

            // Ensure a kinematic Rigidbody exists so trigger collision events fire reliably
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void Start()
        {
            currentLane = 1; // Start in center lane
            transform.position = new Vector3(0f, 0.1f, 0f);
        }

        private void Update()
        {
            if (CyberGameManager.Instance != null && !CyberGameManager.Instance.isPlaying)
            {
                // In main menu or paused - stay in idle stance
                if (visualRoot != null)
                {
                    visualRoot.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2f) * 0.03f, 0f);
                }
                return;
            }

            if (isDead)
            {
                AnimateCrash();
                return;
            }

            HandleInput();
            HandleSlide();
            HandleMovement();
            AnimateProceduralRunner();
        }

        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            // Modern Input System
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                {
                    MoveLane(-1);
                }
                else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                {
                    MoveLane(1);
                }

                if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    Jump();
                }
                else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                {
                    Slide();
                }
            }

            // Mouse Swipe
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    touchStartPos = Mouse.current.position.ReadValue();
                    isSwiping = true;
                }
                else if (Mouse.current.leftButton.wasReleasedThisFrame && isSwiping)
                {
                    Vector2 delta = Mouse.current.position.ReadValue() - touchStartPos;
                    ProcessSwipe(delta);
                    isSwiping = false;
                }
            }

            // Touchscreen Swipe
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    touchStartPos = Touchscreen.current.primaryTouch.position.ReadValue();
                    isSwiping = true;
                }
                else if (Touchscreen.current.primaryTouch.press.wasReleasedThisFrame && isSwiping)
                {
                    Vector2 delta = Touchscreen.current.primaryTouch.position.ReadValue() - touchStartPos;
                    ProcessSwipe(delta);
                    isSwiping = false;
                }
            }
#else
            // Legacy Input Manager fallback
            try
            {
                if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) MoveLane(-1);
                else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) MoveLane(1);

                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) Jump();
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Slide();

                if (Input.GetMouseButtonDown(0))
                {
                    touchStartPos = Input.mousePosition;
                    isSwiping = true;
                }
                else if (Input.GetMouseButtonUp(0) && isSwiping)
                {
                    Vector2 delta = (Vector2)Input.mousePosition - touchStartPos;
                    ProcessSwipe(delta);
                    isSwiping = false;
                }
            }
            catch (System.Exception) {}
#endif
        }

        private void ProcessSwipe(Vector2 delta)
        {
            if (delta.magnitude > 35f)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (delta.x > 0) MoveLane(1);
                    else MoveLane(-1);
                }
                else
                {
                    if (delta.y > 0) Jump();
                    else Slide();
                }
            }
        }

        public void MoveLane(int direction)
        {
            int targetLane = currentLane + direction;
            if (targetLane >= 0 && targetLane <= 2)
            {
                currentLane = targetLane;
            }
        }

        public void Jump()
        {
            if (controller.isGrounded)
            {
                verticalVelocity = jumpForce;
                if (isSliding) StopSlide();
            }
        }

        public void Slide()
        {
            if (!controller.isGrounded)
            {
                // Fast dive-down when in air (Subway Surfers signature move)
                verticalVelocity = -diveGravity;
            }

            if (!isSliding)
            {
                isSliding = true;
                slideTimer = slideDuration;
                controller.height = slideHeight;
                controller.center = slideCenter;
            }
            else
            {
                slideTimer = slideDuration; // Refresh slide duration
            }
        }

        private void StopSlide()
        {
            isSliding = false;
            controller.height = normalHeight;
            controller.center = normalCenter;
        }

        private void HandleSlide()
        {
            if (isSliding)
            {
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0f)
                {
                    StopSlide();
                }
            }
        }

        private void HandleMovement()
        {
            // Gradually accelerate forward speed
            currentForwardSpeed = Mathf.Min(currentForwardSpeed + speedAcceleration * Time.deltaTime, maxForwardSpeed);

            // Calculate Target X based on current lane (0 -> -laneDistance, 1 -> 0, 2 -> +laneDistance)
            float targetX = (currentLane - 1) * laneDistance;
            float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
            float moveX = newX - transform.position.x;

            // Vertical movement & gravity
            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f; // Slight downward push to keep grounded
                }
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            // Combine motion vector
            Vector3 motion = new Vector3(moveX, verticalVelocity * Time.deltaTime, currentForwardSpeed * Time.deltaTime);
            controller.Move(motion);

            // Safety floor check: prevent falling through road surface
            if (transform.position.y < 0f && !isDead)
            {
                transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
                if (verticalVelocity < 0f) verticalVelocity = -2f;
            }
        }

        public void OnHitObstacle(CyberObstacle obstacle)
        {
            if (isDead) return;

            // Only sliding under a LowLaser obstacle counts as dodging
            if (obstacle != null && obstacle.obstacleType == ObstacleType.LowLaser)
            {
                if (isSliding)
                {
                    // Successfully slid under laser!
                    return;
                }
            }

            // Hitting ANY obstacle causes instant death!
            Crash();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDead) return;

            CyberObstacle obstacle = other.GetComponent<CyberObstacle>();
            if (obstacle == null) obstacle = other.GetComponentInParent<CyberObstacle>();
            if (obstacle != null)
            {
                OnHitObstacle(obstacle);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (isDead) return;

            // 1. Direct hit with any CyberObstacle component
            CyberObstacle obstacle = hit.collider.GetComponent<CyberObstacle>();
            if (obstacle == null) obstacle = hit.collider.GetComponentInParent<CyberObstacle>();
            if (obstacle != null)
            {
                OnHitObstacle(obstacle);
                return;
            }

            // 2. Direct hit with any solid barrier or pillar
            string colName = hit.gameObject.name;
            if (colName.Contains("Barricade") || 
                colName.Contains("Barrier") || 
                colName.Contains("Pillar") || 
                colName.Contains("Hazard") ||
                colName.Contains("Laser") ||
                colName.Contains("Obstacle"))
            {
                Crash();
                return;
            }

            // 3. Collision with train hull side body
            if (colName.Contains("Hull"))
            {
                Crash();
                return;
            }

            // 4. Direct frontal impact against steep structures (except smooth ramp/roof/track)
            if (hit.normal.z < -0.4f && !colName.Contains("Ramp") && !colName.Contains("Roof") && !colName.Contains("Road"))
            {
                Crash();
            }
        }


        public void Crash()
        {
            if (isDead) return;

            // If immortal skill is active, block the crash entirely
            if (CyberGameManager.Instance != null && CyberGameManager.Instance.isImmortal) return;

            isDead = true;

            if (CyberGameManager.Instance != null)
            {
                CyberGameManager.Instance.GameOver();
            }

            // Trigger camera shake
            CyberCameraController cam = FindFirstObjectByType<CyberCameraController>();
            if (cam != null)
            {
                cam.TriggerShake(0.6f, 0.45f);
            }
        }

        private void AnimateProceduralRunner()
        {
            if (visualRoot == null) return;

            if (isSliding)
            {
                // Sliding posture: flatten and tilt back
                visualRoot.localPosition = new Vector3(0f, 0.35f, 0.3f);
                visualRoot.localRotation = Quaternion.Euler(55f, 0f, 0f);
                visualRoot.localScale = new Vector3(1.1f, 0.5f, 1.2f);
            }
            else if (!controller.isGrounded)
            {
                // Airborne jumping posture
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.Euler(-15f, 0f, 0f);
                visualRoot.localScale = Vector3.one;

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(30f, 0f, 0f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            }
            else
            {
                // Normal running cycle
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localScale = Vector3.one;

                // Subtle body lean when turning lanes
                float targetTilt = (currentLane - 1) * -12f;
                float currentTilt = visualRoot.localEulerAngles.z;
                if (currentTilt > 180f) currentTilt -= 360f;
                float newTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 12f);
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, newTilt);

                // Run cycle limb swinging
                float runCycle = Time.time * (currentForwardSpeed * 1.1f);
                float legAngle = Mathf.Sin(runCycle) * 40f;
                float armAngle = Mathf.Sin(runCycle + Mathf.PI) * 45f;

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(legAngle, 0f, 0f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-legAngle, 0f, 0f);
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(armAngle, 0f, 0f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-armAngle, 0f, 0f);
            }
        }

        private void AnimateCrash()
        {
            if (visualRoot != null)
            {
                visualRoot.Rotate(Vector3.up, 360f * Time.deltaTime, Space.Self);
                visualRoot.Rotate(Vector3.right, 180f * Time.deltaTime, Space.Self);
                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, new Vector3(0f, 0.4f, -1.2f), Time.deltaTime * 5f);
            }
        }

        public void ResetPlayer()
        {
            isDead = false;
            currentLane = 1;
            currentForwardSpeed = baseForwardSpeed;
            verticalVelocity = 0f;
            StopSlide();
            transform.position = new Vector3(0f, 0.1f, 0f);

            if (visualRoot != null)
            {
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;
            }
        }
    }
}
