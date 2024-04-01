using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;

namespace PluginLit.Core.Editor
{
    public class BuildModifyAndroidProject : BuildProcessorTask
    {
        private string AndroidProjectPath;

        public BuildModifyAndroidProject(string path)
        {
            AndroidProjectPath = path;
        }

        public override void Execute(BuildProcessorContext context)
        {
            if (context.BuildTarget != BuildTarget.Android)
                return;
            
            var projectManager = new AndroidProjectManager(AndroidProjectPath);
            Global.CallCustomOrderMethods<AndroidProjectModifyAttribute, BuildToolsAttribute>(context, projectManager);
            RemoveDebuggable(projectManager.LibraryManifest);
            RemoveDebuggable(projectManager.LauncherManifest);
            projectManager.Save();
        }

        private static void RemoveDebuggable(XmlDocument doc)
        {
            // debuggable
            var element = doc.findFirstElement(AndroidConst.META_DATA_PARENT);
            if (element.HasAttribute("debuggable", AndroidConst.NS_URI))
            {
                element.SetAttribute("debuggable", AndroidConst.NS_URI, "false");
            }
        }
    }
}
