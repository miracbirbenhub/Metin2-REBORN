using System;
using UnityEngine;

namespace Metin2Reborn
{
    public sealed class Metin2Targetable : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float destroyDelay = 0.15f;
        [SerializeField] private GameObject dropPrefab;
        [SerializeField] private int dropCount = 1;
        [SerializeField] private float dropScatter = 0.8f;

        private int health;
        private bool dead;

        public int Health => health;
        public int MaxHealth => maxHealth;
        public bool IsDead => dead;

        public event Action<Metin2Targetable> Died;
        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            health = Mathf.Max(1, maxHealth);
            HealthChanged?.Invoke(health, MaxHealth);
        }

        public void Configure(int healthValue, GameObject drop, int drops = 1)
        {
            maxHealth = Mathf.Max(1, healthValue);
            health = maxHealth;
            dropPrefab = drop;
            dropCount = Mathf.Max(0, drops);
            dead = false;
        }

        public void TakeDamage(int amount)
        {
            if (dead) return;

            int safeDamage = Mathf.Max(0, amount);
            if (safeDamage == 0) return;

            health = Mathf.Max(0, health - safeDamage);
            HealthChanged?.Invoke(health, MaxHealth);

            if (health > 0) return;

            dead = true;
            SpawnDrops();
            Died?.Invoke(this);
            Destroy(gameObject, destroyDelay);
        }

        private void SpawnDrops()
        {
            if (dropPrefab == null || dropCount <= 0) return;

            for (int i = 0; i < dropCount; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * dropScatter;
                Vector3 position = transform.position + new Vector3(offset.x, 0.35f, offset.y);
                Instantiate(dropPrefab, position, Quaternion.identity);
            }
        }
    }
}
