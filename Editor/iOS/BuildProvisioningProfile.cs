using System;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    [Serializable]
    public struct BuildProvisioningProfile
    {
        [Tooltip("签名文件ID")]
        [BrowserFile("选择文件", "mobileprovision")]
        [SerializeField]
        public string ProfileFile;
        
        [DisableEdit]
        [SerializeField]
        public string ProfileId;
        
        [DisableEdit]
        [SerializeField]
        public ProvisioningProfileType ProfileType;
        
        [DisableEdit]
        [SerializeField]
        public string ProfileSpecifier;
        
        [DisableEdit]
        [SerializeField]
        public string CodeSignIdentity;
    }
}