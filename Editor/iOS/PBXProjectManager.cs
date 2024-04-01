#if UNITY_IOS
#define UNITY_IOS_API
#endif
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace PluginLit.Core.Editor
{
    public class PBXProjectManager
#if UNITY_IOS_API
        : UnityEditor.iOS.Xcode.ProjectCapabilityManager
#endif
    {
#if UNITY_IOS_API
        public UnityEditor.iOS.Xcode.PBXProject Project;
#else
        public object Project => new Exception("Error platform");
#endif
        public PlistDocument PlistDocument;
        public PlistDocument WorkSpaceSettings;
        public PlistDocument Entitlements;

        public string ProjectPath => m_PbxProjectPath;

        private string m_PbxProjectPath;

        private string m_EntitlementsPath;
        
#if UNITY_IOS_API
#if UNITY_2019_3_OR_NEWER
        // 2019.3以上的新接口
        public string MainFramework => Project.GetUnityMainTargetGuid();
        public string UnityFramework => Project.GetUnityFrameworkTargetGuid();
#else
        public string MainFramework => Project.TargetGuidByName("Unity-iPhone");
        public string UnityFramework => Project.TargetGuidByName("Unity-iPhone");
#endif
#else
        public string MainFramework => throw new Exception("Error platform");
        public string UnityFramework => throw new Exception("Error platform");
#endif
        
        public PBXProjectManager(string pbxProjectPath,
            string entitlementFilePath,
            string targetName)
#if UNITY_IOS_API
            : base(pbxProjectPath, entitlementFilePath, targetName)
#endif
        {
            m_PbxProjectPath = pbxProjectPath;
            m_EntitlementsPath = entitlementFilePath;
#if UNITY_IOS_API
            Project = this.project;
            var getOrCreateInfoDoc = typeof(UnityEditor.iOS.Xcode.ProjectCapabilityManager)
                .GetMethod("GetOrCreateInfoDoc", BindingFlags.Instance | BindingFlags.NonPublic);
            var getOrCreateEntitlementDoc = typeof(UnityEditor.iOS.Xcode.ProjectCapabilityManager)
                .GetMethod("GetOrCreateEntitlementDoc", BindingFlags.Instance | BindingFlags.NonPublic);
            PlistDocument = new PlistDocument(getOrCreateInfoDoc?.Invoke(this, null) as UnityEditor.iOS.Xcode.PlistDocument);
            Entitlements = new PlistDocument(getOrCreateEntitlementDoc?.Invoke(this, null) as UnityEditor.iOS.Xcode.PlistDocument);
            
            WorkSpaceSettings = new PlistDocument();
            var workSpaceSettingPath = Path.Combine(pbxProjectPath, "..", "project.xcworkspace", "xcshareddata", "WorkspaceSettings.xcsettings");
            if (File.Exists(workSpaceSettingPath))
                WorkSpaceSettings.ReadFromFile(workSpaceSettingPath);
            else
                WorkSpaceSettings.Create();
#endif
        }

        public void Save()
        {
#if UNITY_IOS_API
            var parentPath = Path.Combine(m_PbxProjectPath, "..", "project.xcworkspace", "xcshareddata");
            if (!Directory.Exists(parentPath))
                Directory.CreateDirectory(parentPath);
            
            var workSpaceSettingPath = Path.Combine(parentPath, "WorkspaceSettings.xcsettings");
            WorkSpaceSettings.WriteToFile(workSpaceSettingPath);
            
            WriteToFile();
#endif
        }

        public bool TryAddCapability(
            string targetGuid,
#if UNITY_IOS_API
            UnityEditor.iOS.Xcode.PBXCapabilityType capability,
#else
            object capability,
#endif
            bool addOptionalFramework = false)
        {
#if UNITY_IOS_API
            try
            {
                return project.AddCapability(targetGuid, capability, m_EntitlementsPath, addOptionalFramework);
            }
            catch (WarningException e)
            {
                Debug.LogWarning(e);
            }
#endif

            return false;
        }

        public void AddAssociatedDomainsApplinks(params string[] links)
        {
#if UNITY_IOS_API
            TryAddCapability(MainFramework, UnityEditor.iOS.Xcode.PBXCapabilityType.AssociatedDomains);
            const string domainKey = "com.apple.developer.associated-domains";
            Entitlements.AddArrayElement(domainKey, links.Select(link => $"applinks:{link}").ToArray());
#endif
        }
        
        public void EnableSwiftCompile(string xcodeTarget)
        {
#if UNITY_IOS_API
            // 首先需要一个空的Swift文件
            var swiftPath = Path.Combine(ProjectPath, "Classes", "EmptySwift.swift");
            if (!File.Exists(swiftPath))
            {
                File.WriteAllText(swiftPath, "// emtpy swift script for compile OC with swift");
            }

            var subPath = Global.GetSubPath(ProjectPath, swiftPath);
            var fileGuid = project.AddFile(subPath, subPath);
            project.AddFileToBuild(xcodeTarget, fileGuid);

            // 然后要打开 Build Settings -> Build Options -> Always Embed Swift Standard Libraries 修改为YES
            project.SetBuildProperty(xcodeTarget, "EMBEDDED_CONTENT_CONTAINS_SWIFT", "YES");
            project.SetBuildProperty(xcodeTarget, "SWIFT_VERSION", "4.0");
#endif
        }
        
        public void AddBuildProperty(string targetGuid, string name, string value)
        {
#if UNITY_IOS_API
            project.AddBuildProperty(targetGuid, name, value);
#endif
        }
        
        public void SetBuildProperty(string targetGuid, string name, string value)
        {
#if UNITY_IOS_API
            project.SetBuildProperty(targetGuid, name, value);
#endif
        }

        public void SetBuildPropertyForConfig(string targetGuid, string configName, Dictionary<string, string> dictionary)
        {
#if UNITY_IOS_API
            var config = project.BuildConfigByName(targetGuid, configName);
            if (string.IsNullOrEmpty(config))
                throw new BuildException($"Cannot find build config with name {configName}");

            foreach (var kv in dictionary)
            {
                project.SetBuildPropertyForConfig(config, kv.Key, kv.Value);
            }
#endif
        }

        public void AddFrameworkToProject(string targetGuid, string framework, bool weak)
        {
#if UNITY_IOS_API
            project.AddFrameworkToProject(targetGuid, framework, weak);
#endif
        }


#if UNITY_IOS_API
        public new void AddSignInWithApple()
#else
        public void AddSignInWithApple()
#endif
        {
#if UNITY_IOS_API
            base.AddSignInWithApple();
#endif
        }
        
#if UNITY_IOS_API
        public new void AddInAppPurchase()
#else
        public void AddInAppPurchase()
#endif
        {
#if UNITY_IOS_API
            base.AddInAppPurchase();
#endif
        }
    }
}