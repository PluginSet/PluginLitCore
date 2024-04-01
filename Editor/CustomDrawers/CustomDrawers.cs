using UnityEditor;

namespace PluginLit.Core.Editor
{
    [CustomPropertyDrawer(typeof(DrawablePropertyAttribute), true)]
    public class BrowserFileDrawer : DrawablePropertyDrawer
    {
    }
    
    [CustomPropertyDrawer(typeof(LogicPropertyAttribute), true)]
    public class DisableEditDrawer : DefaultPropertyDrawer
    {
    }
    
    [CustomPropertyDrawer(typeof(VisiblePropertyAttribute), true)]
    public class VisibleCaseBoolValueDrawer : DefaultPropertyDrawer
    {
    }
}