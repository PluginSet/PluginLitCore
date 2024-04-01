using System;
using System.Reflection;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializableDictAttribute: DrawablePropertyAttribute
    {
        private object EmptyKey = null;
#if UNITY_EDITOR
        private Func<SerializedProperty, object> KeyValueGetter;
#endif
        
        public SerializableDictAttribute(string keyPropName, object emptyValue = null)
        {
            EmptyKey = emptyValue;

#if UNITY_EDITOR
            if ("enumValue".Equals(keyPropName))
            {
                EmptyKey = emptyValue?.ToString();
                
                KeyValueGetter = delegate(SerializedProperty property)
                {
                    var index = property.enumValueIndex;
                    if (index < 0 || index >= property.enumNames.Length)
                        return EmptyKey;

                    return property.enumNames[index];
                };
            }
            else
            {
                var propertyInfo = typeof(SerializedProperty).GetProperty(keyPropName);
                KeyValueGetter = property => propertyInfo?.GetValue(property);
            }
#endif
        }
        
#if UNITY_EDITOR
        private bool _layouted = false;
        
        private List<object> keys = new List<object>();

        private void CheckKeysStart()
        {
            keys.Clear();
        }

        private bool CheckKeyIsValid(SerializedProperty key)
        {
            var value = GetKeyValue(key);
            if (value.Equals(EmptyKey))
                return false;

            if (keys.Contains(value))
                return false;
            
            keys.Add(value);
            return true;
        }

        private object GetKeyValue(SerializedProperty key)
        {
            return KeyValueGetter?.Invoke(key);
        }
        
        public override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            
            if (Event.current.type == EventType.Repaint && !_layouted)
                return;

            if (Event.current.type == EventType.Layout)
                _layouted = true;
            else
                _layouted = false;
            
            var pairs = property.FindPropertyRelative("Pairs");
            if (pairs == null || !pairs.isArray)
                return;
            
            var size = pairs.arraySize;
            pairs.isExpanded = EditorGUILayout.Foldout(pairs.isExpanded, $"{label.tooltip ?? label.text}:Count = {size}");
            
            if (pairs.isExpanded)
            {
                EditorGUI.BeginChangeCheck();
                size = EditorGUILayout.IntField("Count", size);
                if (EditorGUI.EndChangeCheck())
                {
                    pairs.arraySize = size;
                    pairs.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                
                EditorGUI.BeginChangeCheck();
                CheckKeysStart();
                for (int i = 0; i < size; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Separator();
                    
                    var item = pairs.GetArrayElementAtIndex(i);
                    var key = item.FindPropertyRelative("Key");
                    var color = GUI.contentColor;
                    if (!CheckKeyIsValid(key))
                        GUI.contentColor = Color.red;
                    EditorGUILayout.PropertyField(key, GUIContent.none);
                    GUI.contentColor = color;
                    
                    EditorGUILayout.LabelField("=>", GUILayout.MaxWidth(40));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("Value"), GUIContent.none);

                    EditorGUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck())
                    property.serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}