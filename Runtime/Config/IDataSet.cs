namespace PluginLit.Core
{
    public interface IDataSet
    {
        void Set<T>(string key, T value);

        T Get<T>(string key);

        T TryGet<T>(string key, T defaultValue);

        void Clear();
    }
}