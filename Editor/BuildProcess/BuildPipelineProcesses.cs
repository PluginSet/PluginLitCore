using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public static class BuildPipelineProcesses
    {
	    private static readonly Logger Logger = LoggerManager.GetLogger("BuildPipelineProcesses");

        [PostProcessScene(-99999999)]
        public static void ResetPlayerDefaultBuildContext()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlaying)
                return;

	        Logger.Debug("ResetPlayerDefaultBuildContext 0000000000000000000000");
		        
            BuildProcessorContext context = BuildProcessorContext.Current;
            if (context == null)
            {
	            Logger.Warn("Not supported platform");
	            return;
            }

            if (context.TaskType == BuildTaskType.None)
	            return;
            
            Global.CallCustomOrderMethods<OnCompileCompleteAttribute, BuildToolsAttribute>(context);
			PlayerSettings.SplashScreen.showUnityLogo = context.BuildChannels.ShowUnityLogo;
        }

        [PostProcessBuild(-99999999 )]
        public static void BuildProcessModifyProject(BuildTarget target, string exportPath)
        {
	        Logger.Debug("BuildProcessModifyProject 0000000000000000000000");
	        var context = BuildProcessorContext.Current;
            if (context == null)
            {
	            Debug.LogWarning("Not supported platform");
	            return;
            }

            if (context.TaskType != BuildTaskType.BuildProject)
	            return;
            
			context.ProjectPath = exportPath;
			
			if (target == BuildTarget.Android)
			{
#if UNITY_EDITOR_OSX
				const string gradlewFileName = "gradlew";
#else
				const string gradlewFileName = "gradlew.bat";
#endif
				string command = Path.Combine(exportPath, gradlewFileName);
				if (!File.Exists(command))
				{
					CopyGradleFiles(exportPath);
				}
			}
	        
            var handler = new BuildTaskHandler();
            if (target == BuildTarget.iOS)
            {
                handler.AddNextTask(new BuildModifyIOSProject(exportPath));
            }
            else if (target == BuildTarget.WebGL)
            {
                handler.AddNextTask(new BuildModifyWebGLProject(exportPath));
            }
            handler.Execute(context);
        }
        
        [PostProcessBuild(99999999)]
        public static void BuildProcessCompleted(BuildTarget target, string exportPath)
        {
	        Logger.Debug("BuildProcessCompleted 0000000000000000000000");
	        var context = BuildProcessorContext.Current;
            if (context == null)
            {
	            Debug.LogWarning("Not supported platform");
	            return;
            }
	        
            var handler = new BuildTaskHandler();
			handler.AddNextTask(new BuildSimpleTask(delegate(BuildProcessorContext processorContext)
			{
				Global.CallCustomOrderMethods<BuildProjectCompletedAttribute, BuildToolsAttribute>(processorContext, exportPath);
			}));
            
            handler.Execute(context);
            
			context.SetBuildResult("unityVersion", Application.unityVersion);
			
#if UNITY_IOS
            context.SetBuildResult("bundleId", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS));
            context.SetBuildResult("platform", "iOS");
#elif UNITY_ANDROID
            context.SetBuildResult("bundleId", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
            context.SetBuildResult("platform", "Android");
#elif UNITY_WEBGL
            context.SetBuildResult("bundleId", context.BuildChannels.PackageName);
            context.SetBuildResult("platform", "WebGL");
#endif

	        if (context.TaskType == BuildTaskType.BuildProject)
	        {
				context.SetBuildResult("projectPath", Path.GetFullPath(exportPath));
				if (target == BuildTarget.Android)
				{
					var androidProject = new AndroidProjectManager(exportPath);
					context.SetBuildResult("targetSdkVersion", androidProject.LauncherGradle.TargetSdkVersion);
				}
	        }
        }
        
        
        private static string GetEditorGradleVersion()
        {
	        var gradlePath = Path.Combine(Global.PlaybackEnginesPath, "AndroidPlayer", "Tools", "gradle");
	        if (EditorPrefs.HasKey("GradleUseEmbedded") && !EditorPrefs.GetBool("GradleUseEmbedded"))
		        gradlePath = EditorPrefs.GetString("GradlePath", gradlePath);
	        foreach (var file in Directory.GetFiles(Path.Combine(gradlePath, "lib"), "gradle-*.jar"))
	        {
		        var fileName = Path.GetFileNameWithoutExtension(file);
		        var match = Regex.Match(fileName, "gradle(-\\w+)+-([0-9]+\\.[0-9]+(\\.[0-9]+)?)");
		        if (match.Groups.Count >= 3)
		        {
			        return match.Groups[2].Value;
		        }
	        }

	        return null;
        }

        
        public static void CopyGradleFiles(string targetPath)
        {
	        var localPath = Path.Combine(Global.PlaybackEnginesPath, "AndroidPlayer",
		        "Tools", "VisualStudioGradleTemplates");
	        Global.CopyFileTo(Path.Combine(localPath, "gradlew.bat"), targetPath);

	        var libPath = Global.GetPackageFullPath("com.pluginlit.core");
	        Global.CopyFileTo(Path.Combine(libPath, "Scripts", "gradlew"), targetPath);
	        Global.SetFileExecutable(Path.Combine(targetPath, "gradlew").Replace("\\", "/"));
	        
	        var wrapperPath = Path.Combine(targetPath, "gradle", "wrapper");
	        if (!Directory.Exists(wrapperPath))
		        Directory.CreateDirectory(wrapperPath);
	        
	        Global.CopyFileTo(Path.Combine(localPath, "gradle-wrapper.jar"), wrapperPath);

	        var lineList = new List<string>();
	        var tempFile = Path.Combine(localPath, "gradle-wrapper.properties.template");
	        if (File.Exists(tempFile))
	        {
		        lineList.AddRange(File.ReadAllLines(tempFile));
	        }
	        else
	        {
		        lineList.AddRange(new string[]
		        {
					"distributionBase=GRADLE_USER_HOME",
					"distributionPath=wrapper/dists",
					"zipStoreBase=GRADLE_USER_HOME",
					"zipStorePath=wrapper/dists",
					"distributionUrl=https\\://services.gradle.org/distributions/gradle-$(GradleVersion)-all.zip"
		        });
	        }
	        
#if UNITY_2020_3_OR_NEWER
			var gradleVersion = "gradle-6.1.1-bin";
#else
			var gradleVersion = "gradle-5.6.4-bin";
#endif
	        var editorGradleVersion = GetEditorGradleVersion();
	        if (!string.IsNullOrEmpty(editorGradleVersion))
		        gradleVersion = $"gradle-{editorGradleVersion}-bin";

	        for (int i = 0; i < lineList.Count; i++)
	        {
		        if (lineList[i].StartsWith("distributionUrl="))
		        {
			        lineList[i] = $"distributionUrl=https\\://services.gradle.org/distributions/{gradleVersion}.zip";
			        break;
		        }
	        }
	        
			var propertiesFile = Path.Combine(wrapperPath, "gradle-wrapper.properties");
			File.WriteAllLines(propertiesFile, lineList);
        }
    }
}
