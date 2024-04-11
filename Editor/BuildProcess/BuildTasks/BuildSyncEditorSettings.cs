
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public class BuildSyncEditorSettings : BuildProcessorTask
    {
        public override void Execute(BuildProcessorContext context)
        {
            var config = PluginsConfig.NewAsset;
            context.SetPluginsConfig(config);
            
            // sync context
            Global.CallCustomOrderMethods<OnSyncEditorSettingAttribute, BuildToolsAttribute>(context);
            CompleteEditorSettings(context, config);
        }
        
        private static void CompleteEditorSettings(BuildProcessorContext context, PluginsConfig config)
        {
            // sync setting
            PlayerSettings.SplashScreen.show = context.BuildChannels.ShowUnityLogo;

            if (context.DebugMode)
                context.Symbols.Add("DEBUG");
            context.Symbols.Sort(string.CompareOrdinal);
            var symbols = string.Join(";", context.Symbols.Distinct());
            var curSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(context.BuildTargetGroup);
            Debug.Log($"SymbolsForGroup change current:{curSymbols} new:{symbols} isEquals:{curSymbols.Equals(symbols)}");
            if (!curSymbols.Equals(symbols))
                PlayerSettings.SetScriptingDefineSymbolsForGroup(context.BuildTargetGroup, symbols);

            PlayerSettings.bundleVersion = context.VersionName;

            if (context.BuildTarget == BuildTarget.Android)
            {
                PlayerSettings.Android.bundleVersionCode = int.Parse(context.VersionCode);
            }
            else if (context.BuildTarget == BuildTarget.iOS)
            {
                PlayerSettings.iOS.buildNumber = context.VersionCode;
            }
            Debug.Log($"sync version and build version:{context.VersionName} build:{context.VersionCode}");
            string linkFileName = Path.Combine(Application.dataPath, "link.xml");
            Global.CheckAndRemoveFile(linkFileName);
            var linkDoc = context.LinkDocument;
            if (linkDoc != null)
            {
                linkDoc.Save(linkFileName);
            }

            string unsafeFileName = Path.Combine(Application.dataPath, "smcs.rsp");
            if (PlayerSettings.allowUnsafeCode)
            {
                if (!File.Exists(unsafeFileName))
                {
                    File.WriteAllText(unsafeFileName, "-unsafe");
                }
            }
            else
            {
                Global.CheckAndRemoveFile(unsafeFileName);
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"config asset::{string.Join("\n", config.DataItems.Select(item => item.ToString()).ToArray())}");
            context.IsWaiting = false;
        }
    }
}
