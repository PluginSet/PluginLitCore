using System.Collections.Generic;

namespace PluginLit.Core.Editor
{
    public static class BuildGlobal
    {
        public static void SetPluginsConfig(this BuildProcessorContext context, PluginsConfig config)
        {
            context.Set("pluginsConfig", config);
        }

        public static PluginsConfig GetPluginsConfig(this BuildProcessorContext context)
        {
            return context.Get<PluginsConfig>("pluginsConfig");
        }
        
        public static void SetBuildResult(this BuildProcessorContext context, string key, object value)
        {
            var dict = context.TryGet<Dictionary<string, object>>("buildResultJson", null);
            if (dict == null)
            {
                dict = new Dictionary<string, object>();
                context.Set("buildResultJson", dict);
            }

            dict[key] = value;
        }

        public static void RemoveBuildResult(this BuildProcessorContext context, string key)
        {
            var dict = context.TryGet<Dictionary<string, object>>("buildResultJson", null);
            if (dict == null)
                return;

            if (dict.ContainsKey(key))
                dict.Remove(key);
        }

    }
}