using UnityEngine;
using UnityEngine.UI;

namespace Metin2Reborn
{
    public sealed class Metin2HealthBar : MonoBehaviour
    {
        [SerializeField] private Metin2Targetable target;
        [SerializeField] private Image fill;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

        public void Initialize(Metin2Targetable value)
        {
            target = value;
            if (target != null) target.HealthChanged += OnHealthChanged;
            if (target != null) OnHealthChanged(target.Health, target.MaxHealth);
        }

        private void Start()
        {
            if (target == null) target = GetComponentInParent<Metin2Targetable>();
            if (target != null)
            {
                target.HealthChanged += OnHealthChanged;
                OnHealthChanged(target.Health, target.MaxHealth);
            }
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (target == null || cam == null) return;
            transform.position = target.transform.position + worldOffset;
            transform.forward = cam.transform.forward;
        }

        private void OnDestroy()
        {
            if (target != null) target.HealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (fill != null) fill.fillAmount = max <= 0 ? 0f : (float)current / max;
        }
    }
}
