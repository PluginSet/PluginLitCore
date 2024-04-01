using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FolderDragAttribute: LogicPropertyAttribute
    {
        private string _validExtension;
        private string _parentPath;
        
        public FolderDragAttribute()
            :this(null)
        {
            
        }

        public FolderDragAttribute(string extension, string parentPath = ".")
        {
            _validExtension = extension;
            _parentPath = parentPath;
        }
        
#if UNITY_EDITOR
        public override void BeginProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            if (Event.current.type != EventType.DragUpdated && Event.current.type != EventType.DragPerform)
                return;

            if (!position.Contains(Event.current.mousePosition))
                return;
            
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            if (Event.current.type != EventType.DragPerform)
                return;
            
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                var path = DragAndDrop.paths[0];
                if (!string.IsNullOrEmpty(_validExtension))
                {
                    if (!_validExtension.Equals(Path.GetExtension(path)))
                    {
                        return;
                    }
                }

                var fullPath = path;
                if (path.StartsWith("Assets"))
                {
                    fullPath = Path.Combine(".", path);
                }

                path = Global.GetRelativePath(_parentPath, fullPath);
                property.stringValue = path;
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
#endif
    }
}