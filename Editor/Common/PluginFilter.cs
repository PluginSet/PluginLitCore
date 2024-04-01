using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public class PluginFilter : IShouldIncludeInBuildCallback
    {
        public static Func<string, BuildProcessorContext, bool> IsBuildParamsEnable<T>(params string[] names) where T : ScriptableObject
        {
            return delegate(string s, BuildProcessorContext context)
            {
                if (names == null || names.Length <= 0)
                    names = new[] {"Enable"};
                
                var @params = context.BuildChannels.Get<T>();
                foreach (var name in names)
                {
                    var fieldInfo = typeof(T).GetField(name);
                    if (fieldInfo == null)
                        throw new BuildException($"Expect type {typeof(T)} to have field {name}, got null");
                    var notEnable = !(bool) fieldInfo.GetValue(@params);
                    if (notEnable)
                    {
                        Debug.Log($"Filter lib file ::::::: {s}");
                        return true;
                    }
                }

                return false;
            };
        }
        
        /// <summary>
        /// 推荐 PluginFilter.RegisterFilterExpress
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="filter"></param>
        public static void RegisterFilter(string packageName, Func<string, BuildProcessorContext, bool> filter)
        {
            BuildUtilities.RegisterShouldIncludeInBuildCallback(new PluginFilter(packageName, filter));
        }

        public static void RegisterFilter(string packageName, Dictionary<string, Func<string, BuildProcessorContext, bool>> filters)
        {
            BuildUtilities.RegisterShouldIncludeInBuildCallback(new PluginFilter(packageName, filters));
        }

        public string PackageName { get; private set; }

        private Func<string, BuildProcessorContext, bool> Filter;

        public bool ShouldIncludeInBuild(string path)
        {
            var context = BuildProcessorContext.Current;
            if (context == null)
                return true;
            
            return !Filter.Invoke(Path.GetFileName(path), context);
        }

        private PluginFilter(string packageName, Func<string, BuildProcessorContext, bool> filter)
        {
            PackageName = packageName;
            Filter = filter;
        }

        private PluginFilter(string packageName, Dictionary<string, Func<string, BuildProcessorContext, bool>> filters)
        {
            PackageName = packageName;
            Filter = (fileName, context) =>
            {
                if (filters.TryGetValue(fileName, out var filter))
                {
                    return filter.Invoke(fileName, context);
                }

                return false;
            };
        }
    }
}
