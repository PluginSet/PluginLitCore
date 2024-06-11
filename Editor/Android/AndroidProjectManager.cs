using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public class AndroidProjectManager
    {
        public XmlDocument LauncherManifest;

        public XmlDocument LibraryManifest;

        public StringBuilder Proguard;

        private GradleConfig _libraryGradle;

        public GradleConfig LibraryGradle
        {
            get
            {
                if (_libraryGradle == null)
                {
                    _libraryGradle = LoadGradleConfig(Path.Combine(LibraryPath, "build.gradle"));
                }

                return _libraryGradle;
            }
        }
        
        private GradleConfig _launcherGradle;

        public GradleConfig LauncherGradle
        {
            get
            {
                if (_launcherGradle == null)
                {
                    _launcherGradle = LoadGradleConfig(Path.Combine(LauncherPath, "build.gradle"));
                }

                return _launcherGradle;
            }
        }

        private GradleConfig _projectGradle;

        public GradleConfig ProjectGradle
        {
            get
            {
                if (_projectGradle == null)
                {
                    _projectGradle = LoadGradleConfig(Path.Combine(ProjectPath, "build.gradle"));
                }

                return _projectGradle;
            }
        }


        public string ProjectPath { get; private set; }

        public string LibraryPath { get; private set; }
        public string LauncherPath { get; private set; }

        private string _currentApplicationName = null;
        private bool _currentApplicationReplaceable = true;
        
        private string _currentMainActivityName = null;
        private bool _currentMainActivityReplaceable = true;
        
        public AndroidProjectManager(string androidProjectPath)
        {
            ProjectPath = androidProjectPath;
            LibraryPath = Path.Combine(androidProjectPath, "unityLibrary");
            if (!Directory.Exists(LibraryPath) && Directory.Exists(Path.Combine(androidProjectPath, "tuanjieLibrary")))
                LibraryPath = Path.Combine(androidProjectPath, "tuanjieLibrary");
            LauncherPath = Path.Combine(androidProjectPath, "launcher");
            
            LauncherManifest = new XmlDocument();
            LibraryManifest = new XmlDocument();
            
            LauncherManifest.Load(Path.Combine(LauncherPath, "src", "main", "AndroidManifest.xml"));
            LibraryManifest.Load(Path.Combine(LibraryPath, "src", "main", "AndroidManifest.xml"));
            
            Proguard = new StringBuilder();
            Global.AppendProguardInLib(Proguard, "com.pluginlit.core");

            var node = LibraryGradle.ROOT.GetOrCreateNode("android/defaultConfig");
            node.AppendContentNode("consumerProguardFiles \'proguard-user.txt\'");
            
            MoveMainActivityToLauncher();
            // SetApplication("com.pluginset.core.PluginSetBaseApplication", null, true);
            // SetMainActivity("com.pluginset.core.PluginSetBaseActivity", true);
            
//            if (!PlayerSettings.SplashScreen.showUnityLogo)
//                LibraryManifest.SetMetaData("unity.splash-enable", "False");
        }

        public void Save()
        {
            LauncherManifest.Save(Path.Combine(LauncherPath, "src", "main", "AndroidManifest.xml"));
            LibraryManifest.Save(Path.Combine(LibraryPath, "src", "main", "AndroidManifest.xml"));
            
            var proguardPath = Path.Combine(LibraryPath, "proguard-user.txt");
            File.WriteAllText(proguardPath, Proguard.ToString());
            
            _libraryGradle?.Save();
            _launcherGradle?.Save();
            _projectGradle?.Save();
        }

        private GradleConfig LoadGradleConfig(string path)
        {
            return new GradleConfig(path);
        }

        public void SetApplication(string applicationName, string theme = null, bool replaceable = false)
        {
            if (!_currentApplicationReplaceable)
                throw new BuildException($"Cannot set main activity again, current name is: {_currentApplicationName}, target is {applicationName}");

            _currentApplicationName = applicationName;
            _currentApplicationReplaceable = replaceable;
            
            var element = LauncherManifest.findOrCreateElemet(AndroidConst.META_DATA_PARENT);
            if (!string.IsNullOrEmpty(applicationName))
                element.SetAttribute("name", AndroidConst.NS_URI, applicationName);
            
            if (!string.IsNullOrEmpty(theme))
                element.SetAttribute("theme", AndroidConst.NS_URI, theme);
        }

        private void MoveMainActivityToLauncher()
        {
            var nodes = LibraryManifest.findElements($"{AndroidConst.ACTIVITY_PATH}/intent-filter/action",AndroidConst.NS_PREFIX, "name", "android.intent.action.MAIN");
            if (nodes.Count <= 0)
                return;
            
            if (nodes.Count > 1)
                throw new BuildException("There is more then one main activity!");

            var mainActivity = (XmlElement)(nodes[0].ParentNode?.ParentNode);
            if (mainActivity == null)
                throw new BuildException("No find main activity node in manifest!");

            var clone = LauncherManifest.findFirstElement(AndroidConst.META_DATA_PARENT);
            clone.InnerXml = mainActivity.OuterXml;
            mainActivity.ParentNode?.RemoveChild(mainActivity);
        }

        public void SetMainActivity(string activityName, bool replaceable = false)
        {
            if (!_currentMainActivityReplaceable)
                throw new BuildException($"Cannot set main activity again, current name is: {_currentMainActivityName}, target is {activityName}");
            
            _currentMainActivityName = activityName;
            _currentMainActivityReplaceable = replaceable;

            var nodes = LauncherManifest.findElements($"{AndroidConst.ACTIVITY_PATH}/intent-filter/action",AndroidConst.NS_PREFIX, "name", "android.intent.action.MAIN");
            if (nodes.Count <= 0)
                return;
            
            if (nodes.Count > 1)
                throw new BuildException("There is more then one main activity!");

            var mainActivity = (XmlElement)(nodes[0].ParentNode?.ParentNode);
            if (mainActivity == null)
                throw new BuildException("No find main activity node in manifest!");

            mainActivity.SetAttribute("name", AndroidConst.NS_URI, activityName);
            mainActivity.SetAttribute("exported", AndroidConst.NS_URI, "true");
        }

        public void AddApplicationPlugin(string key, string pluginFullName)
        {
            LauncherManifest.SetMetaData($"pluginset.application.{key}", pluginFullName);
            Proguard.Append($"-keep class {pluginFullName} {{ *; }}");
        }

        public void AddActivityPlugin(string key, string pluginFullName)
        {
            LauncherManifest.SetMetaData($"pluginset.activity.{key}", pluginFullName);
            Proguard.Append($"-keep class {pluginFullName} {{ *; }}");
        }
    }
}