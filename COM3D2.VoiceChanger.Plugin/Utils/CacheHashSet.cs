using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace COM3D2.VoiceChanger.Plugin.Utils
{
    public class CacheHashSet<T> : IEnumerable<T>
    {
        private readonly HashSet<T> hashSet;
        private readonly Queue<T> queue;
        private readonly ReaderWriterLockSlim lockSlim;

        public CacheHashSet()
        {
            hashSet = new HashSet<T>();
            queue = new Queue<T>();
            lockSlim = new ReaderWriterLockSlim();
        }

        public bool Add(T item)
        {
            lockSlim.EnterWriteLock();
            try
            {
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
