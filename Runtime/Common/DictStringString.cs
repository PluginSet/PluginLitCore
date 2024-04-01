using System;
using UnityEngine;

namespace PluginLit.Core
{
    [Serializable]
    public struct KeyValuePairStringString
    {
        [SerializeField]
        public string Key;

        [SerializeField]
        public string Value;

        public override string ToString()
        {
            return $"[{Key ?? "NULL"}]: {Value ?? "NULL"}";
        }
    }

    [Serializable]
    public struct DictStringString
    {
        [SerializeField]
        public KeyValuePairStringString[] Pairs;

        public override string ToString()
        {
            return $"{string.Join(", ", Pairs ?? Array.Empty<KeyValuePairStringString>())}";
        }
    }
}
