namespace PluginLit.Core.Editor
{
    public abstract class BuildProcessorTask
    {
        public BuildTaskHandler Handler { get; internal set; }
        
        public abstract void Execute(BuildProcessorContext context);
    }
}