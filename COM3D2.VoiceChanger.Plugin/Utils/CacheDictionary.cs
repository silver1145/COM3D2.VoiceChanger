using System.Collections.Generic;
using static VRFaceShortcutConfig;
using System.Linq;
using System.Threading;

namespace COM3D2.VoiceChanger.Plugin.Utils
{
    /// <summary>
    /// Ordered Thread-safe Dictionary
    /// <para>The earliest element will be discarded When exceeding maxLength (>0)</para>
    /// <para>Add / Get existing element will move to the end</para>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    internal class CacheDictionary<TKey, TValue>
    {
        private readonly int maxLength;
        private readonly Dictionary<TKey, TValue> dictionary;
        private readonly Queue<TKey> queue;
        private readonly ReaderWriterLockSlim lockSlim;

        public CacheDictionary(int maxLength = 0)
        {
            this.maxLength = maxLength;
            dictionary = new Dictionary<TKey, TValue>();
            queue = new Queue<TKey>();
            lockSlim = new ReaderWriterLockSlim();
        }

        public void Add(TKey key, TValue value)
        {
            lockSlim.EnterWriteLock();
            try
            {
                if (maxLength > 0 && dictionary.Count >= maxLength && !dictionary.ContainsKey(key))
                {
                    TKey oldestKey = queue.Dequeue();
                    dictionary.Remove(oldestKey);
                }
                if (dictionary.ContainsKey(key))
                {
                    var existingItem = queue.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals(x, key));
                    if (existingItem != null)
                    {
                        var tempItems = new Queue<TKey>();
                        while (queue.Count > 0)
                        {
                            var currentItem = queue.Dequeue();
                            if (!EqualityComparer<TKey>.Default.Equals(currentItem, key))
                            {
                                tempItems.Enqueue(currentItem);
                            }
                        }
                        foreach (var tempItem in tempItems)
                        {
                            queue.Enqueue(tempItem);
                        }
                    }
                }
                dictionary[key] = value;
                queue.Enqueue(key);
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        public bool ContainsKey(TKey key)
        {
            lockSlim.EnterReadLock();
            try
            {
                return dictionary.ContainsKey(key);
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }

        public TValue Get(TKey key)
        {
            lockSlim.EnterUpgradeableReadLock();
            try
            {
                if (dictionary.TryGetValue(key, out TValue value))
                {
                    lockSlim.EnterWriteLock();
                    try
                    {
                        var existingItem = queue.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals(x, key));
                        if (existingItem != null)
                        {
                            var tempItems = new Queue<TKey>();
                            while (queue.Count > 0)
                            {
                                var currentItem = queue.Dequeue();
                                if (!EqualityComparer<TKey>.Default.Equals(currentItem, key))
                                {
                                    tempItems.Enqueue(currentItem);
                                }
                            }
                            foreach (var tempItem in tempItems)
                            {
                                queue.Enqueue(tempItem);
                            }
                            queue.Enqueue(existingItem);
                        }
                    }
                    finally
                    {
                        lockSlim.ExitWriteLock();
                    }
                    return value;
                }
                return default;
            }
            finally
            {
                lockSlim.ExitUpgradeableReadLock();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lockSlim.EnterUpgradeableReadLock();
            try
            {
                if (dictionary.TryGetValue(key, out value))
                {
                    lockSlim.EnterWriteLock();
                    try
                    {
                        var existingItem = queue.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals(x, key));
                        if (existingItem != null)
                        {
                            var tempItems = new Queue<TKey>();
                            while (queue.Count > 0)
                            {
                                var currentItem = queue.Dequeue();
                                if (!EqualityComparer<TKey>.Default.Equals(currentItem, key))
                                {
                                    tempItems.Enqueue(currentItem);
                                }
                            }
                            foreach (var tempItem in tempItems)
                            {
                                queue.Enqueue(tempItem);
                            }
                            queue.Enqueue(existingItem);
                        }
                    }
                    finally
                    {
                        lockSlim.ExitWriteLock();
                    }
                    return true;
                }
                return false;
            }
            finally
            {
                lockSlim.ExitUpgradeableReadLock();
            }
        }

        public void Clear()
        {
            lockSlim.EnterWriteLock();
            try
            {
                dictionary.Clear();
                queue.Clear();
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }
    }
}
