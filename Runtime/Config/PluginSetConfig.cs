using System.Collections.Generic;
using System.Linq;

namespace PluginLit.Core
{
    public class PluginSetConfig : SerializedDataSet
    {
        private static PluginSetConfig _instance;

        public static PluginSetConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = SettingAssetLoader.MainSettingLoader.GetMain<PluginSetConfig>();
                }

                return _instance;
            }
        }

#if UNITY_EDITOR
        public static PluginSetConfig NewAsset
        {
            get
            {
                SettingAssetLoader.MainSettingLoader.RemoveMainAsset<PluginSetConfig>();
                return Instance;
            }
        }

        public T AddConfig<T>(string alias = null) where T : UnityEngine.ScriptableObject
        {
            if (string.IsNullOrEmpty(alias))
                alias = GetTypeId(typeof(T));
            var type = SerializedType.Create<T>(alias);
            var item = Add(type);
            
            var data = CreateInstance<T>();
            data.name = alias;
            item.Data = data;
            
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            UnityEditor.AssetDatabase.AddObjectToAsset(data, path);
            OnSerializedTypesChange();
            return data;
        }
#endif

        public override IEnumerable<SerializedType> SerializedTypes
        {
            get
            {
                foreach (var item in DataItems)
                {
                    if (item.ClassType == null)
                    {
                        item.ClassType = SerializedType.Create(item.Key, item.Data.GetType(), null);
                    }
                }
                return DataItems.Select(item => item.ClassType);
            }
        }
    }
}
