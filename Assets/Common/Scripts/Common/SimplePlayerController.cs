using UnityEngine;


namespace GameAI.Common
{
    /// <summary>
    /// A basic player controller that handles movement and rotation based on input.
    /// </summary>
    public class SimplePlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f; // Units per second movement speed
        public float rotateSpeed = 360f; // Degrees per second rotation speed

        private InputSystem_Actions _playerInput;

        private void Awake()
        {
            _playerInput = new InputSystem_Actions();
            _playerInput.Player.Enable();
        }

        void Update()
        {

            Vector2 move = _playerInput.Player.Move.ReadValue<Vector2>();
            // Get raw input values (-1, 0, or 1) from Unity's Input Manager
            // Correspond to WASD keys or arrow keys by default
            float h = move.x; // A/D left/right (-1 to 1)
            float v = move.y; // W/S up/down

            // Create a 3D direction vector from the 2D input
            // Y is set to 0 since we're moving on a horizontal plane
            Vector3 inputDirection = new Vector3(h, 0f, v).normalized; // Normalize direction vector

            // Check if there's any meaningful input (avoid tiny floating point values)
            if (inputDirection.sqrMagnitude > 0.01f)
            {
                // Calculate how far to move THIS frame
                // moveSpeed * Time.deltaTime. Gives consistent movement regardless of framerate
                Vector3 displacement = inputDirection * moveSpeed * Time.deltaTime;

                // Apply the movement to the object's position
                transform.position += displacement;

                // Create a rotation that faces the movment direction
                // LookRotation creates a quaternion that points forward along inputDirection
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);

                // Smoothly rotate towards the target rotation instead of snapping instantly
                // RotateTowards limits the rotation speed to rotateSpeed degrees per second
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, // current rotation,
                    targetRotation,  // desired rotation,
                    rotateSpeed * Time.deltaTime // Max rotation THIS frame
                );
            }
        }
    }

}
