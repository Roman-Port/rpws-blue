using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws2.ApiEntities
{
    /// <summary>
    /// Caches stuff.
    /// </summary>
    public class CacheBlock<T>
    {
        private List<ClassBlockInfo<T>> cached_items;
        private List<string> cached_keys;

        public int max_items;
        public long timeout_time_seconds;

        public CacheBlock(int maxItems, long timeout_time_seconds = 0)
        {
            //Create structure.
            max_items = maxItems;
            this.timeout_time_seconds = timeout_time_seconds;
            cached_items = new List<ClassBlockInfo<T>>(maxItems);
            cached_keys = new List<string>(maxItems);
        }

        /// <summary>
        /// Remove the oldest item. Assume the lists are already locked.
        /// </summary>
        private void RemoveOldest()
        {
            RemoveAtIndex(0);
        }

        /// <summary>
        /// Remove this item. Assume the lists are already locked.
        /// </summary>
        private void RemoveAtIndex(int i)
        {
            cached_items.RemoveAt(i);
            cached_keys.RemoveAt(i);
        }

        /// <summary>
        /// Add an item to the cache.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="key"></param>
        public void AddItem(T item, string key)
        {
            //Check if it already exists.
            if (CheckIfItemExists(key))
                return;
            
            //Lock all
            lock(cached_items)
            {
                lock(cached_keys)
                {
                    //Check if we're full.
                    if (cached_items.Count >= max_items - 1)
                        RemoveOldest();
                    //Insert new
                    cached_keys.Add(key);
                    cached_items.Add(new ClassBlockInfo<T>
                    {
                        data = item,
                        key = key,
                        openTime = DateTime.UtcNow
                    });
                }
            }
        }

        private bool CheckIfExpired(ClassBlockInfo<T> data)
        {
            TimeSpan s = DateTime.UtcNow - data.openTime;
            return s.TotalSeconds > timeout_time_seconds && timeout_time_seconds > 0;
        }

        /// <summary>
        /// Tries to get an item out of the cache if it exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGetItem(string key, out T item)
        {
            item = default(T);

            //Check if it exists here.
            if (!CheckIfItemExists(key))
                return false;

            //Find index.
            int index = cached_keys.IndexOf(key);

            //Extract it from cache.
            var data = cached_items[index];
            item = data.data;

            if (data.key != key)
                throw new Exception("Keys did not match. Please try again.");

            return true;
        }

        /// <summary>
        /// Checks if an item exists and deletes timed out versions.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool CheckIfItemExists(string key)
        {
            //Check if it exists here.
            if (!cached_keys.Contains(key))
                return false;

            //Find index.
            int index = cached_keys.IndexOf(key);

            //Extract it from cache.
            var data = cached_items[index];

            //If it has expired, delete it and return false.
            if (CheckIfExpired(data))
            {
                lock (cached_items)
                {
                    lock (cached_keys)
                    {
                        RemoveAtIndex(index);
                    }
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Update an item without moving it's position in the cache.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        public void UpdateItem(string key, T item, bool updateTimeout = true)
        {
            //Check if it exists here.
            if (!CheckIfItemExists(key))
                return;

            //Lock all
            lock (cached_items)
            {
                lock (cached_keys)
                {
                    //Find index
                    int index = cached_keys.IndexOf(key);

                    //Update
                    cached_items[index].data = item;
                    if (updateTimeout)
                        cached_items[index].openTime = DateTime.UtcNow;
                }
            }
            
        }
    }

    class ClassBlockInfo<T>
    {
        public string key;
        public DateTime openTime;
        public T data;
    }
}
