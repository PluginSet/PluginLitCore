using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PluginLit.Core.Editor
{
    public enum IosArchiveMethod
    {
        AppStore,
        AdHoc,
        Development,
    }
    
    [Serializable]
    public struct IosBuildTypeInfo
    {
        [SerializeField]
        public string teamId;
        [SerializeField]
        public string bundleId;
        [SerializeField]
        public IosArchiveMethod method;
        [SerializeField]
        public bool automaticallySign;
        [DropDownList("ProfileNames")]
        [SerializeField]
        public string profileName;
        [SerializeField]
        public bool biteCode;
    }
    
    [BuildTools]
    [BuildChannelsParams("iOS", -10000, "苹果打包相关设置")]
    [VisibleCaseBoolValue("SupportIOS", true)]
    public class IosBuildParams: ScriptableObject
    {
        [Tooltip("存储KeyChains时默认使用的services字符串")]
        public string KeyChainServices = "com.pluginlit.core";

        [Tooltip("所有用到的签名文件")]
        public BuildProvisioningProfile[] BuildProfiles;
        
        [Tooltip("可提供的所有构建方式及参数")]
        public IosBuildTypeInfo[] BuildTypeInfos;
        
        [Tooltip("预设的构建方式")]
        public IosArchiveMethod PresetMothod;

        [Tooltip("开启数据收集")]
        public bool EnableAppTrackingTransparency = true;
        
        public string LocationUsageDescription = "Your consent is required to access the microphone";

        public string LocationAlwaysUsageDescription = "Your location is required for xyz benefits for you";

        public string LocationWhenInUseUsageDescription = "Your location is required for xyz benefits for you";
        
        public string PhotoLibraryUsageDescription = "This app requires access to the photo library.";
            
        public string CameraUsageDescription = "Your consent is required to access the camera";
        
        public string MicrophoneUsageDescription = "Your consent is required to access the microphone";
        
        public string CalendarsUsageDescription = "Your consent is required to access the calendar";
        
        public string MotionUsageDescription = "Your consent is required to access sports and fitness";
        
        public string BluetoothPeripheralUsageDescription = "Your consent is required to access Bluetooth";
        
        public string AppleMusicUsageDescription = "Your consent is required to access the media database";
        
        public string RemindersUsageDescription = "Your consent is required to access reminders";

        public string UserTrackingUsageDescription = "Your data will be used to deliver personalized ads to you";

        public IEnumerable<string> ProfileNames
        {
            get
            {
                return BuildProfiles?.Select(e => e.ProfileSpecifier);
            }
        }

        private void OnValidate()
        {
            if (BuildProfiles != null)
            {
                for (int i = 0; i < BuildProfiles.Length; i++)
                {
                    OnUpdateProfile(ref BuildProfiles[i]);
                }
            }
        }
        
        private static readonly string s_PatternTeamID = "<key>TeamIdentifier<\\/key>[\n\t]*<array>[\n\t]*<string>([^<>]*)</string>";
        private static readonly string s_PatternUUID = "<key>UUID<\\/key>[\n\t]*<string>((\\w*\\-?){5})";
        private static readonly string s_PatternSpecifier = "<key>Name<\\/key>[\n\t]*<string>([^<>]*)</string>";
        private static readonly string s_PatternTeamName = "<key>TeamName<\\/key>[\n\t]*<string>([^<>]*)</string>";
        private static readonly string s_PatternDeveloperCertificates = "<key>DeveloperCertificates<\\/key>[\n\t]*<array>[\n\t]*<data>([\\w\\/+=]+)<\\/data>";
        
        private static readonly string DefaultProvisioningProfileSearchPath =
            "{Home}/Library/MobileDevice/Provisioning Profiles";

        /**
         * UID=U8GTR34PG2, CN=\"Apple Distribution: Hangzhou Yuesheng Technology Co., Ltd. (U8GTR34PG2)\", OU=U8GTR34PG2, O=\"Hangzhou Yuesheng Technology Co., Ltd.\", C=US 
         */
        private static Dictionary<string, string> ParseCertSubject(string subject)
        {
            var result = new Dictionary<string, string>();

            string key = "";
            string value = null;
            int state = 0;
            bool inQuot = false;
            StringBuilder builder = new StringBuilder();

            int i = 0;
            while (i < subject.Length)
            {
                var @char = subject[i++];
                if (@char == '\\')
                {
                    builder.Append(subject[i++]);
                    continue;
                }
                if ((@char == '\"' && (i >= subject.Length || subject[i] == ',')) ||
                    @char == ',' && !inQuot)
                {
                    if (state != 2)
                        throw new Exception($"Unknown subject string at pos {i - 1}: {subject}");

                    value = builder.ToString();
                    builder.Clear();
                    state = 0;

                    result.Add(key, value);
                    i++;
                    inQuot = false;
                    continue;
                }

                if (@char == '"')
                {
                    inQuot = true;
                }
                else if (@char == '=')
                {
                    if (state != 1)
                        throw new Exception($"Unknown subject string at pos {i - 1}: {subject}");

                    key = builder.ToString();
                    builder.Clear();
                    state = 2;
                }
                else if (@char == '\"')
                {
                    if (state != 2)
                        throw new Exception($"Unknown subject string at pos {i - 1}: {subject}");

                    if (i >= subject.Length || subject[i] == ',')
                    {
                        value = builder.ToString();
                        builder.Clear();
                        state = 0;

                        result.Add(key, value);
                        i++;
                    }
                }
                else
                {
                    builder.Append(@char);
                    if (state == 0)
                        state = 1;
                }
            }

            return result;
        }

        private static void ParseFile(string filePath, out string profileId, out string profileSpecifier, out string codeSignIdentity, out ProvisioningProfileType type)
        {
            string input = File.ReadAllText(filePath);
            Match match1 = Regex.Match(input, s_PatternUUID, RegexOptions.Singleline);
            if (match1.Success)
                profileId = match1.Groups[1].Value;
            else
                profileId = string.Empty;
            
            string teamId;
            Match match4 = Regex.Match(input, s_PatternTeamID, RegexOptions.Singleline);
            if (match4.Success)
                teamId = match4.Groups[1].Value;
            else
                teamId = string.Empty;

            string teamName;
            Match match5 = Regex.Match(input, s_PatternTeamName, RegexOptions.Singleline);
            if (match5.Success)
                teamName = match5.Groups[1].Value;
            else
                teamName = string.Empty;

            if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(teamName))
                codeSignIdentity = string.Empty;
            else
                codeSignIdentity = $"Apple Distribution: {teamName} ({teamId})";
            
            Match match2 = Regex.Match(input, s_PatternDeveloperCertificates, RegexOptions.Singleline);
            if (match2.Success)
            {
                type = ProvisioningProfileType.Automatic;
                var data = Convert.FromBase64String(match2.Groups[1].Value);
                var cert = new X509Certificate(data);
                var result = ParseCertSubject(cert.Subject);
                if (result.TryGetValue("CN", out var cn))
                {
                    codeSignIdentity = cn;
                    if (codeSignIdentity.StartsWith("iPhone Distribution:") || codeSignIdentity.StartsWith("Apple Distribution:"))
                        type = ProvisioningProfileType.Distribution;
                    else if (codeSignIdentity.StartsWith("iPhone Development:") || codeSignIdentity.StartsWith("Apple Development:"))
                        type = ProvisioningProfileType.Development;
                }
            }
            else
                type = ProvisioningProfileType.Automatic;
            
            Match match3 = Regex.Match(input, s_PatternSpecifier, RegexOptions.Singleline);
            if (match3.Success)
                profileSpecifier = match3.Groups[1].Value;
            else
                profileSpecifier = string.Empty;
        }
        
        private static void OnUpdateProfile(ref BuildProvisioningProfile profile)
        {
            if (string.IsNullOrEmpty(profile.ProfileFile))
                return;

            ParseFile(Path.Combine(".", profile.ProfileFile), out profile.ProfileId, out profile.ProfileSpecifier, out profile.CodeSignIdentity, out profile.ProfileType);
        }

        private static void CopyProvisioningProfile(in BuildProvisioningProfile profile)
        {
            if (string.IsNullOrEmpty(profile.ProfileFile))
                return;
            
            string path = DefaultProvisioningProfileSearchPath.Replace("{Home}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            Global.CopyFile(Path.Combine(".", profile.ProfileFile), Path.Combine(path, $"{profile.ProfileId}.mobileprovision"), null);
        }
        

        [OnSyncEditorSetting]
        public static void OnSyncExportSetting_IOS(BuildProcessorContext context)
        {
            if (context.BuildTarget != BuildTarget.iOS)
                return;

            var asset = context.BuildChannels;
            var setting = asset.Get<IosBuildParams>();
            
            if (setting.BuildProfiles != null)
            {
                for (int i = 0; i < setting.BuildProfiles.Length; i++)
                {
                    CopyProvisioningProfile(in setting.BuildProfiles[i]);
                }
            }

            IosBuildTypeInfo? buildInfo = null;
            if (setting.BuildTypeInfos != null)
            {
                foreach (var info in setting.BuildTypeInfos)
                {
                    if (info.method == setting.PresetMothod)
                    {
                        buildInfo = info;
                        break;
                    }
                }
            }

            if (buildInfo.HasValue)
            {
                var info = buildInfo.Value;
                if (!string.IsNullOrEmpty(info.teamId))
                    PlayerSettings.iOS.appleDeveloperTeamID = info.teamId;
                
                PlayerSettings.iOS.appleEnableAutomaticSigning = info.automaticallySign;
                if (!info.automaticallySign)
                {
                    if (!string.IsNullOrEmpty(info.profileName))
                    {
                        BuildProvisioningProfile? profileValue = null;
                        if (setting.BuildProfiles != null)
                        {
                            foreach (var profile in setting.BuildProfiles)
                            {
                                if (profile.ProfileSpecifier.Equals(info.profileName))
                                {
                                    profileValue = profile;
                                    break;
                                }
                            }
                        }

                        if (profileValue.HasValue)
                        {
                            var profile = profileValue.Value;
                            PlayerSettings.iOS.iOSManualProvisioningProfileID = profile.ProfileId;
                            PlayerSettings.iOS.iOSManualProvisioningProfileType = profile.ProfileType;
                        }
                        else
                        {
                            throw new BuildException($"Cannot find profile with name {info.profileName}");
                        }
                        
                    }
                }
            }
        }

        [iOSXCodeProjectModify(int.MaxValue)]
        public static void OnIosXCodeProjectModify(BuildProcessorContext context, PBXProjectManager project)
        {
            var plist = project.PlistDocument;
            var buildParams = context.BuildChannels.Get<IosBuildParams>();
            //ios一系类权限请求文本
            plist.AddPlistValue("NSLocationUsageDescription", buildParams.LocationUsageDescription);
            plist.AddPlistValue("NSLocationAlwaysUsageDescription", buildParams.LocationAlwaysUsageDescription);
            plist.AddPlistValue("NSLocationWhenInUseUsageDescription", buildParams.LocationWhenInUseUsageDescription);
            plist.AddPlistValue("NSPhotoLibraryUsageDescription", buildParams.PhotoLibraryUsageDescription);
            plist.AddPlistValue("NSCameraUsageDescription", buildParams.CameraUsageDescription);
            plist.AddPlistValue("NSMicrophoneUsageDescription", buildParams.MicrophoneUsageDescription);
            plist.AddPlistValue("NSCalendarsUsageDescription", buildParams.CalendarsUsageDescription);
            plist.AddPlistValue("NSMotionUsageDescription", buildParams.MotionUsageDescription);
            //这两个权限是关于健康啥的 一般app不用申请 不然会拒审
            //plist.SetPlistValue("NSHealthUpdateUsageDescription", "Your consent is required to access health updates");
            //plist.SetPlistValue("NSHealthShareUsageDescription", "Your consent is required to access health sharing");
            plist.AddPlistValue("NSBluetoothPeripheralUsageDescription", buildParams.BluetoothPeripheralUsageDescription);
            plist.AddPlistValue("NSAppleMusicUsageDescription", buildParams.AppleMusicUsageDescription);
            plist.AddPlistValue("NSRemindersUsageDescription", buildParams.RemindersUsageDescription);

            plist.AddPlistValue("KeyChainServices", buildParams.KeyChainServices);
            
            // DevicesUtil.mm 需要依赖 AppTrackingTransparency, 但Unity中并没有相应选项
            if (buildParams.EnableAppTrackingTransparency)
            {
                plist.AddPlistValue("NSUserTrackingUsageDescription",buildParams.UserTrackingUsageDescription);
                project.AddFrameworkToProject(project.UnityFramework, "AppTrackingTransparency.framework", true);
            }
            
            var xcodeTarget = project.UnityFramework;
            project.AddBuildProperty(xcodeTarget, "CLANG_ENABLE_MODULES", "YES");
            project.AddBuildProperty(xcodeTarget, "LD_RUNPATH_SEARCH_PATHS", "$(inherited)");
        }


    }
}