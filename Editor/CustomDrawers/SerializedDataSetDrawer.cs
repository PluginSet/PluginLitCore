using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginLit.Core.Editor
{
    [CustomEditor(typeof(SerializedDataSet), true), CanEditMultipleObjects]
    public class SerializedDataSetDrawer : UnityEditor.Editor
    {
        private readonly Dictionary<string, SerializedProperty> DataItemProperties = new Dictionary<string, SerializedProperty>();

        private GUIStyle lineFontStyle;

        private SerializedProperty dataItemsProp;
//        private bool _layouted;

        private void OnEnable()
        {
            lineFontStyle = new GUIStyle
            {
                fontSize = 12,
                normal = {textColor = new Color(0x18 / 255f, 0xab / 255f, 0xf8 / 255f)},
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }

        private void ResetProps()
        {
//            _layouted = false;
            SerializedDataSet self = (SerializedDataSet) target;
            self.OnLoad();

            serializedObject.Update();
            DataItemProperties.Clear();

            dataItemsProp = serializedObject.FindProperty("DataItems");
            if (dataItemsProp == null || !dataItemsProp.isArray)
                return;

            for (int i = 0; i < dataItemsProp.arraySize; i++)
            {
                var item = dataItemsProp.GetArrayElementAtIndex(i);
                var key = item.FindPropertyRelative("Key").stringValue;
                if (!string.IsNullOrEmpty(key))
                {
                    if (DataItemProperties.ContainsKey(key))
                    {
                        Debug.LogError($"DataItemProperties ContainsKey key {key}");
                        continue;
                    }
                    DataItemProperties.Add(key, item);
                }
            }
        }

        protected virtual void OnDrawSelf()
        {
            base.OnInspectorGUI();
        }

        public override void OnInspectorGUI()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

//            if (Event.current.type == EventType.Repaint && !_layouted)
//                return;
//
//            _layouted = Event.current.type == EventType.Layout;

            // check props
            SerializedDataSet self = (SerializedDataSet) target;
            if (self.IsSomeAssetLose())
            {
                ResetProps();
            }

            if (GUILayout.Button("拷贝渠道配置"))
            {
                CloneAsset(this.target);
            }

            serializedObject.Update();

            GUILayout.Label("-- 基础配置 --", lineFontStyle);

            OnDrawSelf();

            if (dataItemsProp == null)
            {
                ResetProps();
            }

            if (dataItemsProp.hasMultipleDifferentValues)
            {
                return;
            }

            var styleDisabledFoldout = new GUIStyle(EditorStyles.foldout)
            {
                normal = {textColor = Color.gray}
            };
            GUILayout.Space(8);
            GUILayout.Label("-- 扩展配置 --", lineFontStyle);

            bool dirty = false;
            foreach (var info in self.SerializedTypes)
            {
                if (DataItemProperties.TryGetValue(info.Key, out var prop))
                {
                    if (!VisiblePropertyAttribute.IsVisible(info.ClassType, serializedObject))
                        continue;

                    var item = self.GetItem(info.Key);

                    var tips = info.Key;

                    if (!string.IsNullOrEmpty(info.ToolTips))
                        tips = $"{tips}: ({info.ToolTips})";

                    var enabled = true;
                    var fieldInfoEnabled = item.Data.GetType().GetField("Enable");
                    if (fieldInfoEnabled != null)
                        enabled = (bool) fieldInfoEnabled.GetValue(item.Data);

                    prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, tips, enabled ? EditorStyles.foldout : styleDisabledFoldout);
                    if (!prop.isExpanded)
                        continue;

                    if (item.SerializedObject == null)
                        item.SerializedObject = new SerializedObject(item.Data);

                    var sObj = (SerializedObject) item.SerializedObject;
                    sObj.Update();

                    EditorGUI.BeginChangeCheck();

                    SerializedProperty property = sObj.GetIterator();
                    bool expanded = true;
                    while (property.NextVisible(expanded))
                    {
                        expanded = false;
                        if (property.name == "m_Script")
                            continue;

                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(property, true);
                        EditorGUI.indentLevel--;
                    }

                    sObj.ApplyModifiedProperties();
                    if (EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
            }

            if (dirty)
            {
                self.OnChildChanged();
            }
        }

        private static void CloneAsset(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(fileName))
                return;

            var dir = Path.GetDirectoryName(path);
            var targetName = $"{fileName}_copy";
            int index = 1;
            while (File.Exists(Path.Combine(dir ?? throw new InvalidOperationException("dir is null"), $"{targetName}.asset")))
            {
                targetName = $"{fileName}_copy_{index++}";
            }

            var saveFile = EditorUtility.SaveFilePanelInProject("拷贝渠道配置", targetName, "asset", "保存到文件", dir);
            if (string.IsNullOrEmpty(saveFile))
                return;

            targetName = Path.GetFileNameWithoutExtension(saveFile);
            var clone = Object.Instantiate(asset);
            clone.name = targetName;
            AssetDatabase.CreateAsset(clone, saveFile);

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var sub in allAssets)
            {
                if (sub == null || !AssetDatabase.IsSubAsset(sub))
                    continue;

                var subClone = Object.Instantiate(sub);
                subClone.name = sub.name;
                AssetDatabase.AddObjectToAsset(subClone, clone);
            }

            AssetDatabase.SaveAssets();

            ((SerializedDataSet) clone).Reload();
            EditorUtility.SetDirty(clone);
            AssetDatabase.SaveAssets();
        }
    }
}
