using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public static class DirectoryExtension
    {
        public enum DirectoryWalkMode
        {
            BFS,
            DFS,
        }

        private static void GetAllDirectoriesRecurse(string path, List<string> list, DirectoryWalkMode mode)
        {
            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs)
            {
                list.Add(dir.Replace('\\', '/'));

                if (mode == DirectoryWalkMode.DFS)
                    GetAllDirectoriesRecurse(dir, list, mode);
            }

            if (mode == DirectoryWalkMode.BFS)
            {
                foreach (var dir in dirs)
                    GetAllDirectoriesRecurse(dir, list, mode);
            }
        }

        public static List<string> GetAllDirectoriesAndSubDirectories(string path, DirectoryWalkMode mode = DirectoryWalkMode.DFS)
        {
            var list = new List<string>();
            GetAllDirectoriesRecurse(path, list, mode);
            return list;
        }
    }
}
