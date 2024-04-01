using System.IO;
using UnityEditor;

namespace PluginLit.Core.Editor
{
    public static class BuildUtils
    {
#if UNITY_ANDROID
        public static AndroidProjectManager GetProjectManager(string projectPath)
#elif UNITY_IOS
        public static PBXProjectManager GetProjectManager(string projectPath)
#else
        public static object GetProjectManager(string projectPath)
#endif
        {
#if UNITY_ANDROID
            return new AndroidProjectManager(projectPath);
#elif UNITY_IOS
            string xcodeProjectPath = Path.Combine(projectPath, "Unity-iPhone.xcodeproj", "project.pbxproj");
            var bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
            var bundleStrings = bundleId.Split('.');
            return new PBXProjectManager(xcodeProjectPath
                , $"Unity-iPhone/{bundleStrings[bundleStrings.Length - 1]}.entitlements"
                , "Unity-iPhone");
#else
            return null;
#endif
        }
        
    }
}