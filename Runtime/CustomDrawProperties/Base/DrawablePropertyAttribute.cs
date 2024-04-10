using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class DrawablePropertyAttribute: PropertyAttribute
    {
        public DrawablePropertyAttribute()
            :this(10000)
        {
        }
        
        public DrawablePropertyAttribute(int order)
        {
            this.order = order;
        }
        
#if UNITY_EDITOR
        public abstract void DrawProperty(Rect position, SerializedProperty property, GUIContent label);

        public virtual bool UseCustomHeight { get; } = false;

        public virtual float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
#endif
    }
}