using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public static class MenuExternsion
    {
        [MenuItem("Assets/Build AssetBundles")]
        public static void BuildAssetBundles()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var path = Application.streamingAssetsPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, target);
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Assets/Find References", false, 10)]
        public static void FindReference()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            
            var targets = from obj in Selection.objects
                select AssetDatabase.GetAssetPath(obj);

            var searchUids = Global.GetAssetsUids(targets.ToArray());
            if (!string.IsNullOrEmpty(searchUids))
            {
                int startIndex = 0;
                string[] files = Global.GetRelyableFiles();
 
                EditorApplication.update = delegate()
                {
                    string file = files[startIndex];
            
                    bool isCancel = EditorUtility.DisplayCancelableProgressBar("匹配资源中", file, (float)startIndex / (float)files.Length);
 
                    if (Regex.IsMatch(File.ReadAllText(file), searchUids))
                    {
                        Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
                    }
 
                    startIndex++;
                    if (isCancel || startIndex >= files.Length)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorApplication.update = null;
                        startIndex = 0;
                        Debug.Log("匹配结束");
                    }
 
                };
            }
        }
        
        private static string GetRelativeAssetsPath(string path)
        {
            return "Assets" + Path.GetFullPath(path).Replace(Path.GetFullPath(Application.dataPath), "").Replace('\\', '/');
        }

    }
}