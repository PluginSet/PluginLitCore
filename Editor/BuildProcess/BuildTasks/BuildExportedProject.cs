using System.IO;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
	public class BuildExportedProject : BuildProcessorTask
	{
		public override void Execute(BuildProcessorContext context)
		{
			var streamingAssetsPath = Global.StreamingAssetsPath;
			if (!string.IsNullOrEmpty(streamingAssetsPath))
			{
				Global.ClearDirectory(streamingAssetsPath, "*.DS_Store");
				Global.ClearDirectory(streamingAssetsPath, "*.meta");
				Global.ClearDirectory(streamingAssetsPath, "*.manifest");
				AssetDatabase.Refresh();
			}

			BuildOptions buildOption = BuildOptions.None;

			bool debugMode = context.DebugMode;
#if !UNITY_WEBGL
			if (debugMode)
			{
				buildOption |= BuildOptions.Development;
				buildOption |= BuildOptions.AllowDebugging;
//				buildOption |= BuildOptions.ConnectWithProfiler;
			}
#endif

			var target = context.BuildTarget;
			var buildPath = context.ProjectPath;
			string locationPath = buildPath;
			if (target == BuildTarget.iOS)
			{
				// 导出目录保持一致
#if !UNITY_2020_1_OR_NEWER
				locationPath = Path.Combine(buildPath , PlayerSettings.productName);
#endif
#if UNITY_2021_3_OR_NEWER
				EditorUserBuildSettings.iOSXcodeBuildConfig =
					debugMode ? XcodeBuildConfig.Debug : XcodeBuildConfig.Release;
#else
				EditorUserBuildSettings.iOSBuildConfigType = debugMode ? iOSBuildType.Debug : iOSBuildType.Release;
#endif
			}
			else if (target == BuildTarget.Android)
			{
				EditorUserBuildSettings.androidBuildType =
					debugMode ? AndroidBuildType.Debug : AndroidBuildType.Release;
				EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

				context.SetBuildResult("keystoreName",
					Path.GetFullPath(Path.Combine(".", PlayerSettings.Android.keystoreName)));
				context.SetBuildResult("keyaliasName", PlayerSettings.Android.keyaliasName);
				context.SetBuildResult("keystorePass", PlayerSettings.Android.keystorePass);
				context.SetBuildResult("keyaliasPass", PlayerSettings.Android.keyaliasPass);
			}
			else if (target == BuildTarget.WebGL)
			{
				// PlayerSettings.WebGL.emscriptenArgs = "-s \"BINARYEN_TRAP_MODE='clamp'\"";
			}
			else
			{
				throw new BuildException($"Unsupported build target {target}");
			}

			Debug.Log("local path::::::::::: " + locationPath);

			Global.CheckAndDeletePath(locationPath);
			if (!Directory.Exists(locationPath))
				Directory.CreateDirectory(locationPath);

			EditorUserBuildSettings.development = debugMode;
#if UNITY_2021_1_OR_NEWER
			EditorUserBuildSettings.androidCreateSymbols =
				context.ProductMode ? AndroidCreateSymbols.Public : AndroidCreateSymbols.Disabled;
#elif UNITY_2018_4_OR_NEWER && (!UNITY_2019_1_OR_NEWER || UNITY_2019_2_OR_NEWER)
			EditorUserBuildSettings.androidCreateSymbolsZip = context.ProductMode;
#endif
			BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, locationPath, target, buildOption);
		}
	}
}
