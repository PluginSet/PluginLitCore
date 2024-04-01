using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PasswordAttribute: DrawablePropertyAttribute
    {
#if UNITY_EDITOR
        public override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.PasswordField(position, label, property.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = value;
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
#endif
    }
}