using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PluginLit.Core
{
    public class PluginsConfig : SerializedDataSet
    {
        private static PluginsConfig _instance;

        public static PluginsConfig Instance
        {
            get
            {
                if (_instance == null
#if UNITY_EDITOR
                    || !Application.isPlaying
#endif
                    )
                {
                    _instance = SettingAssetLoader.MainSettingLoader.GetMain<PluginsConfig>();
                }

                return _instance;
            }
        }

        public static PluginsConfig LoadFromJson(string json)
        {
            _instance = SerializedDataSet.LoadFromJson<PluginsConfig>(json);
            return _instance;
        }
        
#if UNITY_EDITOR
        public static PluginsConfig NewAsset
        {
            get
            {
                SettingAssetLoader.MainSettingLoader.RemoveMainAsset<PluginsConfig>();
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
