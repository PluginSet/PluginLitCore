namespace PluginLit.Core.Editor
{
    public static class AndroidConst
    {
        public static readonly string NS_PREFIX = "android";
        public static readonly string NS_URI = "http://schemas.android.com/apk/res/android";

        public static readonly string NS_TOOLS = "http://schemas.android.com/tools";

        public static readonly string ROOT_NAME = "manifest";
        
        public static readonly string META_DATA_PARENT = $"/{ROOT_NAME}/application";
        public static readonly string META_DATA_PATH = $"{META_DATA_PARENT}/meta-data";
        public static readonly string ACTIVITY_PATH = $"{META_DATA_PARENT}/activity";
        public static readonly string PROVIDER_PATH = $"{META_DATA_PARENT}/provider";
        
        public static readonly string USES_PERMISSION_PARENT = $"/{ROOT_NAME}";
        public static readonly string USES_PERMISSION_PATH = $"{USES_PERMISSION_PARENT}/uses-permission";
    }
}