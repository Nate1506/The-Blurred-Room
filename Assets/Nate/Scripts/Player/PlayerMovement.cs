using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Walk / Run on a CharacterController. Running uses PlayerStamina.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] PlayerInputSource input;
        [SerializeField] PlayerStamina stamina;
        [SerializeField] float walkSpeed = 2.2f;
        [SerializeField] float runSpeed = 4.5f;
        [SerializeField] float acceleration = 14f;
        [SerializeField] float gravity = -20f;

        CharacterController controller;
        Vector3 planarVelocity;
        float verticalVelocity;

        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }
        public float CurrentSpeed => planarVelocity.magnitude;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!input) input = GetComponentInParent<PlayerInputSource>();
            if (!stamina) stamina = this.FindInRoot<PlayerStamina>();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector2 move = (input && !input.MovementLocked) ? Vector2.ClampMagnitude(input.Move, 1f) : Vector2.zero;

            IsMoving = move.sqrMagnitude > 0.01f;
            IsRunning = IsMoving && input && input.RunHeld && (!stamina || stamina.CanRun);
            if (stamina) stamina.SetRunning(IsRunning);

            float speed = IsRunning ? runSpeed : walkSpeed;
            Vector3 target = (transform.right * move.x + transform.forward * move.y) * speed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, target, acceleration * dt);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += gravity * dt;

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
        }
    }
}
