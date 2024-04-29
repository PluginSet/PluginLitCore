using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LayerValueAttribute: DrawablePropertyAttribute
    {
#if UNITY_EDITOR
        public override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            var optionStrings = new List<string>();
            var optionNumbers = new List<int>();
            
            for (int i = 0; i < 32; i++)
            {
                var name = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(name))
                    continue;
                
                optionNumbers.Add(i);
                optionStrings.Add(name);
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PrefixLabel(position, label);
            var rect = position;
            rect.xMin += EditorGUIUtility.labelWidth + 2f;
            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.IntPopup(rect, property.intValue, optionStrings.ToArray(), optionNumbers.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = value;
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorGUI.EndProperty();
        }
#endif
    }
}
