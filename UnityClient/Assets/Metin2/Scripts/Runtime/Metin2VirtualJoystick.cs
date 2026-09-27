using UnityEngine;
using UnityEngine.EventSystems;

namespace Metin2Reborn
{
    public sealed class Metin2VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private float radius = 80f;
        [SerializeField] private Metin2PlayerController player;

        private RectTransform root;

        private void Awake()
        {
            root = transform as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData) => UpdateInput(eventData);

        public void OnDrag(PointerEventData eventData) => UpdateInput(eventData);

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (player != null) player.SetMoveInput(Vector2.zero);
        }

        private void UpdateInput(PointerEventData eventData)
        {
            if (root == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    root, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            Vector2 input = Vector2.ClampMagnitude(local / radius, 1f);
            if (handle != null) handle.anchoredPosition = input * radius;
            if (player != null) player.SetMoveInput(input);
        }
    }
}
