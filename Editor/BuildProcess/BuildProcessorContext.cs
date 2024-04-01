using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    [Serializable]
    public class PatchFiles
    {
        public string[] AddFiles;
        public string[] ModFiles;
    }

    public enum BuildTaskType
    {
        None,
        Prebuild,
        BuildProject,
    }

    public class BuildProcessorContext
    {
        private class LinkTypes
        {
            public string Assembly;
            public string[] Types;
        }

        private static BuildProcessorContext _current;
        
        public static BuildProcessorContext Current
        {
            get
            {
                if (_current == null)
                {
                    if (Application.isBatchMode)
                        throw new BuildException("Please load BuildProcessorContext first!!!");


#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
                    return Default();
#else
                    return null;
#endif
                }
                
                return _current;
            }
        }

        public static BuildProcessorContext Default()
        {
            if (Application.isBatchMode)
                throw new BuildException("Don't use this value in batch mode");
            return _current = new BuildProcessorContext().LoadFromDefault();
        }

        public static BuildProcessorContext BatchMode()
        {
            return _current = new BuildProcessorContext().LoadFromCommand();
        }

        public BuildTarget BuildTarget { get; internal set; }
        public BuildTargetGroup BuildTargetGroup { get; private set; }
        public BuildTaskType TaskType { get; internal set; }

        public string Channel { get; private set; }

        private bool _isWaiting;

        public event Action OnBuildStopWaiting;

        public bool IsWaiting
        {
            get => _isWaiting;
            set
            {
                _isWaiting = value;
                if (!value)
                    OnBuildStopWaiting?.Invoke();
            }
        }

        public bool DebugMode;
        /// <summary>
        /// 是否需要符号表 android
        /// </summary>
        public bool ProductMode;
        public string VersionName;
        public string VersionCode;
        public string Build;
        public PatchFiles PatchFiles;
        public string BuildPath;
        public string ResourceVersion;

        public string ProjectPath;

        public List<string> Symbols = new List<string>();
        public List<string> TemplatePaths = new List<string>();


        private Dictionary<string, LinkTypes> LinkAssemblies = new Dictionary<string, LinkTypes>();
        
        public Dictionary<string, string> CommandArgs { get; private set; }


        public XmlDocument LinkDocument
        {
            get
            {
                if (LinkAssemblies.Count <= 0)
                    return null;

                XmlDocument linkDoc = new XmlDocument();
                linkDoc.LoadXml("<linker></linker>");

                foreach (var info in LinkAssemblies.Values)
                {
                    XmlElement ele = linkDoc.createElementWithPath("/linker/assembly");
                    ele.SetAttribute("fullname", info.Assembly);
                    if (info.Types != null && info.Types.Length > 0)
                    {
                        foreach (var typeName in info.Types)
                        {
                            var node = ele.createSubElement("type");
                            node.SetAttribute("fullname", typeName);
                            node.SetAttribute("preserve", "all");
                        }
                    }
                    else
                    {
                        ele.SetAttribute("preserve", "all");
                    }
                }

                return linkDoc;
            }
        }

        public BuildChannels BuildChannels
        {
            get
            {
                if (_buildChannels == null)
                    throw new BuildException($"Cannot load BuildChannel with {Channel} at platform {BuildTarget}");

                if (!_buildChannels.IsMatchToPlatform(BuildTarget))
                    throw new BuildException($"The loaded BuildChannel with {Channel} is not match platform {BuildTarget}");

                return _buildChannels;
            }
        }

        private Dictionary<string, object> _data = new Dictionary<string, object>();

        private BuildChannels _buildChannels;

        private BuildProcessorContext LoadFromDefault()
        {
            Reset();

            var editorSetting = EditorSetting.Asset;
            Channel = editorSetting.CurrentChannel;
            
            Build = editorSetting.Build.ToString();
            VersionName = editorSetting.versionName;
            VersionCode = editorSetting.versionCode.ToString();

            Debug.Log($"LoadFromDefault VersionName:{VersionName} VersionCode:{VersionCode}");
            
            DebugMode = EditorUserBuildSettings.development;
            ProductMode = editorSetting.BuildProductMode;
            
            InitDataWithCommand(editorSetting.CommandsSimulation);
            
            return this;
        }
        

        private BuildProcessorContext LoadFromCommand()
        {
            Reset();

            DebugMode = false;

            var args = Environment.GetCommandLineArgs();
            var list = new List<string>(args);
            list.RemoveAt(0);
            InitDataWithCommand(list.ToArray());
            return this;
        }

        private void InitDataWithCommand(params string[] args)
        {
            CommandArgs = Global.GetCommandParams(args, "-");
            DebugMode = CommandArgs.ContainsKey("debug");
            ProductMode = CommandArgs.ContainsKey("product");
            BuildPath = CommandArgs.TryGet("path", BuildPath);
            Channel = CommandArgs.TryGet("channel", Channel);
            VersionName = CommandArgs.TryGet("versionname", VersionName);
            VersionCode = CommandArgs.TryGet("versioncode", VersionCode);
            PatchFiles = JsonUtility.FromJson<PatchFiles>(CommandArgs.TryGet("patchdata", "{}"));
            Build = CommandArgs.TryGet("build", Build);
            ResourceVersion = CommandArgs.TryGet("gitcommit", string.Empty);
            if (string.IsNullOrEmpty(ResourceVersion))
                ResourceVersion = $"{VersionName}-{VersionCode}";
            ProjectPath = Path.Combine(BuildPath, Channel);
            var patchFile = CommandArgs.TryGet("patchfile", null);
            if (!string.IsNullOrEmpty(patchFile))
                PatchFiles = JsonUtility.FromJson<PatchFiles>(File.ReadAllText(patchFile));
            Debug.Log("InitDataWithCommand::: build commands:: " + MiniJson.Serialize(CommandArgs));
            
            _buildChannels = BuildChannels.GetAsset(Channel);
            EditorSetting._currentBuildChannel = _buildChannels;
        }

        private void Reset()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            BuildTarget = target;
            VersionName = PlayerSettings.bundleVersion;

            switch (target)
            {
                case BuildTarget.Android:
                    BuildTargetGroup = BuildTargetGroup.Android;
                    VersionCode = PlayerSettings.Android.bundleVersionCode.ToString();
                    break;
                case BuildTarget.iOS:
                    BuildTargetGroup = BuildTargetGroup.iOS;
                    VersionCode = PlayerSettings.iOS.buildNumber;
                    break;
                case BuildTarget.WebGL:
                    BuildTargetGroup = BuildTargetGroup.WebGL;
                    VersionCode = "0";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, $"BuildTarget:{target} is not support");
            }

            BuildPath = Path.Combine(Application.dataPath, "..", "Build");
            DebugMode = false;
            ProductMode = false;
            PatchFiles = null;
            Channel = "default";
            Build = "0";

            _buildChannels = null;

            _data.Clear();

            Symbols.Clear();
            LinkAssemblies.Clear();
            TemplatePaths.Clear();
        }

        public T Get<T>(string key)
        {
            return (T) _data[key];
        }

        public T TryGet<T>(string key, T defaultValue)
        {
            object value;
            if (_data.TryGetValue(key, out value))
            {
                return (T) value;
            }

            return defaultValue;
        }

        public void Set<T>(string key, T value)
        {
            _data[key] = value;
        }

        public void AddLinkAssembly(string name, params string[] typeNames)
        {
            if (LinkAssemblies.TryGetValue(name, out var info))
            {
                if (info.Types == null || info.Types.Length <= 0)
                    return;

                if (typeNames == null || typeNames.Length <= 0)
                {
                    info.Types = null;
                    return;
                }

                var list = new List<string>(info.Types);
                list.AddRange(typeNames);
                info.Types = list.Distinct().ToArray();
            }
            else
            {
                LinkAssemblies.Add(name, new LinkTypes
                {
                    Assembly = name,
                    Types = typeNames
                });
            }
        }
    }
}
