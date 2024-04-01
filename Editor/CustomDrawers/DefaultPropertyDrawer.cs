using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public class DefaultPropertyDrawer: CustomPropertyBaseDrawer
    {
        protected override void PropertyField(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.hasVisibleChildren && property.isExpanded)
                EditorGUI.PropertyField(position, property, label, true);
            else
                EditorGUI.PropertyField(position, property, label, false);
        }
    }
}