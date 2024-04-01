using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginLit.Core.Editor
{
    [CustomEditor(typeof(EditorSetting))]
    public class EditorSettingDrawer : SerializedDataSetDrawer
    {
        private static IEnumerable<string> CreatedChannels()
        {
            var path = BuildChannels.MainPath;
            var pattern = BuildChannels.GetFileNameInternal("*") + ".asset";
            var list = new List<string>();
            foreach (var file in Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(file);
                if (asset is BuildChannels)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var name = GetChannelName(fileName);
                    if (string.IsNullOrEmpty(name))
                        continue;

                    list.Add(name);
                }
            }

            return list;
        }

        private static string GetChannelName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            var rp = BuildChannels.GetFileNameInternal("(?<channel>.+)");
            var match = Regex.Match(fileName, rp);
            var name = match.Groups["channel"]?.Value;
            return name;
        }

        private static IEnumerable<string> CustomChannels()
        {
            return from prop in Global.GetProperties<EditorChannelMenuAttribute>()
                from val in (IEnumerable<string>) prop.GetValue(null, null)
                select val;
        }

        private static void ShowChannelMenuItems(string current, Action<string> callback)
        {
            var menu = new GenericMenu();

            foreach (var channel in CreatedChannels())
            {
                menu.AddItem(new GUIContent(channel), current.Equals(channel), delegate(object data)
                {
                    callback?.Invoke((string) data);
                }, channel);
            }

            menu.AddSeparator("");

            foreach (var channel in CustomChannels())
            {
                menu.AddItem(new GUIContent($"Custom/{channel}"), current.Equals(channel), delegate(object data)
                {
                    callback?.Invoke((string) data);
                }, channel);
            }

            menu.ShowAsContext();
        }

        private bool _channelChanged = true;
        private Object _channelAsset;

        const string fpLocalBuild = @"\\192.168.199.111\share\打包机共享\build";

        private static int GetLocalBuildNumber()
        {
            var build = 1;
#if UNITY_EDITOR_OSX
#else
            try
            {
                if (File.Exists(fpLocalBuild))
                {
                    build = int.Parse(File.ReadAllText(fpLocalBuild));
                }
                else
                {
                    Debug.LogError($"读取 {fpLocalBuild} 里面的 build 号失败");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
#endif

            return build;
        }

        private static void SaveLocalBuildNumber(int build)
        {
#if UNITY_EDITOR_OSX
#else
            try
            {
                File.WriteAllText(fpLocalBuild, build.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
#endif
        }

        // [MenuItem("Test/Test LAN IO")]
        // public static void TestLanIO()
        // {
        //     const string fp = @"\\192.168.199.111\share\打包机共享\build";
        //     if (File.Exists(fp))
        //     {
        //         var content = File.ReadAllText(fp);
        //         var build = int.Parse(content) + 1;
        //         Debug.Log(content);
        //         File.WriteAllText(fp, build.ToString());
        //     }
        //     else
        //     {
        //         Debug.Log("file not found");
        //     }
        // }

        protected override void OnDrawSelf()
        {
            var channel = serializedObject.FindProperty("CurrentChannel");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent(channel.displayName, "编辑器下当前渠道配置"));

            var currentChannel = channel.stringValue ?? "default";
            if (EditorGUILayout.DropdownButton(new GUIContent(currentChannel), FocusType.Passive, GUILayout.MinWidth(100)))
            {
                ShowChannelMenuItems(currentChannel, delegate(string sel)
                {
                    if (string.IsNullOrEmpty(sel))
                        return;

                    if (channel.stringValue.Equals(sel))
                        return;

                    _channelChanged = true;
                    channel.stringValue = sel;
                    channel.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                });
            }

            if (_channelChanged || _channelAsset == null)
            {
                _channelAsset = BuildChannels.GetAsset(channel.stringValue);
                _channelChanged = false;
            }

            EditorGUI.BeginChangeCheck();
            var obj = EditorGUILayout.ObjectField(_channelAsset, typeof(BuildChannels), false);
            if (EditorGUI.EndChangeCheck())
            {
                _channelAsset = obj;
                channel.stringValue = GetChannelName(obj.name);
                channel.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorGUILayout.EndHorizontal();

            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (iterator.displayName.Equals("Script"))
                    continue;

                if (iterator.displayName.Equals(channel.displayName))
                    continue;

                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(iterator, true);
                    if (EditorGUI.EndChangeCheck())
                        iterator.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var syncSettings = GUILayout.Button("同步设置");

            if (syncSettings)
            {
                BuildHelper.SyncPluginsConfig();
            }

            AssetDatabase.SaveAssets();
        }
    }
}
