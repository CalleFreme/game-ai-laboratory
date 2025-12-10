using UnityEngine;


namespace GameAI.Common
{
    /// <summary>
    /// A basic Rigidbody-based player controller that handles movement and rotation based on input.
    /// Uses physics-friendly movement (MovePosition / MoveRotation).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimplePlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Units per second movement speed.")]
        [SerializeField] private float moveSpeed = 5f; // Units per second movement speed
        [Tooltip("Degrees per second rotation speed.")]
        [SerializeField] private float rotateSpeed = 360f; // Degrees per second rotation speed

        private InputSystem_Actions _playerInput;
        private Rigidbody _rb;

        // Cached input direction in world space
        private Vector3 _inputDirection;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            // Make sure we have an input actions instance
            _playerInput = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            // Enable the "Player" action map when this object is enabled
            _playerInput.Player.Enable();
        }

        private void OnDisable()
        {
            // Always disable to avoid leaking input subscriptions
            _playerInput.Player.Disable();
        }

        private void OnDestroy()
        {
            // Clean up the input actions instance. ? to be safe in case it wasn't created.
            _playerInput?.Dispose();
        }

        void Update()
        {

            // Read the 2D movement input from the Input System each frame
            Vector2 move = _playerInput.Player.Move.ReadValue<Vector2>();

            // Map X/Y plane input to world X/Z plane movement
            _inputDirection = new Vector3(move.x, 0f, move.y);

            // Optional: normalize if magnitude > 1 (diagonal)
            if (_inputDirection.sqrMagnitude > 1f)
            {
                _inputDirection.Normalize();
            }
        }

        private void FixedUpdate()
        {
            if (_inputDirection.sqrMagnitude < 0.0001f) // No meaningful input
                return;

            // --- Movement ---
            float dt = Time.fixedDeltaTime; // Fixed timestep, important for physics calculations
            Vector3 displacement = _inputDirection * moveSpeed * dt;
            Vector3 targetPosition = _rb.position + displacement;

            _rb.MovePosition(targetPosition);

            // --- Rotation ---
            Quaternion targetRotation = Quaternion.LookRotation(_inputDirection, Vector3.up);

            Quaternion newRotation = Quaternion.RotateTowards(
                _rb.rotation,
                targetRotation,
                rotateSpeed * dt
            );

            _rb.MoveRotation(newRotation);
        }
    }

}
