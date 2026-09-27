using UnityEngine;

namespace Metin2Reborn
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class Metin2PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private Vector3 velocity;
        private Vector2 moveInput;
        private Vector3 planarVelocity;

        public void SetMoveInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1f);
        public void SetCamera(Transform value) => cameraTransform = value;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 direction = right * moveInput.x + forward * moveInput.y;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            Vector3 targetVelocity = direction * moveSpeed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, acceleration * Time.deltaTime);

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-rotationSpeed * Time.deltaTime));
            }

            controller.Move(planarVelocity * Time.deltaTime);

            if (controller.isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
