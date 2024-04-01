using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class VisibleCaseEnumValueAttribute: VisiblePropertyAttribute
    {
        private readonly string _targetName;
        private readonly string _targetValue;
        private readonly bool _revers;
        
        public VisibleCaseEnumValueAttribute(string targetName, object targetValue, bool revers = false)
        {
            _targetName = targetName;
            _targetValue = targetValue.ToString();
            _revers = revers;
        }
        
#if UNITY_EDITOR
        public override bool IsVisible(ScriptableObject asset)
        {
            return _targetValue.Equals(GetValue(asset, _targetName, -1).ToString());
        }

        public override bool IsVisible(SerializedObject sObject)
        {
            var prop = sObject.FindProperty(_targetName);
            if (prop != null)
            {
                var index = prop.enumValueIndex;
                if (index < 0)
                    return false;

                if (index >= prop.enumNames.Length)
                    return false;
                    
                var enumName = prop.enumNames[index];
                return _targetValue.Equals(enumName) ? !_revers : _revers;
            }

            return false;
        }
#endif
    }
}