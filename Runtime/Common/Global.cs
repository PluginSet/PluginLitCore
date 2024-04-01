#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEditor.PackageManager;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PluginLit.Core
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class OrderCallBack : Attribute
    {
        public int Order;

        public OrderCallBack(int order)
        {
            Order = order;
        }
    }

    internal class MethodOrder
    {
        public MethodBase Method;
        public int Order;
    }

    public struct BundleInfo
    {
        public string FilePath;
        public string Name;
        public string Variant;
    }

    public static class Global
    {
        public delegate void CopyFileDelegate(string src, string dst);
        public delegate bool FilterDelegate(string name);

        public static string PlaybackEnginesPath
        {
            get
            {
#if UNITY_EDITOR_OSX
                return Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "PlaybackEngines");
#else
                return Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines");
#endif
            }
        }

        public static string StreamingAssetsPath => Path.Combine(Application.dataPath, "StreamingAssets");

        public static IEnumerable<Type> GetAllTypes(string assemblyName = null, bool excludeGen = false)
        {
            return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                where (assemblyName == null || assembly.GetName().Name.Equals(assemblyName))
#if !NET_STANDARD_2_0
                      && !(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder)
#endif
                from type in assembly.GetTypes()
                where !excludeGen || !type.IsGenericTypeDefinition
                select type;
        }

        public static IEnumerable<Type> GetAllTypes<T>(string assemblyName = null, bool excludeGen = false)
        {
            return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                where (assemblyName == null || assembly.GetName().Name.Equals(assemblyName))
#if !NET_STANDARD_2_0
                      && !(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder)
#endif
                from type in assembly.GetTypes()
                where (!excludeGen || !type.IsGenericTypeDefinition) && type.IsDefined(typeof(T), false)
                select type;
        }

        public static IEnumerable<MethodInfo> GetMethods<T>(string assembly = null, BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) where T : Attribute
        {
            return from type in GetAllTypes(assembly)
                from method in type.GetMethods(flags)
                where method.IsDefined(typeof(T), false)
                select method;
        }

        public static IEnumerable<MethodInfo> GetMethods<T, TC>(string assembly = null, BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) where T : Attribute
        {
            return from type in GetAllTypes<TC>(assembly)
                from method in type.GetMethods(flags)
                where method.IsDefined(typeof(T), false)
                select method;
        }

        public static IEnumerable<PropertyInfo> GetProperties<T>(string assembly = null, BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) where T : Attribute
        {
            return from type in GetAllTypes(assembly)
                from prop in type.GetProperties(flags)
                where prop.IsDefined(typeof(T), false)
                select prop;
        }

        public static IEnumerable<PropertyInfo> GetProperties<T, TC>(string assembly = null, BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) where T : Attribute
        {
            return from type in GetAllTypes<TC>(assembly)
                from prop in type.GetProperties(flags)
                where prop.IsDefined(typeof(T), false)
                select prop;
        }

        public static void CallCustomMethods<T>(params object[] args) where T : Attribute
        {
            foreach (var method in GetMethods<T>())
            {
                method.Invoke(null, args);
            }
        }

        public static void CallCustomMethods<T, TC>(params object[] args) where T : Attribute
        {
            foreach (var method in GetMethods<T, TC>())
            {
                method.Invoke(null, args);
            }
        }

        public static void CallCustomOrderMethods<T>(params object[] args) where T : OrderCallBack
        {
            ExecuteOrderMethods<T>(GetMethods<T>(), args);
        }


        public static void CallCustomOrderMethods<T, TC>(params object[] args) where T : OrderCallBack
        {
            ExecuteOrderMethods<T>(GetMethods<T, TC>(), args);
        }

        public static void ExecuteOrderMethods<T>(IEnumerable<MethodInfo> methods, params object[] args) where T : OrderCallBack
        {
            List<MethodOrder> methodOrders = new List<MethodOrder>();
            foreach (var method in methods)
            {
                methodOrders.Add(new MethodOrder {Method = method, Order = getOrderMethodOrder<T>(method)});
            }

            methodOrders.Sort((a, b) =>
            {
                return a.Order < b.Order ? -1 : (a.Order > b.Order ? 1 : 0);
            });
            var count = methodOrders.Count;
            for (int i = 0; i < count; i++)
            {
                methodOrders[i].Method.Invoke(null, args);
            }
        }

        public static void CheckAndRemoveFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void CheckAndDeletePath(string path, bool checkIsEmpty = false)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (path.ToLower().Equals(Application.dataPath.ToLower()))
                return;

            if (path.ToLower().Equals(Path.Combine(Application.dataPath, "..").ToLower()))
                return;

            if (File.Exists(path))
            {
                File.Delete(path);
                CheckAndRemoveFile(path + ".meta");
                return;
            }

            if (!Directory.Exists(path))
                return;

            if (checkIsEmpty && (Directory.GetDirectories(path).Length > 0 || Directory.GetFiles(path).Length > 0))
                return;

            CheckAndRemoveFile(path + ".meta");
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            CheckAndDeletePath(Path.GetDirectoryName(path), true);
        }

        public static void CheckAndCopyFile(string src, string dst)
        {
            var dir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.Copy(src, dst, true);
        }

        public static void CopyFile(string src, string dst, CopyFileDelegate copy)
        {
            var dir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (copy != null)
                copy.Invoke(src, dst);
            else
                CheckAndCopyFile(src, dst);
        }

        public static void CopyFileTo(string file, string path, CopyFileDelegate copy = null)
        {
            var dst = path;
            var fileName = Path.GetFileName(file);
            if (!string.IsNullOrEmpty(fileName))
                dst = Path.Combine(path, fileName);
            
            CopyFile(file, dst, copy);
        }
        
        public static void CopyFilesTo(string dstPath, string srcPath, string pattern, SearchOption option = SearchOption.AllDirectories)
        {
            if (!Directory.Exists(srcPath))
                return;

            srcPath = Path.GetFullPath(srcPath);
            dstPath = Path.GetFullPath(dstPath);
            foreach (var file in Directory.GetFiles(srcPath, pattern, option))
            {
                CopyFile(file, file.Replace(srcPath, dstPath), null);
            }
        }

        public static void CopyFileFromDirectory(string src, string dst, string fileName)
        {
            var srcFile = Path.Combine(src, fileName);
            if (!File.Exists(srcFile))
            {
                Debug.LogWarning($"Cannot find file {srcFile}");
                return;
            }

            File.Copy(srcFile, Path.Combine(dst, fileName), true);
        }

        public static void CopyFiles(string src, string dst, CopyFileDelegate copy, string pattern = "*.*"
            , SearchOption option = SearchOption.AllDirectories)
        {
            CopyFiles(src, dst, pattern, option, copy);
        }

        /// <summary>
        /// 拷贝文件夹
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="pattern"></param>
        /// <param name="option"></param>
        /// <param name="copy"></param>
        /// <param name="isNeglectMete">是否忽略meta文件</param>
        public static void CopyFiles(string src, string dst, string pattern = "*.*"
            , SearchOption option = SearchOption.AllDirectories, CopyFileDelegate copy = null,bool isNeglectMete = true)
        {
            if (src.EndsWith("/") || src.EndsWith("\\"))
                src = src.Substring(0, src.Length - 1);

            if (!Directory.Exists(src))
                return;

            foreach (var file in Directory.GetFiles(src, pattern, option))
            {
                if (isNeglectMete && file.Contains(".meta"))
                    continue;
                string newFile = file.Replace(src, dst);
                if (copy != null)
                    copy.Invoke(file, newFile);
                else
                    CheckAndCopyFile(file, newFile);
            }
        }

        public static string[] GetLowerSubPaths(string parent, string[] files)
        {
            var count = files.Length;
            string[] result = new string[count];
            string fullparent = GetFullPath(parent).ToLower() + "/";
            for (int i = 0; i < count; i++)
            {
                result[i] = GetFullPath(files[i]).ToLower().Replace(fullparent, "");
            }

            return result;
        }

        public static string GetSubPath(string parent, string file)
        {
            string fullparent = GetFullPath(parent) + "/";
            return GetFullPath(file).Replace(fullparent, "");
        }

        public static string GetAssetPath(string path)
        {
            return "Assets/" + GetSubPath(Application.dataPath, path);
        }

        public static string GetFullPath(string path)
        {
            return Path.GetFullPath(path).Replace("\\", "/");
        }

        public static string GetRelativePath(string parent, string file)
        {
            string fullparent = GetFullPath(parent);
            string fullPath = GetFullPath(file);

            List<string> parentSplits = new List<string>(fullparent.Split('/'));
            List<string> currentSplits = new List<string>(fullPath.Split('/'));

            while (parentSplits.Count > 0 && currentSplits.Count > 0)
            {
                if (!parentSplits[0].Equals(currentSplits[0]))
                    break;

                parentSplits.RemoveAt(0);
                currentSplits.RemoveAt(0);
            }

            for (int i = 0; i < parentSplits.Count; i++)
            {
                currentSplits.Insert(0, "..");
            }

            return string.Join("/", currentSplits);
        }

        public static AssetBundleManifest BuildAssetBundles(BuildTarget target, string path, AssetBundleBuild[] builds, BuildAssetBundleOptions options = BuildAssetBundleOptions.None)
        {
            if (builds.Length <= 0)
                return null;

            var manifest = BuildPipeline.BuildAssetBundles(path, builds, options, target);
            if (manifest != null)
                Debug.Log("Build AssetBundles: \n" + string.Join("\n", manifest.GetAllAssetBundles()));
            return manifest;
        }

        public static string[] GetRelyableFiles(string path = null)
        {
            if (string.IsNullOrEmpty(path))
                path = Application.dataPath;

            var withoutExtensions = new List<string>() {".prefab", ".unity", ".mat", ".asset", ".controller", ".spriteatlas"};
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(s => withoutExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
            return files;
        }

        public static string GetPackageFullPath(string packageName)
        {
            // Check for potential UPM package
            string packagePath = Path.GetFullPath($"Packages/{packageName.ToLower()}");
            if (Directory.Exists(packagePath))
            {
                return packagePath;
            }

            packagePath = Path.GetFullPath("Assets/..");
            if (Directory.Exists(packagePath))
            {
                // Search default location for development package
                if (Directory.Exists(packagePath + $"/Assets/Packages/{packageName}/Editor Resources"))
                {
                    return packagePath + $"/Assets/Packages/{packageName}";
                }
            }

            return null;
        }


        public static string GetAssetsUids(params string[] files)
        {
            List<string> uids = new List<string>();
            foreach (var file in files)
            {
                if (!string.IsNullOrEmpty(file))
                {
                    string uid = AssetDatabase.AssetPathToGUID(file);
                    if (!string.IsNullOrEmpty(uid))
                        uids.Add(uid);
                }
            }

            return string.Join("|", uids);
        }


        public static List<string> FindAllReferences(string path, string[] targets)
        {
            List<string> result = new List<string>();
            var files = GetRelyableFiles(path);
            var dirtyFiles = targets.Select(s => GetSubPath(".", s)).ToArray();
            foreach (var file in files)
            {
                var subPath = GetSubPath(".", file);
                var dependencies = AssetDatabase.GetDependencies(subPath, true);
                if (dirtyFiles.Any(f => dependencies.Contains(f)))
                    result.Add(file);
            }

            return result;
        }

        public static List<string> GetAllModifyFiles(params string[] dirtyFiles)
        {
            List<string> result = new List<string>();
            result.AddRange(dirtyFiles);

            bool loop = true;
            while (loop)
            {
                loop = GetModifyFiles(ref result, result.ToArray());
            }

            return result;
        }

        private static readonly string[] AssetExts = new[]
        {
            "txt", "bytes", "ttf", "otf", "json", "fbx",
            "prefab", "unity", "anim", "controller", "overrideController",
            "mat", "spriteatlas", "asset", "shader",
            "png", "jpg", "bmp", "tif", "gif",
            "mp4", "mp3", "ogg", "wav"
        };

        public static bool IsAsset(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext))
                return false;

            return AssetExts.Contains(ext.ToLower().Substring(1));
        }

        public static bool GetModifyFiles(ref List<string> list, params string[] dirtyfiles)
        {
            bool dirty = false;
            foreach (var file in FindAllReferences(dirtyfiles))
            {
                if (!list.Contains(file))
                {
                    list.Add(file);
                    dirty = true;
                }
            }

            return dirty;
        }

        public static List<string> GetAllDependencies(params string[] targets)
        {
            var list = new List<string>();

            foreach (var fileName in GetLowerSubPaths(".", targets))
            {
                if (!File.Exists(fileName))
                    continue;
                list.AddRange(AssetDatabase.GetDependencies(fileName));
            }

            return list;
        }

        public static List<string> FindAllReferences(params string[] targets)
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            string[] files = GetRelyableFiles();
            string uids = GetAssetsUids(GetLowerSubPaths(".", targets));

            List<string> result = new List<string>();
            if (!string.IsNullOrEmpty(uids) && files.Length > 0)
            {
                foreach (var file in files)
                {
                    if (Regex.IsMatch(File.ReadAllText(file), uids))
                    {
                        result.Add(file);
                    }
                }
            }

            return result;
        }

        public static void ExecuteCommand(string fileName, params string[] args)
        {
            
            ExecuteCommand(fileName, Application.platform == RuntimePlatform.WindowsEditor, args);
        }

        public static void ExecuteCommand(string fileName, bool useShell, params string[] args)
        {
            ExecuteCommand(fileName, null, useShell, args);
        }

        public static void SetFileExecutable(string fileName, string workDir = null)
        {
//            if (Regex.IsMatch(fileName, @"\s*[\w]+\s*"))
//                return;
//
            var path = fileName;
            if (!string.IsNullOrEmpty(workDir) && !Path.IsPathRooted(path))
            {
                path = Path.Combine(workDir, fileName);
            }

            if (!File.Exists(path))
                return;
            
            ExecuteCommand("chmod", false, "u+x ", path);
        }
        
        public static void ExecuteCommand(string fileName, string workDir, bool useShell, params string[] args)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                SetFileExecutable(fileName, workDir);
            }
            
            Process process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = string.Join(" ", args);
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.RedirectStandardOutput = !useShell;
            process.StartInfo.UseShellExecute = useShell;
            process.StartInfo.CreateNoWindow = !useShell;
            process.StartInfo.ErrorDialog = true;
            process.StartInfo.WorkingDirectory = workDir ?? Environment.CurrentDirectory;
            process.Start();

            Debug.Log(process.StartInfo.FileName);
            Debug.Log(process.StartInfo.Arguments);

            StringBuilder exceptionInfo = null;
            if (useShell)
            {
                process.WaitForExit();
            }
            else
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    if (exceptionInfo != null)
                    {
                        exceptionInfo.AppendLine(line);
                    }
                    else
                    {
                        if (line.StartsWith("Warning:"))
                        {
                            Debug.LogWarning(line);
                        }
                        else if (line.StartsWith("Error:"))
                        {
                            Debug.LogError(line);
                        }
                        else if (line.StartsWith("Unhandled Exception:"))
                        {
                            exceptionInfo = new StringBuilder(line);
                        }
                        else
                        {
                            Debug.Log(line);
                        }
                    }
                }
                
                process.WaitForExit();
                process.Close();

                if (exceptionInfo != null)
                {
                    Debug.LogError(exceptionInfo);
                }
            }
        }

        public static void AddPlatformSymbol(BuildTargetGroup target, string symbol)
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            if (string.IsNullOrEmpty(symbols))
            {
                symbols = symbol;
            }
            else
            {
                if (symbols.Split(';').Any(s => s.Trim().Equals(symbol)))
                    return;
                symbols += ";" + symbol;
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, symbols);
        }

        public static void RemovePlatformSymbol(BuildTargetGroup target, string symbol)
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            if (string.IsNullOrEmpty(symbols))
                return;

            var list = new List<string>(symbols.Split(';'));
            list.RemoveAll(sym => sym.Trim().Equals(symbol));
            symbols = string.Join(";", list);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, symbols);
        }

        public static void RenameBundleFiles(string path, AssetBundleManifest manifest, Func<string, AssetBundleManifest, string> changeFunc)
        {
            // rename all bundles
            foreach (var name in manifest.GetAllAssetBundles())
            {
                var filePath = Path.Combine(path, name);
                if (!File.Exists(filePath))
                    continue;

//				var manifestFile = Path.Combine(path, $"{name}.manifest");
//				if (File.Exists(manifestFile))
//					File.Delete(manifestFile);

                var targetFile = Path.Combine(path, changeFunc(name, manifest));
                if (File.Exists(targetFile))
                    File.Delete(targetFile);

                File.Move(filePath, targetFile);
            }
        }

        public static void ClearDirectory(string path, string pattern)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path, pattern, SearchOption.AllDirectories))
            {
                File.Delete(file);
            }
        }

        public static void MoveAllFilesToPath(string sourcePath, string destPath, string pattern = "*.*", SearchOption option = SearchOption.AllDirectories)
        {
            if (GetFullPath(sourcePath).ToLower().Equals(GetFullPath(destPath).ToLower()))
                return;

            foreach (var file in Directory.GetFiles(sourcePath, pattern, option))
            {
                var targetPath = file.Replace(sourcePath, destPath);
                if (File.Exists(targetPath))
                    File.Delete(targetPath);
                var direct = Path.GetDirectoryName(targetPath);
                if (string.IsNullOrEmpty(direct))
                    continue;
                if (!Directory.Exists(direct))
                    Directory.CreateDirectory(direct);
                File.Move(file, targetPath);
            }
        }

        public static void RevertFileBundleInfo(BundleInfo info)
        {
            var tempPath = GetSubPath(".", info.FilePath);
            var importer = AssetImporter.GetAtPath(tempPath);
            if (importer == null)
                return;

            importer.assetBundleName = info.Name;
            importer.assetBundleVariant = info.Variant;
            importer.SaveAndReimport();
        }

        public static void AppendProguardInLib(StringBuilder proguard, string libName, string subPath = null)
        {
            var path = GetPackageFullPath(libName);
            if (string.IsNullOrEmpty(path)) return;
            path = Path.Combine(path, "Plugins", "Android");
            if (!string.IsNullOrEmpty(subPath))
                path = Path.Combine(path, subPath);
            path = Path.Combine(path, "proguard-user.txt");
            
            if (!File.Exists(path)) return;
            proguard.AppendLine(File.ReadAllText(path));
        }

        public static void CopyDependenciesInLib(string libName, string pathName = "Dependencies", FilterDelegate filter = null)
        {
            var libPath = GetPackageFullPath(libName);
            if (string.IsNullOrEmpty(libPath))
                return;
            var srcPath = Path.Combine(libPath, pathName);
            if (!Directory.Exists(srcPath))
            {
                Debug.LogWarning($"Cannot find directory : {srcPath}");
                return;
            }
            
            var dependenciesPath = Path.Combine(Application.dataPath, "PluginDependencies", "Editor");
            foreach (var file in Directory.GetFiles(srcPath, "*.xml", SearchOption.AllDirectories))
            {
                var subPath = GetSubPath(srcPath, file);
                if (filter != null && filter.Invoke(subPath))
                    continue;
                    
                CopyFile(file, Path.Combine(dependenciesPath, subPath), null);
            }
        }

        public static void CopyDependenciesFileInLib(string libName, string fileName, string pathName = "Dependencies", CopyFileDelegate @delegate = null)
        {
            var libPath = GetPackageFullPath(libName);
            if (string.IsNullOrEmpty(libPath))
                return;

            if (!fileName.EndsWith(".xml"))
                fileName += ".xml";
            
            var fileFullPath = Path.Combine(libPath, pathName, fileName);
            if (!File.Exists(fileFullPath))
            {
                Debug.LogWarning($"Cannot find dependencies file : {fileFullPath}");
                return;
            }
            
            var dependenciesPath = Path.Combine(Application.dataPath, "PluginDependencies", "Editor");
            CopyFileTo(fileFullPath, dependenciesPath, @delegate);
        }
        
        public static Dictionary<string, string> GetCommandParams(string[] args, string param_start = "-")
        {
            var result = new Dictionary<string, string>();
            if (args == null || args.Length <= 0)
                return result;
            
            var start_len = param_start.Length;
            string lastKey = null;
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith(param_start))
                {
                    if (!string.IsNullOrEmpty(lastKey))
                    {
                        if (result.ContainsKey(lastKey))
                        {
                            throw new Exception("Multiple key setting in args:: " + string.Join(" ", args));
                        }
                        result.Add(lastKey, "");
                    }

                    lastKey = arg.Remove(0, start_len).ToLower();
                }
                else
                {
                    if (string.IsNullOrEmpty(lastKey))
                    {
                        throw new Exception($"Unknown params name for string: {arg} in command: {string.Join(" ", args)} ");
                    }
                    result.Add(lastKey, arg);

                    lastKey = null;
                }
            }
            
            if (!string.IsNullOrEmpty(lastKey))
            {
                if (result.ContainsKey(lastKey))
                {
                    throw new Exception("Multiple key setting in args:: " + string.Join(" ", args));
                }
                result.Add(lastKey, "");
            }

            return result;
        }

        public static TV TryGet<TK, TV>(this Dictionary<TK, TV> dict, TK key, TV defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
                return value;

            return defaultValue;
        }

        public static List<T> CollectSameItemsAndRemove<T>(List<T> lista, List<T> listb)
        {
            var result = new List<T>();
            for (int i = lista.Count - 1; i >= 0; i--)
            {
                var itema = lista[i];
                if (listb.Contains(itema))
                {
                    result.Add(itema);
                    lista.Remove(itema);
                    listb.Remove(itema);
                }
            }

            return result;
        }
        
        public static List<T> CollectMatchItemsAndRemove<T>(List<T> lista, List<T> listb, Func<T, T, bool> matchFunc)
        {
            var result = new List<T>();
            for (int i = lista.Count - 1; i >= 0; i--)
            {
                var itema = lista[i];
                for (int j = listb.Count - 1; j >= 0; j--)
                {
                    var itemb = listb[j];
                    if (matchFunc(itema, itemb))
                    {
                        result.Add(itema);
                        lista.Remove(itema);
                        listb.Remove(itemb);
                        break;
                    }
                }
            }

            return result;
        }

        public static bool CheckGitLibImported(string libName, string gitHttpAddress)
        {

            var added = IncludePackage(libName);
            if (added)
                return true;

            var request = Client.Add(gitHttpAddress);
            EditorUtility.DisplayProgressBar($"import {libName}", "loading...", 0);
            while (!request.IsCompleted)
            {
                EditorApplication.Step();
            }

            EditorUtility.ClearProgressBar();
            if (request.Status == StatusCode.Success)
            {
                Debug.LogFormat("Add {0} package success!", libName);
                return true;
            }
            
            if (Application.isBatchMode)
                throw new Exception($"{libName} package need!");
            
            Debug.LogWarning($"Add {libName} package fail!");

            return false;
        }
        
        public static bool IncludePackage(string packageName)
        {
            return !string.IsNullOrEmpty(GetPackageFullPath(packageName));
        }

        static int getOrderMethodOrder<T>(MethodBase info) where T : OrderCallBack
        {
            return ((OrderCallBack) info.GetCustomAttributes(typeof(T), false).First()).Order;
        }

        public static string GetProjectAssetsPath(BuildTarget target, string exportPath)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return Path.Combine(exportPath, "unityLibrary", "src", "main", "assets");
                case BuildTarget.iOS:
                    return Path.Combine(exportPath, "Data", "Raw");
                case BuildTarget.WebGL:
                    return Path.Combine(exportPath, "StreamingAssets");
                default:
                    return null;
            }
        }
    }
}
#endif