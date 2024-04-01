using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor.WebGL
{
    [BuildTools]
    [BuildChannelsParams("WebGL", -9998, "h5打包相关设置")]
    [VisibleCaseBoolValue("SupportWebGL", true)]
    public class WebGLBuildParams : ScriptableObject
    {
        public enum WebGLTemplates
        {
            Default,
            Minimal,
        }

        [Tooltip("导出时设置的默认画布宽度")] public int DefaultCanvasWidth = 960;
        [Tooltip("导出时设置的默认画布高度")] public int DefaultCanvasHeight = 640;
        [Tooltip("导出时设置的在后台运行")] public bool RunInBackground = true;
        [Tooltip("导出时设置的导出模板")] public WebGLTemplates WebGLTemplate = WebGLTemplates.Default;

        public WebGLCompressionFormat CompressionFormat = WebGLCompressionFormat.Gzip;

        public Object[] PreloadAssets = null;

        [Tooltip("额外模板资源")] [BrowserPath("Choose a Directory")]
        public string ExtraTemplates;

        [OnSyncEditorSetting]
        public static void OnSyncExportSetting_webgl(BuildProcessorContext context)
        {
            if (context.BuildTarget != BuildTarget.WebGL)
                return;
            
            var asset = context.BuildChannels;
            var setting = asset.Get<WebGLBuildParams>();

            PlayerSettings.defaultWebScreenWidth = setting.DefaultCanvasWidth;
            PlayerSettings.defaultWebScreenHeight = setting.DefaultCanvasHeight;
            PlayerSettings.runInBackground = setting.RunInBackground;
            PlayerSettings.WebGL.compressionFormat = setting.CompressionFormat;
#if UNITY_2020_1_OR_NEWER
            PlayerSettings.WebGL.decompressionFallback = setting.CompressionFormat != WebGLCompressionFormat.Disabled;
#endif
#if UNITY_2021_1_OR_NEWER
            PlayerSettings.WebGL.debugSymbolMode = context.DebugMode ? WebGLDebugSymbolMode.External : WebGLDebugSymbolMode.Off;
#else
            PlayerSettings.WebGL.debugSymbols = context.DebugMode;
#endif

            if (context.ProductMode)
                PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);

            switch (setting.WebGLTemplate)
            {
                case WebGLTemplates.Default:
                case WebGLTemplates.Minimal:
                    PlayerSettings.WebGL.template = $"APPLICATION:{setting.WebGLTemplate.ToString()}";
                    break;
                default:
                    throw new BuildException("Unsupported webGL template:: " + setting.WebGLTemplate);
            }

            PlayerSettings.SetPreloadedAssets(setting.PreloadAssets);
        }
    }
}