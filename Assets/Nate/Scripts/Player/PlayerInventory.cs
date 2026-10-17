using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Simple list of item ids. Duplicates allowed (e.g. 3 candles).</summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Tooltip("0 = unlimited")]
        [SerializeField] int capacity = 0;

        readonly List<string> items = new List<string>();

        public IReadOnlyList<string> Items => items;
        public bool IsFull => capacity > 0 && items.Count >= capacity;

        public event Action<string> ItemAdded;
        public event Action<string> ItemRemoved;

        public bool TryAdd(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || IsFull) return false;
            items.Add(itemId);
            ItemAdded?.Invoke(itemId);
            return true;
        }

        public bool Remove(string itemId)
        {
            if (!items.Remove(itemId)) return false;
            ItemRemoved?.Invoke(itemId);
            return true;
        }

        public bool Has(string itemId) => items.Contains(itemId);

        public int Count(string itemId)
        {
            int n = 0;
            foreach (string id in items) if (id == itemId) n++;
            return n;
        }
    }
}
