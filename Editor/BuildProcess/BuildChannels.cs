#if UNITY_IOS
#define UNITY_IOS_API
#endif
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BuildChannelsParamsAttribute : Attribute
    {
        public string Alias;
        public int Order;
        public string ToolTips;

        public BuildChannelsParamsAttribute(string alias, int order = 0, string toolTips = null)
        {
            Alias = alias;
            Order = order;
            ToolTips = toolTips;
        }

        public BuildChannelsParamsAttribute(string key, string toolTips)
            : this(key, 0, toolTips)
        {
        }
    }

    [BuildTools]
    public class BuildChannels : SerializedDataSet
    {
        [Serializable]
        public class BuildSceneInfo
        {
            public SceneAsset Scene;
            public bool enable;

            public override string ToString()
            {
                return $"{Scene.name}({(enable ? "enabled" : "disabled")})";
            }
        }
        
        [Tooltip("支持安卓平台")]
        public bool SupportAndroid = true;
        
        [Tooltip("支持iOS平台")]
        public bool SupportIOS = true;
        
        [Tooltip("支持WebGL平台")]
        public bool SupportWebGL = true;

        [Tooltip("参与打包的场景")]
        public BuildSceneInfo[] Scenes;

        [Tooltip("项目名称")]
        public string ProductName;

        [Tooltip("公司名称")]
        public string CompanyName;

        [Tooltip("包名")]
        public string PackageName;

        [Tooltip("图标贴图")]
        public Texture2D IconTexture;

        [Tooltip("加入link文件防止代码裁剪的模块")]
        public string[] LinkModules;

        [Tooltip("额外的宏设置")]
        public string[] ExtendSymbols;

        [Tooltip("是否显示Unity闪屏")]
        public bool ShowUnityLogo = false;

        [Tooltip("额外的StreamingAssets资源，这些目录或文件会在构建项目前拷贝到StreamingAssets目录")]
        public string[] ExtendStreamingAssets;

        internal static readonly string MainPath = Path.Combine("Assets", "Editor", "Channels");
        private static SettingAssetLoader assetLoader = new SettingAssetLoader(MainPath);

        private static List<SerializedType> buildChannelSerializedTypes;

        public bool IsMatchToPlatform(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return SupportAndroid;
                case BuildTarget.iOS:
                    return SupportIOS;
                case BuildTarget.WebGL:
                    return SupportWebGL;
                default:
                    Debug.LogWarning("No property to match build target:: " + buildTarget);
                    return false;
            }
        }

        public override IEnumerable<SerializedType> SerializedTypes
        {
            get
            {
                if (buildChannelSerializedTypes != null)
                    return buildChannelSerializedTypes;

                var orders = new Dictionary<string, int>();
                buildChannelSerializedTypes = new List<SerializedType>();
                foreach (var type in Global.GetAllTypes<BuildChannelsParamsAttribute>())
                {
                    foreach (var attr in type.GetCustomAttributes(typeof(BuildChannelsParamsAttribute), false))
                    {
                        var paramAttr = (BuildChannelsParamsAttribute) attr;
                        orders.Add(paramAttr.Alias, paramAttr.Order);
                        buildChannelSerializedTypes.Add(SerializedType.Create(paramAttr.Alias, type, paramAttr.ToolTips));
                    }
                }

                buildChannelSerializedTypes.Sort((a, b) =>
                {
                    var aorder = orders[a.Key];
                    var border = orders[b.Key];
                    if (aorder == border)
                        return string.CompareOrdinal(a.Key, b.Key);
                    return aorder - border;
                });
                return buildChannelSerializedTypes;
            }
        }

        public static BuildChannels GetAsset(string channel)
        {
            return assetLoader.GetMain<BuildChannels>(GetFileNameInternal(channel));
        }

        private static string[] GetAllDefaultChannelNames()
        {
            return new[]
            {
                GetFileNameInternal("default")
            };
        }

        internal static string GetFileNameInternal(string channel)
        {
            return $"Channel_{channel}";
        }

        public static void InitDefaultChannels()
        {
            foreach (var fileName in GetAllDefaultChannelNames())
            {
                assetLoader.GetMain<BuildChannels>(fileName);
            }
        }

        private static void SetPlatformIcons(BuildTargetGroup platform, PlatformIconKind kind, Texture2D texture)
        {
            var icons = PlayerSettings.GetPlatformIcons(platform, kind);
            foreach (var icon in icons)
            {
                icon.SetTexture(texture);
            }
            PlayerSettings.SetPlatformIcons(platform, kind, icons);
        }

        [OnSyncEditorSetting(int.MinValue)]
        public static void OnSyncEditorSetting_base(BuildProcessorContext context)
        {
            BuildChannels asset = context.BuildChannels;
            if (!asset.IsMatchToPlatform(context.BuildTarget))
                throw new BuildException("BuildTarget is not match");

            var packageName = asset.PackageName;
            if (string.IsNullOrEmpty(packageName))
            {
                if (context.BuildTarget != BuildTarget.WebGL)
                    throw new BuildException($"PackageName not configured in {asset.name}");
            }
            else
            {
                if (PlayerSettings.GetApplicationIdentifier(context.BuildTargetGroup) != packageName)
                    PlayerSettings.SetApplicationIdentifier(context.BuildTargetGroup, packageName);
            }

            var productName = asset.ProductName;
            if (!string.IsNullOrEmpty(productName))
                PlayerSettings.productName = productName;

            var companyName = asset.CompanyName;
            if (!string.IsNullOrEmpty(companyName))
                PlayerSettings.companyName = companyName;

            var sceneCount = asset.Scenes.Length;
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[sceneCount];
            for (int i = 0; i < sceneCount; i++)
            {
                var info = asset.Scenes[i];
                var path = AssetDatabase.GetAssetPath(info.Scene);
                scenes[i] = new EditorBuildSettingsScene(path, info.enable);
            }

            if (asset.IconTexture != null)
            {
                var icon = asset.IconTexture;
                if (context.BuildTarget == BuildTarget.Android)
                {
#if UNITY_ANDROID_API
                    SetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Legacy, icon);
                    SetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Round, icon);
// #if UNITY_2019_3_OR_NEWER
//                     SetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, icon);
// #endif
#endif
                }
                else if (context.BuildTarget == BuildTarget.iOS)
                {
#if UNITY_IOS_API
                    SetPlatformIcons(BuildTargetGroup.iOS, UnityEditor.iOS.iOSPlatformIconKind.Application, icon);
                    SetPlatformIcons(BuildTargetGroup.iOS, UnityEditor.iOS.iOSPlatformIconKind.Marketing, icon);
                    SetPlatformIcons(BuildTargetGroup.iOS, UnityEditor.iOS.iOSPlatformIconKind.Settings, icon);
                    SetPlatformIcons(BuildTargetGroup.iOS, UnityEditor.iOS.iOSPlatformIconKind.Notification, icon);
                    SetPlatformIcons(BuildTargetGroup.iOS, UnityEditor.iOS.iOSPlatformIconKind.Spotlight, icon);
#endif
                }
            }
            
            EditorBuildSettings.scenes = scenes;

            if (asset.LinkModules != null)
            {
                foreach (var assembly in asset.LinkModules)
                {
                    context.AddLinkAssembly(assembly);
                }
            }

            if (asset.ExtendSymbols != null)
            {
                foreach (var symbol in asset.ExtendSymbols)
                {
                    context.Symbols.Add(symbol);
                }
            }
        }
    }
}
