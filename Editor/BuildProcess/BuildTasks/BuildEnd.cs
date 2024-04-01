using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public class BuildEnd : BuildProcessorTask
    {
        public override void Execute(BuildProcessorContext context)
        {
            var outJson = context.TryGet<Dictionary<string, object>>("buildResultJson", null);
            if (outJson != null && !string.IsNullOrEmpty(context.BuildPath) && Directory.Exists(context.BuildPath))
            {
                outJson["channel"] = context.Channel;
                outJson["version"] = context.VersionName;
                outJson["build"] = context.Build;
                var outJsonFile = Path.Combine(context.BuildPath, "buildResult.json");
                File.WriteAllText(outJsonFile, MiniJson.Serialize(outJson));
            }

            // clear temp paths
            foreach (var path in context.TemplatePaths)
            {
                Global.CheckAndDeletePath(path);
            }

            AssetDatabase.Refresh();
            
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
    }
}
