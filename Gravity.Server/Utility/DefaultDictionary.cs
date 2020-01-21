using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.Utility
{
    internal class DefaultDictionary<TKey, TValue>: IDictionary<TKey, TValue>
    {
        private readonly IEqualityComparer<TValue> _valueComparer;
        private readonly bool _storeDefault;
        private readonly TValue _defaultValue;
        private readonly IDictionary<TKey, TValue> _wrapped;

        /// <summary>
        /// A dictionary with more useful behaviour than the standard one
        /// </summary>
        /// <param name="keyComparer">A key comparer</param>
        /// <param name="storeDefault">Pass true to store default values in the dictionary</param>
        /// <param name="defaultValue">The default value to return when the given key is not present</param>
        public DefaultDictionary(
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer = null,
            bool storeDefault = true, 
            TValue defaultValue = default)
        {
            _valueComparer = valueComparer;
            _storeDefault = storeDefault || valueComparer == null;
            _defaultValue = defaultValue;
            _wrapped = new Dictionary<TKey, TValue>(keyComparer);
        }

        public TValue this[TKey key] 
        {
            get => _wrapped.TryGetValue(key, out var value) ? value : _defaultValue;
            set
            {
                if (!_storeDefault && _valueComparer.Equals(value, _defaultValue))
                    _wrapped.Remove(key);
                else
                    _wrapped[key] = value;
            }
        }

        public ICollection<TKey> Keys => _wrapped.Keys;
        public ICollection<TValue> Values => _wrapped.Values;
        public int Count => _wrapped.Count;
        public bool IsReadOnly => _wrapped.IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            this[key] = value;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (!_storeDefault && item.Value.Equals(_defaultValue))
                _wrapped.Remove(item.Key);
            else
                _wrapped.Add(item);
        }

        public void Clear()
        {
            _wrapped.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _wrapped.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return _wrapped.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _wrapped.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _wrapped.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return _wrapped.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _wrapped.Remove(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _wrapped.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _wrapped.GetEnumerator();
        }
    }
}