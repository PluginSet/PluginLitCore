using System;

namespace PluginLit.Core.Editor
{
    public class BuildException: Exception
    {
        public BuildException(string msg)
            :base(msg)
        {
            
        }
        
    }
}