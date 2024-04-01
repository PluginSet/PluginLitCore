
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
    public class DropDownListAttribute: DrawablePropertyAttribute
    {
        private string _listGetterName;

        public DropDownListAttribute(string listGetterName)
        {
            _listGetterName = listGetterName;
        }
        
#if UNITY_EDITOR
        private IEnumerable<string> GetDropListSelections(SerializedObject serializedObject)
        {
            var targetObject = serializedObject?.targetObject;
            
            if (targetObject is null)
                return null;

            var property = targetObject.GetType().GetProperty(_listGetterName);
            if (property == null)
                return null;
            return property.GetValue(targetObject) as IEnumerable<string>;
        }

        private void ShowDropListMenu(SerializedProperty property, Action<string> callback)
        {
            var menu = new GenericMenu();
            var current = property.stringValue;
            var selections = GetDropListSelections(property.serializedObject);
            if (selections != null)
            {
                foreach (var selection in selections)
                {
                    menu.AddItem(new GUIContent(selection), current.Equals(selection), delegate(object data)
                    {
                        callback?.Invoke((string) data);
                    }, selection);
                }
            }
            menu.ShowAsContext();
        }
        
        public override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            float space = 0;
            float width = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(position.x, position.y, width, position.height);
            GUI.Label(labelRect, label);
            
            var dropButtonRect = new Rect(position.x + width + space, position.y, position.width - width - space, position.height);
            if (EditorGUI.DropdownButton(dropButtonRect, new GUIContent(property.stringValue), FocusType.Passive))
            {
                ShowDropListMenu(property, delegate(string val)
                {
                    if (string.IsNullOrEmpty(val) || val.Equals(property.stringValue))
                        return;
                    
                    property.stringValue = val;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                });
            }
        }
#endif
    }
}