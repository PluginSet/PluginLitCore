using System;
#if UNITY_EDITOR
using System.Reflection;
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

        public override bool UseCustomHeight => true;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {

            if (property.isExpanded)
            {
                var pairs = property.FindPropertyRelative("Pairs");
                if (pairs != null && pairs.isArray)
                {
                    var count = pairs.arraySize;
                    return (count + 1) * EditorGUIUtility.singleLineHeight + 2f * count;
                }
            }
            return EditorGUIUtility.singleLineHeight;
        }

        public override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            var pairs = property.FindPropertyRelative("Pairs");
            if (pairs == null || !pairs.isArray)
                return;

            var pos = position.position;
            var width = position.width;
            var size = pairs.arraySize;
            pairs.isExpanded = EditorGUI.Foldout(
                new Rect(pos, new Vector2(width * 0.8f, EditorGUIUtility.singleLineHeight))
                , pairs.isExpanded, $"{label.tooltip ?? label.text}:Count = {size}");
            property.isExpanded = pairs.isExpanded;
            
            EditorGUI.BeginChangeCheck();
            size = EditorGUI.IntField(new Rect(new Vector2(pos.x + width * 0.8f, pos.y), new Vector2(width * 0.2f, EditorGUIUtility.singleLineHeight)), size);
            if (EditorGUI.EndChangeCheck())
            {
                pairs.arraySize = size;
                pairs.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            pos.y += EditorGUIUtility.singleLineHeight + 2f;
            
            if (pairs.isExpanded)
            {
                EditorGUI.BeginChangeCheck();
                CheckKeysStart();
                for (int i = 0; i < size; i++)
                {
                    var tendPos = new Vector2(pos.x + 6f, pos.y);
                    var item = pairs.GetArrayElementAtIndex(i);
                    var key = item.FindPropertyRelative("Key");
                    var color = GUI.contentColor;
                    if (!CheckKeyIsValid(key))
                        GUI.contentColor = Color.red;

                    EditorGUI.PropertyField(
                        new Rect(tendPos, new Vector2(width * 0.4f, EditorGUIUtility.singleLineHeight))
                        , key, GUIContent.none);
                    GUI.contentColor = color;

                    tendPos.x += width * 0.4f + 6f;
                    EditorGUI.LabelField(new Rect(tendPos, new Vector2(20, EditorGUIUtility.singleLineHeight)), "=>");
                    tendPos.x += 26f;
                    EditorGUI.PropertyField(Rect.MinMaxRect(tendPos.x, tendPos.y, position.xMax,
                            tendPos.y + EditorGUIUtility.singleLineHeight)
                        , item.FindPropertyRelative("Value"), GUIContent.none);

                    pos.y += EditorGUIUtility.singleLineHeight + 2f;
                }

                if (EditorGUI.EndChangeCheck())
                    property.serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}