using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    // 添加该特性的属性需返回 IEnumerable<string>
    [AttributeUsage(AttributeTargets.Property)]
    public class EditorChannelMenuAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class EditorParamsAttribute : Attribute
    {
        public string Key;
        public int Order;
        public string ToolTips;

        public EditorParamsAttribute(string key, int order = 0, string toolTips = null)
        {
            Key = key;
            Order = order;
            ToolTips = toolTips;
        }

        public EditorParamsAttribute(string key, string toolTips)
            : this(key, 0, toolTips)
        {
        }
    }


    public class EditorSetting : SerializedDataSet
    {
        private static readonly string MainPath = Path.Combine("Assets", "Editor");
        private static SettingAssetLoader assetLoader = new SettingAssetLoader(MainPath);

        private static List<SerializedType> editorSettingSerializedTypes;

        public override IEnumerable<SerializedType> SerializedTypes
        {
            get
            {
                if (editorSettingSerializedTypes != null)
                    return editorSettingSerializedTypes;

                var orders = new Dictionary<string, int>();
                editorSettingSerializedTypes = new List<SerializedType>();
                foreach (var type in Global.GetAllTypes<EditorParamsAttribute>())
                {
                    foreach (var attr in type.GetCustomAttributes(typeof(EditorParamsAttribute), false))
                    {
                        var paramAttr = (EditorParamsAttribute) attr;
                        orders.Add(paramAttr.Key, paramAttr.Order);
                        editorSettingSerializedTypes.Add(SerializedType.Create(paramAttr.Key, type,
                            paramAttr.ToolTips));
                    }
                }

                editorSettingSerializedTypes.Sort((a, b) =>
                {
                    var aorder = orders[a.Key];
                    var border = orders[b.Key];
                    if (aorder > border)
                        return 1;
                    if (aorder < border)
                        return -1;
                    return 0;
                });
                return editorSettingSerializedTypes;
            }
        }

        public static EditorSetting Asset => assetLoader.GetMain<EditorSetting>("EditorSetting");
        
        internal static BuildChannels _currentBuildChannel;

        public static BuildChannels CurrentBuildChannel
        {
            get
            {
                if (Application.isBatchMode)
                    return _currentBuildChannel;
                
                return BuildChannels.GetAsset(Asset.CurrentChannel);
            }
        }

        public string CurrentChannel = "default";

        [Tooltip("版本名称")]
        public string versionName;

        [Tooltip("版本号")]
        public int versionCode;

        [Tooltip("模拟Build号")]
        public int Build;
        
        [Tooltip("构建时使用生产模式")]
        public bool BuildProductMode = true;

        [Tooltip("模拟构建时的参数集合")]
        public string[] CommandsSimulation;
    }
}
