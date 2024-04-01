using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class LogicPropertyAttribute: PropertyAttribute
    {
        public LogicPropertyAttribute()
            :this(1)
        {
        }
        
        public LogicPropertyAttribute(int order)
        {
            this.order = order;
        }

#if UNITY_EDITOR
        public abstract void BeginProperty(Rect position, SerializedProperty property, GUIContent label);

        public virtual void EndProperty()
        {
    }
#endif
    }
}