using System.Collections.Generic;

namespace PluginLit.Core
{
    public class SimpleDataSet: IDataSet
    {
        private Dictionary<string, object> _data;

        public SimpleDataSet()
        {
            _data = new Dictionary<string, object>();
        }

        public void Set<T>(string key, T value)
        {
            _data[key] = value;
        }

        public T Get<T>(string key)
        {
            return (T)_data[key];
        }

        public T TryGet<T>(string key, T defaultValue)
        {
            if (_data.TryGetValue(key, out var val))
            {
                return (T) val;
            }

            return defaultValue;
        }
        

        public void Set(string key, object value)
        {
            Set<object>(key, value);
        }

        public object Get(string key)
        {
            return _data[key];
        }

        public object TryGet(string key, object defaultValue)
        {
            return TryGet<object>(key, defaultValue);
        }

        public void Clear()
        {
            _data?.Clear();
        }
    }
}