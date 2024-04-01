using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

namespace PluginLit.Core.Editor
{
    public class BuildModifyIOSProject: BuildProcessorTask
    {
        
        [Serializable]
        private struct BuildTypeInfo
        {
            [SerializeField]
            public string teamId;
            [SerializeField]
            public string bundleId;
            [SerializeField]
            public string method;
            [SerializeField]
            public string signStyle;
            [SerializeField]
            public string profile;
            [SerializeField]
            public string cert;
            [SerializeField]
            public bool biteCode;
        }
        
        [Serializable]
        private struct BuildTypes
        {
            [SerializeField]
            public BuildTypeInfo appstore;
            [SerializeField]
            public BuildTypeInfo adHoc;
            [SerializeField]
            public BuildTypeInfo development;
        }
        
        [Serializable]
        private struct BuildConfig
        {
            [SerializeField]
            public string info;
            [SerializeField]
            public string scheme;
            [SerializeField]
            public BuildTypes buildTypes;
        }
        
        private string IOSProjectPath;

        public BuildModifyIOSProject(string path)
        {
            IOSProjectPath = path;
        }

        public override void Execute(BuildProcessorContext context)
        {
            if (context.BuildTarget != BuildTarget.iOS)
                return;
            
            string xcodeProjectPath = Path.Combine(IOSProjectPath, "Unity-iPhone.xcodeproj", "project.pbxproj");

            var bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
            var bundleStrings = bundleId.Split('.');
            var project = new PBXProjectManager(xcodeProjectPath
                , $"Unity-iPhone/{bundleStrings[bundleStrings.Length - 1]}.entitlements"
                , "Unity-iPhone");

            //通用的必须加的包
            var targetGuid = project.UnityFramework;
            var mainTargetGuild = project.MainFramework;

//            pbxProject.AddFrameworkToProject(targetGuid, "AppTrackingTransparency.framework", false); // TODO
            // 填写个假的token  https://blog.csdn.net/yinfourever/article/details/107385530
            project.AddBuildProperty(mainTargetGuild, "USYM_UPLOAD_AUTH_TOKEN", "490e4f2bba5acc946a3eafb76c145605");
            project.AddBuildProperty(targetGuid, "USYM_UPLOAD_AUTH_TOKEN", "490e4f2bba5acc946a3eafb76c145605");
            
            // FIX:ERROR ITMS-90206: "Invalid Bundle. The bundle at 'xxx.app/Frameworks/UnityFramework.framework' contains disallowed file 'Frameworks'."
            project.SetBuildProperty(targetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

//            //推送权限处理  TODO
//            var unityIphone = pbxProject.ProjectGuid();
//            pbxProject.AddBuildProperty(unityIphone, "GCC_PREPROCESSOR_DEFINITIONS", "DISABLE_PUSH_NOTIFICATIONS=1");

            if (project.WorkSpaceSettings.HasPlistValue("BuildSystemType"))
                project.WorkSpaceSettings.RemoveElement("BuildSystemType");

            var iosParams = context.BuildChannels.Get<IosBuildParams>();
            var buildConfig = new BuildConfig
            {
                info = "Info.plist",
                scheme = "Unity-iPhone",
                buildTypes = new BuildTypes()
            };

            IosBuildTypeInfo? releaseBuild = null;
            IosBuildTypeInfo? debugBuild = null;
            BuildProvisioningProfile? releaseProfile = null;
            BuildProvisioningProfile? debugProfile = null;
            if (iosParams.BuildTypeInfos != null)
            {
                foreach (var info in iosParams.BuildTypeInfos)
                {
                    var buildTypeInfo = new BuildTypeInfo
                    {
                        teamId = info.teamId,
                        bundleId = info.bundleId,
                        method = GetBuildMethodDesc(info.method),
                        signStyle = info.automaticallySign ? "automatic" : "manual",
                        biteCode = info.biteCode,
                    };

                    BuildProvisioningProfile? profileValue = null;
                    if (!info.automaticallySign)
                    {
                        var profile = FindBuildProvisioningProfile(iosParams, info.profileName);
                        buildTypeInfo.profile = profile.ProfileId;
                        buildTypeInfo.cert = profile.CodeSignIdentity;
                        profileValue = profile;
                    }

                    switch (info.method)
                    {
                        case IosArchiveMethod.AppStore:
                            buildConfig.buildTypes.appstore = buildTypeInfo;
                            if (!releaseBuild.HasValue || info.method == iosParams.PresetMothod)
                            {
                                releaseBuild = info;
                                releaseProfile = profileValue;
                            }
                            break;
                        case IosArchiveMethod.AdHoc:
                            buildConfig.buildTypes.adHoc = buildTypeInfo;
                            if (!releaseBuild.HasValue || info.method == iosParams.PresetMothod)
                            {
                                releaseBuild = info;
                                releaseProfile = profileValue;
                            }
                            break;
                        case IosArchiveMethod.Development:
                            buildConfig.buildTypes.development = buildTypeInfo;
                            debugBuild = info;
                            debugProfile = profileValue;
                            break;
                    }
                }
            }

            if (releaseBuild.HasValue)
            {
                SetCodeSignWithConfig(project, "Release", releaseBuild.Value.teamId, releaseProfile);
                SetCodeSignWithConfig(project, "ReleaseForProfiling", releaseBuild.Value.teamId, releaseProfile);
                SetCodeSignWithConfig(project, "ReleaseForRunning", releaseBuild.Value.teamId, releaseProfile);
            }

            if (debugBuild.HasValue)
            {
                SetCodeSignWithConfig(project, "Debug", debugBuild.Value.teamId, debugProfile);
            }
            
            Global.CallCustomOrderMethods<iOSXCodeProjectModifyAttribute, BuildToolsAttribute>(context, project);
            project.Save();
            File.WriteAllText(Path.Combine(IOSProjectPath, "build_config.json"), JsonUtility.ToJson(buildConfig));
        }

        private void SetCodeSignWithConfig(PBXProjectManager project, string configName, string teamId, BuildProvisioningProfile? profile)
        {
            var target = project.MainFramework;
            if (profile.HasValue)
            {
                var codeSignIdentity = profile.Value.CodeSignIdentity;
                var dict = new Dictionary<string, string>()
                {
                    {"DEVELOPMENT_TEAM", teamId},
                    {"CODE_SIGN_STYLE", "Manual" },
                    {"CODE_SIGN_IDENTITY[sdk=iphoneos*]", codeSignIdentity},
                    {"PROVISIONING_PROFILE_APP", profile.Value.ProfileId},
                };
                if (codeSignIdentity.StartsWith("Apple Distribution:"))
                    dict.Add("CODE_SIGN_IDENTITY", "Apple Distribution");
                else if (codeSignIdentity.StartsWith("Apple Development:"))
                    dict.Add("CODE_SIGN_IDENTITY", "Apple Development");
                else if (codeSignIdentity.StartsWith("iPhone Distribution:"))
                    dict.Add("CODE_SIGN_IDENTITY", "iOS Distribution");
                else if (codeSignIdentity.StartsWith("iPhone Development:"))
                    dict.Add("CODE_SIGN_IDENTITY", "iOS Development");
                project.SetBuildPropertyForConfig(target, configName, dict);
            }
            else
            {
                project.SetBuildPropertyForConfig(target, configName, new Dictionary<string, string>()
                {
                    {"DEVELOPMENT_TEAM", teamId },
                    {"CODE_SIGN_STYLE", "Automatic"},
                    {"CODE_SIGN_IDENTITY", ""},
                    {"CODE_SIGN_IDENTITY[sdk=iphoneos*]", ""},
                });
            }
        }

        private string GetBuildMethodDesc(IosArchiveMethod method)
        {
            switch (method)
            {
                case IosArchiveMethod.AppStore:
                    return "app-store";
                case IosArchiveMethod.AdHoc:
                    return "ad-hoc";
                case IosArchiveMethod.Development:
                    return "development";
                default:
                    throw new BuildException($"Unknown build archive method : {method.ToString()}");
            }
        }

        private BuildProvisioningProfile FindBuildProvisioningProfile(IosBuildParams iosBuildParams, string name)
        {
            if (iosBuildParams.BuildProfiles == null)
                throw new BuildException($"No build profiles setting in iosBuildParams");

            foreach (var info in iosBuildParams.BuildProfiles)
            {
                if (info.ProfileSpecifier.Equals(name))
                    return info;
            }
            
            throw new BuildException($"Cannot find build profile named ${name} in iosBuildParams");
        }

    }
}