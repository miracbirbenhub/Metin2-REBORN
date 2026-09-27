using UnityEngine;

namespace Metin2Reborn
{
    public sealed class Metin2Targetable : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float destroyDelay = 0.15f;
        private int health;

        public int Health => health;
        public int MaxHealth => maxHealth;

        private void Awake() => health = maxHealth;

        public void TakeDamage(int amount)
        {
            if (health <= 0) return;
            health = Mathf.Max(0, health - Mathf.Max(0, amount));
            if (health == 0) Destroy(gameObject, destroyDelay);
        }
    }
}
