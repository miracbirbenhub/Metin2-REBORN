using System;
using UnityEngine;

namespace Metin2Reborn.RPG
{
    public sealed class RPGCombatant : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int attack = 20;
        [SerializeField] private float attackInterval = 1.2f;
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private int experienceReward = 25;
        [SerializeField] private int yangReward = 8;

        private float nextAttackTime;
        private RPGCombatant target;
        private int health;
        private bool dead;

        public int Health => health;
        public int MaxHealth => maxHealth;
        public bool IsDead => dead;

        public event Action<RPGCombatant> Died;
        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            health = Mathf.Max(1, maxHealth);
            HealthChanged?.Invoke(health, MaxHealth);
        }

        private void Update()
        {
            if (dead || target == null || target.dead) return;
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance <= attackRange && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + Mathf.Max(0.1f, attackInterval);
                target.TakeDamage(attack);
            }
        }

        public void SetTarget(RPGCombatant value) => target = value;

        public void TakeDamage(int amount)
        {
            if (dead) return;
            health = Mathf.Max(0, health - Mathf.Max(0, amount));
            HealthChanged?.Invoke(health, MaxHealth);
            if (health > 0) return;

            dead = true;
            Died?.Invoke(this);

            if (!CompareTag("Player") && RPGGameState.Instance != null)
            {
                RPGGameState.Instance.AddExperience(experienceReward);
                RPGGameState.Instance.AddYang(yangReward);
            }

            Destroy(gameObject, 0.1f);
        }
    }
}
