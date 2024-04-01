using System;

namespace PluginLit.Core.Editor
{
    public class BuildSimpleTask: BuildProcessorTask
    {
        private readonly Action<BuildProcessorContext> _action;
        
        public BuildSimpleTask(Action<BuildProcessorContext> action)
        {
            _action = action;
        }
        
        public override void Execute(BuildProcessorContext context)
        {
            _action?.Invoke(context);
        }
    }
}