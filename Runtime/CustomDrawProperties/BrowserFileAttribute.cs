using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BrowserFileAttribute : DrawablePropertyAttribute
    {
        public string Title;
        public string Extestion;
        
        public BrowserFileAttribute(string title, string extension)
        {
            Title = title;
            Extestion = extension;
        }

#if UNITY_EDITOR
        public override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            float space = 0;
            float width = EditorGUIUtility.labelWidth;
            var buttonRect = new Rect(position.x, position.y, width, position.height);
            
            if (GUI.Button(buttonRect, label))
            {
                var current = property.stringValue;
                if (string.IsNullOrEmpty(current))
                    current = Application.dataPath;
                string path = EditorUtility.OpenFilePanel(this.Title, current, this.Extestion);
                if (!string.IsNullOrEmpty(path))
                {
                    var projectPath = Path.Combine(Application.dataPath, "..");
                    path = Global.GetRelativePath(projectPath, path);
                    property.stringValue = path;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            
            var labelRect = new Rect(position.x + width + space, position.y, position.width - width - space, position.height);
            EditorGUI.LabelField(labelRect, new GUIContent(property.stringValue));
        }
#endif
    }
}
