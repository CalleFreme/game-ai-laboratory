using UnityEngine;

namespace GameAI.Common
{
    public class SimpleCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 5f, -8f);
        public float followSpeed = 10f;
        public float lookSpeed = 5f;

        private void LateUpdate()
        {
            if (!target) return;

            // Smooth position
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );

            // Smooth look at
            Vector3 toTarget = target.position - transform.position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toTarget, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    lookSpeed * Time.deltaTime
                );
            }
        }
    }
}
