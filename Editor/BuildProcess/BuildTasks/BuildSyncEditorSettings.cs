
namespace PluginLit.Core.Editor
{
    public class BuildSyncEditorSettings : BuildProcessorTask
    {
        public override void Execute(BuildProcessorContext context)
        {
            var config = PluginSetConfig.NewAsset;
            context.SetPluginsConfig(config);
            
            // sync context
            Global.CallCustomOrderMethods<OnSyncEditorSettingAttribute, BuildToolsAttribute>(context);
        }
    }
}
