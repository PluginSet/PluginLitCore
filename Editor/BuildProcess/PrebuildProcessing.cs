using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
 
namespace PluginLit.Core.Editor
{
    public class PreBuildProcessing : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;
        public void OnPreprocessBuild(BuildReport report)
        {
            var python = "/Library/Frameworks/Python.framework/Versions/2.7/bin/python";
            if (File.Exists(python))
                System.Environment.SetEnvironmentVariable("EMSDK_PYTHON", python);
        }
    }
}