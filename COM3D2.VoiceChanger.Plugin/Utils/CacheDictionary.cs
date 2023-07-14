using System.Collections.Generic;

namespace COM3D2.VoiceChanger.Plugin.Utils
{
    internal class CacheDictionary<TKey, TValue>
    {
        private readonly int maxLength;
        private readonly Dictionary<TKey, TValue> dictionary;
        private readonly Queue<TKey> queue;

        public CacheDictionary(int maxLength)
        {
            this.maxLength = maxLength;
            dictionary = new Dictionary<TKey, TValue>();
            queue = new Queue<TKey>();
        }

        public void Add(TKey key, TValue value)
        {
            if (dictionary.Count >= maxLength)
            {
                TKey oldestKey = queue.Dequeue();
                dictionary.Remove(oldestKey);
            }

            dictionary[key] = value;
            queue.Enqueue(key);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public TValue Get(TKey key)
        {
            if (dictionary.TryGetValue(key, out TValue value))
            {
                queue.Enqueue(queue.Dequeue());
                return value;
            }
            return default;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (dictionary.TryGetValue(key, out value))
            {
                queue.Enqueue(queue.Dequeue());
                return true;
            }
            return false;
        }
    }
}
