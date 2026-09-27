using UnityEngine;

namespace Metin2Reborn
{
    public sealed class Metin2AutoAttacker : MonoBehaviour
    {
        [SerializeField] private float range = 3.2f;
        [SerializeField] private float attacksPerSecond = 1.2f;
        [SerializeField] private int damage = 25;
        [SerializeField] private LayerMask targetMask = ~0;

        private float cooldown;

        private void Update()
        {
            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, range, targetMask);
            float best = float.MaxValue;
            Metin2Targetable target = null;

            foreach (Collider hit in hits)
            {
                var candidate = hit.GetComponentInParent<Metin2Targetable>();
                if (candidate == null) continue;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; target = candidate; }
            }

            if (target == null) return;
            transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));
            target.TakeDamage(damage);
            cooldown = 1f / Mathf.Max(0.01f, attacksPerSecond);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
