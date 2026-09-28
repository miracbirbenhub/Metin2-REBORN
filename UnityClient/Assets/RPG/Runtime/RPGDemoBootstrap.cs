using UnityEngine;

namespace Metin2Reborn.RPG
{
    public sealed class RPGDemoBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int enemyCount = 3;

        private void Awake()
        {
            if (RPGGameState.Instance == null)
                new GameObject("RPGGameState").AddComponent<RPGGameState>();
        }

        private void Start()
        {
            if (enemyPrefab == null) return;

            for (int i = 0; i < Mathf.Clamp(enemyCount, 1, 12); i++)
            {
                Vector3 p = new Vector3((i - 1) * 2.5f, 0f, 4f + (i % 2) * 1.5f);
                Instantiate(enemyPrefab, p, Quaternion.identity);
            }
        }
    }
}
