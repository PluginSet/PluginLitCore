using System;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PluginLit.Core.Editor
{
    [BuildTools]
    public static class BuildHelper
    {
        public static void PreBuildWithContext(BuildProcessorContext context)
        {
            context.TaskType = BuildTaskType.Prebuild;
            try
            {
                var handler = new BuildTaskHandler();
                handler.AddNextTask(new BuildSyncEditorSettings());
                handler.AddNextTask(new BuildEnd());
                handler.Execute(context);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        public static void BuildWithContext(BuildProcessorContext context)
        {
            context.TaskType = BuildTaskType.BuildProject;
            try
            {
                var handler = new BuildTaskHandler();
                handler.AddNextTask(new BuildSyncEditorSettings());
                handler.AddNextTask(new BuildPrepareAssets());
                handler.AddNextTask(new BuildExportedProject());
                handler.AddNextTask(new BuildEnd());
                handler.Execute(context);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        public static void SyncPluginsConfig()
        {
            var context = BuildProcessorContext.Default();
            PreBuildWithContext(context);
        }

        public static void BuildAppDefault(string buildPath)
        {
#if UNITY_ANDROID
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
#endif
            var context = BuildProcessorContext.Default();
#if UNITY_ANDROID
            context.BuildTarget = BuildTarget.Android;
#elif UNITY_IOS
            context.BuildTarget = BuildTarget.iOS;
#elif UNITY_WEBGL
            context.BuildTarget = BuildTarget.WebGL;
#endif
            context.BuildPath = buildPath;
            BuildWithContext(context);
        }

        public static void PreBuild()
        {
            if (!Application.isBatchMode)
                throw new BuildException("Application is not in batch mode");

            PreBuildWithContext(BuildProcessorContext.BatchMode());
        }

        public static void Build()
        {
            if (!Application.isBatchMode)
                throw new BuildException("Application is not in batch mode");

            BuildWithContext(BuildProcessorContext.BatchMode());
        }
    }
}
