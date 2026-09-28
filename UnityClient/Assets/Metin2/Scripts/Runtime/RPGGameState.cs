using UnityEngine;

namespace Metin2Reborn.RPG
{
    public sealed class RPGGameState : MonoBehaviour
    {
        public static RPGGameState Instance { get; private set; }

        [SerializeField] private int level = 1;
        [SerializeField] private int experience;
        [SerializeField] private int yang;

        public int Level => level;
        public int Experience => experience;
        public int Yang => yang;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int ExperienceToNextLevel => 100 + (level - 1) * 60;

        public void AddExperience(int amount)
        {
            experience += Mathf.Max(0, amount);
            while (experience >= ExperienceToNextLevel)
            {
                experience -= ExperienceToNextLevel;
                level++;
            }
        }

        public void AddYang(int amount) => yang += Mathf.Max(0, amount);
    }
}
