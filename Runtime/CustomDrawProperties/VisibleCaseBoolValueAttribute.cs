using System;
using UnityEngine;
#if  UNITY_EDITOR
using UnityEditor;
#endif

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public class VisibleCaseBoolValueAttribute: VisiblePropertyAttribute
    {
        private readonly string _targetName;
        private readonly bool _targetValue;
        
        public VisibleCaseBoolValueAttribute(string targetName, bool targetValue)
        {
            _targetName = targetName;
            _targetValue = targetValue;
        }

#if UNITY_EDITOR
        public override bool IsVisible(ScriptableObject asset)
        {
            return _targetValue.Equals(GetValue(asset, _targetName, !_targetValue));
        }

        public override bool IsVisible(SerializedObject sObject)
        {
            var prop = sObject.FindProperty(_targetName);
            if (prop != null)
                return _targetValue.Equals(prop.boolValue);

            return false;
        }
#endif
    }
}