using System;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BrowserPathAttribute : DrawablePropertyAttribute
    {
        public string Title;
        
        public BrowserPathAttribute(string title)
        {
            Title = title;
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
                else
                    current = Path.Combine(Application.dataPath, current);

                string path = EditorUtility.SaveFolderPanel(this.Title, current, Application.dataPath);
                if (!string.IsNullOrEmpty(path))
                {
                    path = Global.GetRelativePath(Application.dataPath, path);
                    property.stringValue = path;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            
            var labelRect = new Rect(position.x + width + space, position.y, position.width - width - space, position.height);
            GUI.Label(labelRect, property.stringValue);
        }
#endif
    }
}
