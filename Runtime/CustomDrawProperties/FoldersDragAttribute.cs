#if false
using System;
#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FoldersDragAttribute: LogicPropertyAttribute
    {
        private string _fieldName;
        private string _childPropertyName;
        
        public FoldersDragAttribute(string fieldName, string childPropertyName = null)
        {
            _fieldName = fieldName;
            _childPropertyName = childPropertyName;
        }
        
        
#if UNITY_EDITOR
        public override void BeginProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedObject parent = null;
            if (!property.isArray && !string.IsNullOrEmpty(_childPropertyName))
            {
                parent = property.serializedObject;
                property = parent.FindProperty(_childPropertyName);
            }
            
            if (property == null || !property.isArray)
                return;
            
            if (Event.current.type == EventType.DragExited)
            {
                if (!position.Contains(Event.current.mousePosition))
                    return;
                    
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    foreach (var path in DragAndDrop.paths)
                    {
                        if (!Directory.Exists(path))
                            continue;

                        var assetPath = Global.GetSubPath(".", path);
                        if (!assetPath.StartsWith("Assets/"))
                            continue;

                        bool exsist = false;
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            var ele = property.GetArrayElementAtIndex(i);
                            if (assetPath.Equals(ele.FindPropertyRelative(_fieldName).stringValue))
                            {
                                exsist = true;
                                break;
                            }
                        }
                        if (exsist)
                            continue;

                        var index = property.arraySize++;
                        var element = property.GetArrayElementAtIndex(index);
                        var prop = element.FindPropertyRelative(_fieldName);
                        prop.stringValue = assetPath;
                        element.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    parent?.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
#endif
    }
}
#endif