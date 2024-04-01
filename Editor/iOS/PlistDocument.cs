#if UNITY_IOS
#define UNITY_IOS_API
#endif

namespace PluginLit.Core.Editor
{
    public class PlistDocument
    {
#if UNITY_IOS_API
        private UnityEditor.iOS.Xcode.PlistDocument Document;
        
        private UnityEditor.iOS.Xcode.PlistElementDict root => Document.root;
#endif
        
        public PlistDocument()
#if UNITY_IOS_API
            :this(new UnityEditor.iOS.Xcode.PlistDocument())
#endif
        {
        }
        
#if UNITY_IOS_API
        public PlistDocument(UnityEditor.iOS.Xcode.PlistDocument document)
        {
            Document = document;
        }
#endif

        public void Create()
        {
#if UNITY_IOS_API
            Document.Create();
#endif
        }

        public void ReadFromFile(string path)
        {
#if UNITY_IOS_API
            Document.ReadFromFile(path);
#endif
        }
        
        public void WriteToFile(string path)
        {
#if UNITY_IOS_API
            Document.WriteToFile(path);
#endif
        }
        
        
        public bool HasPlistValue(string key)
        {
#if UNITY_IOS_API
            return root.values.ContainsKey(key);
#else
            return false;
#endif
        }

        public void RemoveElement(string key)
        {
#if UNITY_IOS_API
            root.values.Remove(key);
#endif
        }
        
        public void AddPlistValue(string key, string value)
        {
#if UNITY_IOS_API
            if (HasPlistValue(key)) return;
            root.SetString(key, value);
#endif
        }
        
        public void AddPlistValue(string key, bool value)
        {
#if UNITY_IOS_API
            if (HasPlistValue(key)) return;
            root.SetBoolean(key, value);
#endif
        }
        
        public void AddPlistValue(string key, int value)
        {
#if UNITY_IOS_API
            if (HasPlistValue(key)) return;
            root.SetInteger(key, value);
#endif
        }

        public void SetPlistValue(string key, string value)
        {
#if UNITY_IOS_API
            root.SetString(key, value);
#endif
        }
        
        public void SetPlistValue(string key, bool value)
        {
#if UNITY_IOS_API
            root.SetBoolean(key, value);
#endif
        }
        
        public void SetPlistValue(string key, int value)
        {
#if UNITY_IOS_API
            root.SetInteger(key, value);
#endif
        }
        
        public void AddArrayElement(string arrayKey, params string[] values)
        {
#if UNITY_IOS_API
            var array = FindOrCreateArray(root, arrayKey);
            foreach (var value in values)
            {
                AddStringIfNo(array, value);
            }
#endif
        }
        
        public void AddDictElement(string dictKey, string key, bool value)
        {
#if UNITY_IOS_API
            var dict = FindOrCreateDict(dictKey);
            dict.SetBoolean(key, value);
#endif
        }
        
        public void AddDictElement(string dictKey, string key, int value)
        {
#if UNITY_IOS_API
            var dict = FindOrCreateDict(dictKey);
            dict.SetInteger(key, value);
#endif
        }
        
        public void AddDictElement(string dictKey, string key, string value)
        {
#if UNITY_IOS_API
            var dict = FindOrCreateDict(dictKey);
            dict.SetString(key, value);
#endif
        }

        public void AddXcodeURLType(string id, string role, params string[] schemes)
        {
#if UNITY_IOS_API
            var cfBundleUrlTypes = FindOrCreateArray(root, "CFBundleURLTypes");

            var node = cfBundleUrlTypes.values.Find(element => element.AsDict().values["CFBundleURLName"].AsString().Equals(id))?.AsDict();
            if (node == null)
            {
                node = cfBundleUrlTypes.AddDict();
            }
            
            node.SetString("CFBundleTypeRole", role);
            node.SetString("CFBundleURLName", id);
            
            var schemeList = FindOrCreateArray(node, "CFBundleURLSchemes");
            foreach (var scheme in schemes)
            {
                AddStringIfNo(schemeList, scheme);
            }
#endif
        }

        public void AddApplicationQueriesSchemes(string scheme)
        {
#if UNITY_IOS_API
            AddStringIfNo(FindOrCreateArray(root, "LSApplicationQueriesSchemes"), scheme);
#endif
        }
        
#if UNITY_IOS_API
        private UnityEditor.iOS.Xcode.PlistElementDict FindOrCreateDict(string key)
        {
            if (root.values.ContainsKey(key))
            {
                return root[key].AsDict();
            }
            else
            {
                return root.CreateDict(key);
            }
        }

        private static UnityEditor.iOS.Xcode.PlistElementArray FindOrCreateArray(UnityEditor.iOS.Xcode.PlistElementDict node, string key)
        {
            if (node.values.ContainsKey(key))
            {
                return node[key].AsArray();
            }
            else
            {
                return node.CreateArray(key);
            }
        }

        private static void AddStringIfNo(UnityEditor.iOS.Xcode.PlistElementArray array, string value)
        {
            if (array.values.Find(element => element.AsString().Equals(value)) == null)
            {
                array.AddString(value);
            }
        }
#endif
    }
}