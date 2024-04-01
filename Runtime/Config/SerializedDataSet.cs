using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace PluginLit.Core
{
    public class SerializedType
    {
        public static SerializedType Create<T>(string key, string toolTips = null) where T: ScriptableObject
        {
            return Create(key, typeof(T), toolTips);
        }
        
        public static SerializedType Create(string key, Type type, string toolTips = null)
        {
            return new SerializedType(key, type, toolTips);
        }
        
        private SerializedType(string key, Type type, string toolTips)
        {
            Key = key;
            ClassType = type;
            ToolTips = toolTips;
        }

        public string Key;
        public Type ClassType;
        public string ToolTips;
    }
    

    [Serializable]
    public class SerializedDataItem
    {
        [SerializeField]
        public string Key;

        [SerializeField]
        public string ClassID;
        
        [SerializeField]
        public ScriptableObject Data;
        
        [NonSerialized]
        public SerializedType ClassType;

        [NonSerialized]
        public string ToolTips;

#if UNITY_EDITOR
        [NonSerialized]
        public SerializedObject SerializedObject;
        
        public override string ToString()
        {
            return $"{Key}:{ClassID}@[{AssetDatabase.GetAssetPath(Data)}]";
        }
#endif
    }
    
    public abstract class SerializedDataSet: ScriptableObject, ISettingAsset
    {
        [HideInInspector]
        public List<SerializedDataItem> DataItems = new List<SerializedDataItem>();

        public abstract IEnumerable<SerializedType> SerializedTypes { get; }

        private Dictionary<string, SerializedType> validKeysMap { get; set; }
        private Dictionary<Type, SerializedType> validTypesMap { get; set; }

        private bool isLoading { get; set; }

        public static T LoadFromJson<T>(string json) where T : SerializedDataSet
        {
            var result = ScriptableObject.CreateInstance<T>();
            JsonUtility.FromJsonOverwrite(json, result);
            result.OnLoad();
            return result;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        private void LoadValidMap()
        {
            validKeysMap = new Dictionary<string, SerializedType>();
            validTypesMap = new Dictionary<Type, SerializedType>();
            foreach (var info in SerializedTypes)
            {
                validKeysMap.Add(info.Key, info);
                validTypesMap.Add(info.ClassType, info);
            }
        }

        public Dictionary<string, SerializedType> ValidKeysMap
        {
            get
            {
                if (validKeysMap == null) 
                    LoadValidMap();

                return validKeysMap;
            }
        }

        private Dictionary<Type, SerializedType> ValidTypesMap
        {
            get
            {
                if (validTypesMap == null)
                    LoadValidMap();

                return validTypesMap;
            }
        }

        private Dictionary<string, SerializedDataItem> _itemsMap;
        private Dictionary<string, SerializedDataItem> ItemsMap
        {
            get
            {
                if (_itemsMap == null)
                {
                    DataItems.Clear();
                    CheckDataItems();
                }

                return _itemsMap;
            }
        }

        public void OnLoad()
        {
            CheckDataItems();
        }

#if UNITY_EDITOR
        public void Reload()
        {
            DataItems.Clear();
            OnLoad();
        }
#endif

        private void CheckDataItems()
        {
            if (isLoading)
                return;

            isLoading = true;
            
            if (_itemsMap == null)
                _itemsMap = new Dictionary<string, SerializedDataItem>();
            else
                _itemsMap.Clear();
            
            foreach (var item in DataItems)
            {
                if (ValidKeysMap.ContainsKey(item.Key))
                {
                    _itemsMap.Add(item.Key, item);
                }
            }

#if UNITY_EDITOR
            try
            {
                var path = AssetDatabase.GetAssetPath(this);
                var subAssets = new List<Object>(AssetDatabase.LoadAllAssetRepresentationsAtPath(path));
                subAssets.RemoveAll(o => o == null);
                
                bool dirty = false;
#endif
                foreach (var info in SerializedTypes)
                {
                    var key = info.Key;
                    if (_itemsMap.ContainsKey(key))
                        continue;

                    var item = Add(info);
#if UNITY_EDITOR
                    var subAsset = subAssets.Find(val => val.name.Equals(key));
                    if (subAsset != null)
                    {
                        item.Data = (ScriptableObject) subAsset;
                        Debug.Log("========== add back item " + item);
                        continue;
                    }
                    
                    dirty = true;
#endif
                    var data = ScriptableObject.CreateInstance(info.ClassType);
                    data.name = key;
                    item.Data = (ScriptableObject) data;
            
#if UNITY_EDITOR
                    AssetDatabase.AddObjectToAsset(data, path);
                    Debug.Log($"================== Add {key} data({info.ToolTips}) to {path}");
#endif
                }

#if UNITY_EDITOR
                if (dirty)
                {
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
                }
            }
            finally
            {
                isLoading = false;
            }

#endif
            isLoading = false;
        }

        /// <summary>
        /// 推荐使用不带参数的版本，自动从 T 的 Attribute 里面获取 key。免得手动填错了。
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T Get<T>(string key) where T: ScriptableObject
        {
            if (ItemsMap == null)
            {
                return default(T);
            }
            
            if (ItemsMap.TryGetValue(key, out var existItem))
            {
                return (T) existItem.Data;
            }

            Debug.LogError($"SerializedDataSet has no data with key:{key}!");
            return default(T);
        }

        public T Get<T>() where T : ScriptableObject
        {
            if (ValidTypesMap.TryGetValue(typeof(T), out var info))
                return Get<T>(info.Key);
            
            Debug.LogError($"SerializedDataSet has no valid type:{typeof(T).Name}!");
            return default(T);
        }

        private T TryGet<T>(string key, T defaultValue) where T: ScriptableObject
        {
            if (ItemsMap.TryGetValue(key, out var existItem))
            {
                return (T) existItem.Data;
            }

            return defaultValue;
        }
        
        public T TryGet<T>(T defaultValue) where T : ScriptableObject
        {
            if (ValidTypesMap.TryGetValue(typeof(T), out var info))
                return TryGet<T>(info.Key, defaultValue);
            
            Debug.LogError($"SerializedDataSet has no valid type:{typeof(T).Name}!");
            return default(T);
        }

        public SerializedDataItem GetItem(string key)
        {
            return ItemsMap[key];
        }

        protected SerializedDataItem Add(SerializedType type)
        {
            var item = new SerializedDataItem
            {
                Key = type.Key,
                ClassType = type,
                ClassID = GetTypeId(type.ClassType),
                ToolTips = type.ToolTips
            };

            ItemsMap.Add(type.Key, item);
            DataItems.Add(item);
            return item;
        }

        protected void OnSerializedTypesChange()
        {
            validKeysMap = null;
            validTypesMap = null;
        }

        protected static string GetTypeId(Type type)
        {
            return type.FullName ?? type.Name;
        }
        
#if UNITY_EDITOR
        public virtual void OnChildChanged()
        {
            
        }
        
        public bool IsSomeAssetLose()
        {
            if (_itemsMap == null)
                return true;
            
            foreach (var type in SerializedTypes)
            {
                if (!_itemsMap.ContainsKey(type.Key))
                    return true;
            }
            
            return false;
        }
#endif
    }
}