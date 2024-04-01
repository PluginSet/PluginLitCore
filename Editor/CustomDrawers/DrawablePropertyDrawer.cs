using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public class DrawablePropertyDrawer: CustomPropertyBaseDrawer
    {
        protected override void PropertyField(Rect position, SerializedProperty property, GUIContent label)
        {
            var toolTipAttrs = fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false);
            var toolTip = toolTipAttrs.Length > 0 ? toolTipAttrs.First() : null;
            if (toolTip != null)
                label.tooltip = ((TooltipAttribute) toolTip).tooltip;
            ((DrawablePropertyAttribute)attribute).DrawProperty(position, property, label);
        }
    }
}