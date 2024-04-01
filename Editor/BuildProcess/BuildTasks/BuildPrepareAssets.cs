using System.IO;

namespace PluginLit.Core.Editor
{
    public class BuildPrepareAssets: BuildProcessorTask
    {
	    public override void Execute(BuildProcessorContext context)
	    {
		    
            Global.CallCustomOrderMethods<PrepareAssetsBeforeBuildAttribute, BuildToolsAttribute>(context);
	    
		    var config = context.BuildChannels;
		    if (config.ExtendStreamingAssets == null)
			    return;
		    
		    var streamingAssetsPath = Global.StreamingAssetsPath;
		    foreach (var path in config.ExtendStreamingAssets)
		    {
			    if (File.Exists(path))
				    Global.CopyFileTo(path, streamingAssetsPath);
			    else if (Directory.Exists(path))
				    Global.CopyFilesTo(streamingAssetsPath, path, "*.*");
		    }
	    }
    }
}