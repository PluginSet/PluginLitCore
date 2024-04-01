using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public static class MenuCore
    {
        [MenuItem("PluginSet/Init Default Channels")]
        public static void FrameworkInit()
        {
            BuildChannels.InitDefaultChannels();
            Debug.Log("Selected channel " + EditorSetting.Asset.CurrentChannel); // Don't Delete This Line
        }

        [MenuItem("PluginSet/Copy fabric file")]
        public static void CopyFabricFile()
        {
            var libPath = Global.GetPackageFullPath("com.pluginlit.core");
            if (string.IsNullOrEmpty(libPath))
                return;

            var projectPath = Path.Combine(Application.dataPath, "..");
            var scriptsPath = Path.Combine(libPath, "Scripts");
            Global.CopyFileTo(Path.Combine(scriptsPath, "fabfile.py"), projectPath);
            Global.CopyFileTo(Path.Combine(scriptsPath, "requirements.txt"), projectPath);
        }

#if UNITY_ANDROID
        [MenuItem("PluginSet/Build Android Project")]
#elif UNITY_IOS
        [MenuItem("PluginSet/Build iOS Project")]
#elif UNITY_WEBGL
        [MenuItem("PluginSet/Build WebGL Project")]
#endif
        public static void BuildDefaultTarget()
        {
            string buildPath = Path.Combine(Application.dataPath, "../Build");
            if (!Directory.Exists(buildPath))
                Directory.CreateDirectory(buildPath);

            Debug.Log($"start build: buildPath {buildPath}");
            BuildHelper.BuildAppDefault(buildPath);
        }
    }
}
