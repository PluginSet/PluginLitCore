using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DisableEditAttribute: LogicPropertyAttribute
    {
#if UNITY_EDITOR
        public override void BeginProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
        }

        public override void EndProperty()
        {
            GUI.enabled = true;
        }
#endif
    }
}