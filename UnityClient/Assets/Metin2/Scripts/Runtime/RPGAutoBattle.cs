using UnityEngine;

namespace Metin2Reborn.RPG
{
    public sealed class RPGAutoBattle : MonoBehaviour
    {
        [SerializeField] private float searchRadius = 8f;
        [SerializeField] private float retargetInterval = 0.4f;
        [SerializeField] private RPGCombatant combatant;

        private float nextSearch;

        private void Awake()
        {
            if (combatant == null) combatant = GetComponent<RPGCombatant>();
        }

        private void Update()
        {
            if (combatant == null || combatant.IsDead || Time.time < nextSearch) return;
            nextSearch = Time.time + retargetInterval;

            RPGCombatant nearest = null;
            float best = searchRadius * searchRadius;

            foreach (RPGCombatant candidate in FindObjectsByType<RPGCombatant>(FindObjectsSortMode.None))
            {
                if (candidate == combatant || candidate.IsDead || candidate.CompareTag("Player")) continue;
                float d = (candidate.transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = candidate;
                }
            }

            combatant.SetTarget(nearest);
        }
    }
}
