using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    public interface ISettingAsset
    {
        void OnLoad();
    }
    
    public class SettingAssetLoader
    {
        private static readonly string MainPath = "Setting";
#if UNITY_EDITOR
        public static SettingAssetLoader MainSettingLoader = new SettingAssetLoader(Path.Combine("Assets", "Resources", MainPath));
#else
        public static SettingAssetLoader MainSettingLoader = new SettingAssetLoader(MainPath);
#endif
        
        private string SavePath;
        
        public SettingAssetLoader(string savePath)
        {
            SavePath = savePath;
        }

#if UNITY_EDITOR
        public void RemoveMainAsset<T>()
        {
            var fileName = GetFullName(typeof(T).Name);
            AssetDatabase.DeleteAsset(fileName);
            AssetDatabase.Refresh();
        }
#endif

        public T GetMain<T>() where T: ScriptableObject, ISettingAsset
        {
            return GetMain<T>(typeof(T).Name);
        }

        public T GetMain<T>(string name) where T: ScriptableObject, ISettingAsset
        {
            var fileName = GetFullName(name);
            T asset = LoadAsset<T>(fileName);
            asset.OnLoad();
            return asset;
        }
        
        public string GetFullName(string name)
        {
#if UNITY_EDITOR
            var fileName = $"{name}.asset";
#else
            var fileName = name;
#endif
            if (!string.IsNullOrEmpty(SavePath))
                fileName = Path.Combine(SavePath, fileName);
            return fileName;
        }
        
#if UNITY_EDITOR
        private static T LoadAsset<T>(string fileName) where T: ScriptableObject
        {
            if (File.Exists(fileName))
            {
                var array = AssetDatabase.LoadAllAssetRepresentationsAtPath(fileName);
                Debug.Log($"Load {array.Length} sub asset at path {fileName}");
                return AssetDatabase.LoadAssetAtPath<T>(fileName);
            }
            return (T)CreateAsset(fileName, typeof(T));
        }

        private static ScriptableObject LoadAsset(string fileName, Type type)
        {
            if (EditorApplication.isPlaying)
                return (ScriptableObject) Resources.Load(fileName);
            
            if (File.Exists(fileName))
                return (ScriptableObject) AssetDatabase.LoadAssetAtPath(fileName, type);

            return CreateAsset(fileName, type);
        }

        private static ScriptableObject CreateAsset(string fileName, Type type)
        {
            var savePath = Path.GetDirectoryName(fileName);
            if (savePath != null && !Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, fileName);
            AssetDatabase.Refresh();
            return asset;
        }
#else
        private static T LoadAsset<T>(string fileName) where T: ScriptableObject
        {
            return Resources.Load<T>(fileName);
        }

        private static ScriptableObject LoadAsset(string fileName, Type type)
        {
            return (ScriptableObject) Resources.Load(fileName, type);
        }
#endif
    }
}