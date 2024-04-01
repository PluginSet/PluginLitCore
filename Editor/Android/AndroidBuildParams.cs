using System.Xml;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    [BuildTools]
    [BuildChannelsParams("Android", -10000, "安卓打包相关设置")]
    [VisibleCaseBoolValue("SupportAndroid", true)]
    public class AndroidBuildParams: ScriptableObject
    {
        [Tooltip("是否使用自定义签名文件")]
        public bool UseCustomKeystore = true;
        
        [FolderDrag(".keystore")]
        [BrowserFile("Choose a KeyStore", "keystore")]
        [Tooltip("使用的KeyStore文件全路径")]
        [VisibleCaseBoolValue("UseCustomKeystore", true)]
        public string KeyStorePath;
        
//        [Password]
        [Tooltip("已设定的KeyStore密码")]
        [VisibleCaseBoolValue("UseCustomKeystore", true)]
        public string KeyStorePassword;

        [Tooltip("使用的KeyStore别名")]
        [VisibleCaseBoolValue("UseCustomKeystore", true)]
        public string KeyAliasName;
        
//        [Password]
        [Tooltip("已设定的KeyStore别名密码")]
        [VisibleCaseBoolValue("UseCustomKeystore", true)]
        public string KeyAliasPassword;
        
        [Tooltip("是否使用分包")]
        public bool SplitApplicationBinary;

//        [Tooltip("Manifest中的application的name属性")]
//        public string ApplicationName;
//
//        [Tooltip("Manifest中的application的theme属性")]
//        public string ApplicationTheme;

        [Tooltip("构建目标SDK版本号")]
        public AndroidSdkVersions TargetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        [OnSyncEditorSetting]
        public static void OnSyncExportSetting_android(BuildProcessorContext context)
        {
            if (context.BuildTarget != BuildTarget.Android)
                return;

            var asset = context.BuildChannels;
            var setting = asset.Get<AndroidBuildParams>();

            if (!setting.UseCustomKeystore || string.IsNullOrEmpty(setting.KeyStorePath))
            {
#if UNITY_2019_1_OR_NEWER
                PlayerSettings.Android.useCustomKeystore = false;
                return;
#endif
            }

#if UNITY_2019_1_OR_NEWER
            PlayerSettings.Android.useCustomKeystore = true;
#endif
            PlayerSettings.Android.keystoreName = setting.KeyStorePath;
            PlayerSettings.Android.keystorePass = setting.KeyStorePassword;
            PlayerSettings.Android.keyaliasName = setting.KeyAliasName;
            PlayerSettings.Android.keyaliasPass = setting.KeyAliasPassword;

            PlayerSettings.Android.targetSdkVersion = setting.TargetSdkVersion;
            
            PlayerSettings.Android.useAPKExpansionFiles = setting.SplitApplicationBinary;
        }
    }
}