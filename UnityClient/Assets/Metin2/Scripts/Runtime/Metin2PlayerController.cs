using UnityEngine;

namespace Metin2Reborn
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class Metin2PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -25f;

        private CharacterController controller;
        private Vector3 velocity;
        private Vector2 moveInput;

        public void SetMoveInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1f);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    rotationSpeed * Time.deltaTime);
            }

            controller.Move(direction * moveSpeed * Time.deltaTime);

            if (controller.isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
