#if UNITY_IOS
#define UNITY_IOS_API
#endif
namespace PluginLit.Core.Editor
{
    public static class PBXCapabilityType
    {
#if UNITY_IOS_API
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType ApplePay = UnityEditor.iOS.Xcode.PBXCapabilityType.ApplePay;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType AppGroups = UnityEditor.iOS.Xcode.PBXCapabilityType.AppGroups;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType AssociatedDomains = UnityEditor.iOS.Xcode.PBXCapabilityType.AssociatedDomains;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType BackgroundModes = UnityEditor.iOS.Xcode.PBXCapabilityType.BackgroundModes;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType DataProtection = UnityEditor.iOS.Xcode.PBXCapabilityType.DataProtection;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType GameCenter = UnityEditor.iOS.Xcode.PBXCapabilityType.GameCenter;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType HealthKit = UnityEditor.iOS.Xcode.PBXCapabilityType.HealthKit;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType HomeKit = UnityEditor.iOS.Xcode.PBXCapabilityType.HomeKit;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType iCloud = UnityEditor.iOS.Xcode.PBXCapabilityType.iCloud;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType InAppPurchase = UnityEditor.iOS.Xcode.PBXCapabilityType.InAppPurchase;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType InterAppAudio = UnityEditor.iOS.Xcode.PBXCapabilityType.InterAppAudio;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType KeychainSharing = UnityEditor.iOS.Xcode.PBXCapabilityType.KeychainSharing;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType Maps = UnityEditor.iOS.Xcode.PBXCapabilityType.Maps;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType PersonalVPN = UnityEditor.iOS.Xcode.PBXCapabilityType.PersonalVPN;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType PushNotifications = UnityEditor.iOS.Xcode.PBXCapabilityType.PushNotifications;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType Siri = UnityEditor.iOS.Xcode.PBXCapabilityType.Siri;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType Wallet = UnityEditor.iOS.Xcode.PBXCapabilityType.Wallet;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType WirelessAccessoryConfiguration = UnityEditor.iOS.Xcode.PBXCapabilityType.WirelessAccessoryConfiguration;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType AccessWiFiInformation = UnityEditor.iOS.Xcode.PBXCapabilityType.AccessWiFiInformation;
        public static readonly UnityEditor.iOS.Xcode.PBXCapabilityType SignInWithApple = UnityEditor.iOS.Xcode.PBXCapabilityType.SignInWithApple;
#else
        public static readonly object ApplePay = null;
        public static readonly object AppGroups = null;
        public static readonly object AssociatedDomains = null;
        public static readonly object BackgroundModes = null;
        public static readonly object DataProtection = null;
        public static readonly object GameCenter = null;
        public static readonly object HealthKit = null;
        public static readonly object HomeKit = null;
        public static readonly object iCloud = null;
        public static readonly object InAppPurchase = null;
        public static readonly object InterAppAudio = null;
        public static readonly object KeychainSharing = null;
        public static readonly object Maps = null;
        public static readonly object PersonalVPN = null;
        public static readonly object PushNotifications = null;
        public static readonly object Siri = null;
        public static readonly object Wallet = null;
        public static readonly object WirelessAccessoryConfiguration = null;
        public static readonly object AccessWiFiInformation = null;
        public static readonly object SignInWithApple = null;
#endif
    }
}