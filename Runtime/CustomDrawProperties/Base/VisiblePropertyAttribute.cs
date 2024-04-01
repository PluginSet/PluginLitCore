using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace PluginLit.Core
{
    public abstract class VisiblePropertyAttribute: PropertyAttribute
    {
        public VisiblePropertyAttribute()
            :this(1)
        {
        }
        
        public VisiblePropertyAttribute(int order)
        {
            this.order = order;
        }
        
#if UNITY_EDITOR
        public abstract bool IsVisible(SerializedObject property);
        public abstract bool IsVisible(ScriptableObject asset);

        public static T GetValue<T>(ScriptableObject asset, string propName, T def = default(T))
        {
            var type = asset.GetType();
            var prop = type.GetProperty(propName);
            if (prop != null)
                return (T)prop.GetValue(asset);

            var field = type.GetField(propName);
            if (field != null)
                return (T)field.GetValue(asset);
            
            return def;
        }

        private static bool IsVisibleInternal(MemberInfo fieldInfo, ScriptableObject asset)
        {
            var attrs = fieldInfo.GetCustomAttributes(typeof(VisiblePropertyAttribute), true);
            if (attrs.Length <= 0)
                return true;
            
            foreach (var attr in attrs)
            {
                if (((VisiblePropertyAttribute) attr).IsVisible(asset))
                {
                    return true;
                }
            }

            return false;
        }
        
        private static bool IsVisibleInternal(MemberInfo fieldInfo, SerializedObject sObject)
        {
            var attrs = fieldInfo.GetCustomAttributes(typeof(VisiblePropertyAttribute), true);
            if (attrs.Length <= 0)
                return true;
            
            foreach (var attr in fieldInfo.GetCustomAttributes(typeof(VisiblePropertyAttribute), true))
            {
                if (((VisiblePropertyAttribute) attr).IsVisible(sObject))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsVisible(MemberInfo fieldInfo, SerializedObject sObject)
        {
            if (IsVisibleInternal(fieldInfo, sObject))
                return true;
            
            var target = sObject.targetObject;
            if (AssetDatabase.IsMainAsset(target))
                return false;

            var path = AssetDatabase.GetAssetPath(target);
            var root = AssetDatabase.LoadMainAssetAtPath(path);
            if (root is ScriptableObject o)
                return IsVisibleInternal(fieldInfo, o);
            
            return false;
        }
#endif
    }
}