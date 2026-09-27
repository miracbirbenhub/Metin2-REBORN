using System.Collections.Generic;
using UnityEngine;

namespace Metin2Reborn
{
    public sealed class Metin2Inventory : MonoBehaviour
    {
        private readonly Dictionary<string, int> items = new Dictionary<string, int>();

        public int GetAmount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            return items.TryGetValue(itemId, out int value) ? value : 0;
        }

        public void Add(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
            if (!items.ContainsKey(itemId)) items[itemId] = 0;
            items[itemId] += amount;
            Debug.Log($"Metin2 Inventory: +{amount} {itemId} (total {items[itemId]})");
        }
    }
}
