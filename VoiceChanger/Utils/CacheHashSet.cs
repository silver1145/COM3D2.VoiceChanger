using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static VRFaceShortcutConfig;

namespace COM3D2.VoiceChanger.Plugin.Utils
{
    /// <summary>
    /// Ordered Thread-safe HashSet
    /// <para>The earliest element will be discarded When exceeding maxLength (>0)</para>
    /// <para>Add existing element will move to the end</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CacheHashSet<T> : IEnumerable<T>
    {
        private readonly int maxLength;
        private readonly HashSet<T> hashSet;
        private readonly Queue<T> queue;
        private readonly ReaderWriterLockSlim lockSlim;

        public CacheHashSet(int maxLength = 0)
        {
            this.maxLength = maxLength;
            hashSet = new HashSet<T>();
            queue = new Queue<T>();
            lockSlim = new ReaderWriterLockSlim();
        }

        public bool Add(T item)
        {
            lockSlim.EnterWriteLock();
            try
            {
                if (maxLength > 0 && hashSet.Count >= maxLength && !hashSet.Contains(item))
                {
                    var oldestItem = queue.Dequeue();
                    hashSet.Remove(oldestItem);
                }
                if (hashSet.Contains(item))
                {
                    var existingItem = queue.FirstOrDefault(x => EqualityComparer<T>.Default.Equals(x, item));
                    if (existingItem != null)
                    {
                        var tempItems = new Queue<T>();
                        while (queue.Count > 0)
                        {
                            var currentItem = queue.Dequeue();
                            if (!EqualityComparer<T>.Default.Equals(currentItem, item))
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
                    return false;
                }
                else
                {
                    hashSet.Add(item);
                }
                queue.Enqueue(item);
                return true;
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        public bool Remove(T item)
        {
            lockSlim.EnterWriteLock();
            try
            {
                if (hashSet.Contains(item))
                {
                    hashSet.Remove(item);
                    var tempItems = new Queue<T>();
                    while (queue.Count > 0)
                    {
                        var currentItem = queue.Dequeue();
                        if (!EqualityComparer<T>.Default.Equals(currentItem, item))
                        {
                            tempItems.Enqueue(currentItem);
                        }
                    }
                    foreach (var tempItem in tempItems)
                    {
                        queue.Enqueue(tempItem);
                    }
                    return true;
                }
                return false;
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        public bool Any()
        {
            lockSlim.EnterReadLock();
            try
            {
                return hashSet.Any();
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }

        public void Clear()
        {
            lockSlim.EnterWriteLock();
            try
            {
                hashSet.Clear();
                queue.Clear();
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            lockSlim.EnterReadLock();
            try
            {
                return hashSet.Contains(item);
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lockSlim.EnterReadLock();
            try
            {
                foreach (T item in queue)
                {
                    yield return item;
                }
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
