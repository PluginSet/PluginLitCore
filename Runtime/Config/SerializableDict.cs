using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PluginLit.Core
{
    [Serializable]
    public struct SerializableKeyValuePair<K, V>: IEquatable<SerializableKeyValuePair<K, V>>
    {
        [SerializeField]
        public K Key;

        [SerializeField]
        public V Value;

        private int _keyHash;

        public SerializableKeyValuePair(K key, V value)
        {
            _keyHash = key.GetHashCode();
            Key = key;
            Value = value;
        }
        
        public SerializableKeyValuePair(KeyValuePair<K, V> pair)
        :this(pair.Key, pair.Value)
        {
        }

        public bool KeyEquals(in K other)
        {
            if (default(K) == null && other == null)
            {
                return Key == null;
            }
                
            var num2 = other.GetHashCode() & int.MaxValue;
            return _keyHash == num2 && EqualityComparer<K>.Default.Equals(Key, other);
        }

        public bool Equals(SerializableKeyValuePair<K, V> other)
        {
            return KeyEquals(other.Key) && Value.Equals(other.Value);
        }

        public override string ToString()
        {
            return $"[{Key?.ToString() ?? "NULL"}]: {Value?.ToString() ?? "NULL"}";
        }

        public KeyValuePair<K, V> Pair()
        {
            return new KeyValuePair<K, V>(Key, Value);
        }
            
    }

    [Serializable]
    public class SerializableDict<K, V>: IDictionary
    {
        [SerializeField]
        public SerializableKeyValuePair<K, V>[] Pairs;

        public SerializableKeyValuePair<K, V>[] SafePairs
        {
            get
            {
                if (Pairs == null)
                    Pairs = Array.Empty<SerializableKeyValuePair<K, V>>();

                return Pairs;
            }
        }

        public SerializableDict()
        {
            
        }
        
        public SerializableDict(IDictionary<K, V> dictionary)
        {
            Pairs = new SerializableKeyValuePair<K, V>[dictionary.Count];
            var iter = dictionary.GetEnumerator();
            var index = 0;
            while (iter.MoveNext())
            {
                var kv = iter.Current;
                Pairs[index++] = new SerializableKeyValuePair<K, V>(kv.Key, kv.Value);
            }
            iter.Dispose();
        }
        
        public bool TryGetValue(in K key, out V value)
        {
            foreach (var p in SafePairs)
            {
                if (p.KeyEquals(key))
                {
                    value = p.Value;
                    return true;
                }
            }
            
            value = default(V);
            return false;
        }

        private void AddInternal(in SerializableKeyValuePair<K, V> item)
        {
            var list = new List<SerializableKeyValuePair<K, V>>(SafePairs.Length + 1);
            list.AddRange(Pairs);
            list.Add(item);
            Pairs = list.ToArray();
        }

        private void RemoveInternal(K key)
        {
            var list = new List<SerializableKeyValuePair<K, V>>(SafePairs.Length);
            list.AddRange(Pairs);
            list.RemoveAll(p => p.Key.Equals(key));
            Pairs = list.ToArray();
        }
        
        public void Add(object key, object value)
        {
            AddInternal(new SerializableKeyValuePair<K, V>((K)key, (V)value));
        }

        public void Clear()
        {
            Pairs = Array.Empty<SerializableKeyValuePair<K, V>>();
        }

        public bool Contains(object key)
        {
            return SafePairs.Any(p => p.Key.Equals(key));
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return SafePairs.ToDictionary(p => p.Key).GetEnumerator();
        }

        public void Remove(object key)
        {
            RemoveInternal((K) key);
        }

        public bool IsReadOnly => false;
        public bool IsFixedSize => true;

        public object this[object key]
        {
            get => SafePairs.First(p => p.Key.Equals(key)).Value;
            set
            {
                for (int i = 0; i < SafePairs.Length; i++)
                {
                    if (Pairs[i].Key.Equals(key))
                    {
                        Pairs[i] = new SerializableKeyValuePair<K, V>((K)key, (V)value);
                        return;
                    }
                }

                AddInternal(new SerializableKeyValuePair<K, V>((K)key, (V)value));
            }
        }


        public ICollection Keys => SafePairs.Select(p => p.Key).ToArray();
        public ICollection Values => SafePairs.Select(p => p.Value).ToArray();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (array.Rank != 1)
                throw new ArgumentException("The destination array must be one-dimensional.", nameof(array));

            if (array.Length - index < SafePairs.Length)
                throw new ArgumentException("The number of elements in the source dictionary exceeds the available space in the destination array.");

            int i = index;
            foreach (var p in Pairs)
            {
                array.SetValue(p, i);
                i++;
            }
        }

        public bool IsSynchronized => false;
        public int Count => SafePairs.Length;

        public object SyncRoot { get; } = new object();
    }
}