using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.Assertions;

namespace PluginLit.Core.Editor
{
    /// <summary>
    /// https://gist.github.com/zcyemi/527269ad89c6ed3f10d5867004781e46
    /// </summary>
    public class GradleConfig
    {
        private readonly GradleNode m_root;
        private readonly string m_filePath;

        public GradleNode ROOT => m_root;

        public int TargetSdkVersion
        {
            get
            {
                var node = ROOT.TryGetNode("android/defaultConfig");
                if (node == null)
                    return -1;

                foreach (var child in node.CHILDREN)
                {
                    var match = Regex.Match(child.NAME, "\\s*targetSdkVersion\\s+([0-9]+)");
                    if (!match.Success) continue;

                    return int.Parse(match.Groups[1].Value);
                }
                
                return -1;
            }
        }

        public GradleConfig(string filePath)
        {
            var file = File.ReadAllText(filePath);
            TextReader reader = new StringReader(file);

            m_filePath = filePath;
            m_root = new GradleNode("root");
            var mCurNode = m_root;

            var str = new StringBuilder();

            while (reader.Peek() > 0)
            {
                var c = (char) reader.Read();
                switch (c)
                {
                    case '/':
                        if (reader.Peek() == '/')
                        {
                            var isSuffix = false;
                            var strF = FormatStr(str);
                            if (!string.IsNullOrEmpty(strF))
                            {
                                mCurNode.AppendChildNode(new GradleContentNode(strF, mCurNode));
                                isSuffix = true;
                            }

                            str.Clear();

                            reader.Read();
                            var comment = reader.ReadLine();
                            var commentNode = new GradleCommentNode(comment, mCurNode) {SuffixComment = isSuffix};
                            mCurNode.AppendChildNode(commentNode);
                        }
                        else
                        {
                            str.Append('/');
                        }

                        break;
                    case '\n':
                    {
                        var strF = FormatStr(str);
                        if (!string.IsNullOrEmpty(strF))
                        {
                            mCurNode.AppendChildNode(new GradleContentNode(strF, mCurNode));
                        }
                    }
                        str.Clear();
                        break;
                    case '\r':
                        break;
                    case '\t':
                        break;
                    case '{':
                    {
                        var n = FormatStr(str);
                        if (!string.IsNullOrEmpty(n))
                        {
                            GradleNode node = new GradleNode(n, mCurNode);
                            mCurNode.AppendChildNode(node);
                            mCurNode = node;
                        }
                    }
                        str.Clear();
                        break;
                    case '}':
                    {
                        var strF = FormatStr(str);
                        if (!string.IsNullOrEmpty(strF))
                        {
                            mCurNode.AppendChildNode(new GradleContentNode(strF, mCurNode));
                        }

                        mCurNode = mCurNode.PARENT;
                    }
                        str.Clear();
                        break;
                    case '\'':
                    case '"':
                        str.Append(c);
                        ReadString(c, str, reader);
                        str.Append(c);
                        break;
                    default:
                        str.Append(c);
                        break;
                }
            }
            
            //Debug.Log("Gradle parse done!");
        }

        private void ReadString(char q, StringBuilder sb, TextReader reader)
        {
            var escape = false;
            while (true)
            {
                var c = (char) reader.Read();
                if (escape)
                {
                    sb.Append(c);
                    escape = false;
                }
                else
                {
                    if (c == '\\')
                    {
                        sb.Append(c);
                        escape = true;
                    }
                    else if (c == q)
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
        }

        public void Save(string path = null)
        {
            if (path == null)
                path = m_filePath;
            File.WriteAllText(path, Print());
            //Debug.Log("Gradle parse done! " + path);
        }

        private string FormatStr(StringBuilder sb)
        {
            string str = sb.ToString();
            str = str.Trim();
            return str;
        }

        public string Print()
        {
            var sb = new StringBuilder();
            PrintNode(sb, m_root, -1);
            return sb.ToString();
        }

        private static string GetLevelIndent(int level)
        {
            if (level <= 0) return "";
            var sb = new StringBuilder("");
            for (var i = 0; i < level; i++)
            {
                sb.Append("    ");
            }

            return sb.ToString();
        }

        private static void PrintNode(StringBuilder stringBuilder, GradleNode node, int level)
        {
            if (node.PARENT != null)
            {
                if (node is GradleCommentNode commentNode)
                {
                    stringBuilder.Append($"{(commentNode.SuffixComment ? string.Empty : $"\n{GetLevelIndent(level)}")}//{commentNode.NAME}");
                }
                else
                {
                    stringBuilder.Append("\n" + GetLevelIndent(level) + node.NAME);
                }
            }

            if (!(node is GradleContentNode) && !(node is GradleCommentNode))
            {
                if (node.PARENT != null)
                {
                    stringBuilder.Append(" {");
                }

                foreach (var c in node.CHILDREN)
                {
                    PrintNode(stringBuilder, c, level + 1);
                }

                if (node.PARENT != null)
                {
                    stringBuilder.Append("\n" + GetLevelIndent(level) + "}");
                }
            }
        }
    }

    public class GradleNode
    {
        private List<GradleNode> m_children = new List<GradleNode>();

        private GradleNode m_parent;

        protected string m_name;

        public GradleNode PARENT => m_parent;

        public string NAME => m_name;

        public List<GradleNode> CHILDREN => m_children;

        public GradleNode(string name, GradleNode parent = null)
        {
            m_parent = parent;
            m_name = name;
        }

        public void Each(Action<GradleNode> f)
        {
            f(this);
            foreach (var n in m_children)
            {
                n.Each(f);
            }
        }

        public void AppendChildNode(GradleNode node)
        {
            if (m_children == null) m_children = new List<GradleNode>();
            m_children.Add(node);
            node.m_parent = this;
        }

        public void InsertChildNode(GradleNode node, int insertIndex = 0)
        {
            if (m_children == null) m_children = new List<GradleNode>();
            m_children.Insert(insertIndex, node);
            node.m_parent = this;
        }

        /// <summary>
        /// 节点路径索引
        /// </summary>
        /// <param name="path"> Sample "android/signingConfigs/release"</param>
        /// <returns></returns>
        public GradleNode TryGetNode(string path)
        {
            var subPath = path.Split('/');
            var cNode = this;
            foreach (var p in subPath)
            {
                if (string.IsNullOrEmpty(p))
                    continue;
                var tNode = cNode.FindChildNodeByName(p);
                if (tNode == null)
                {
                    Debug.Log("GradleParser: Can't find Node:" + p);
                    return null;
                }

                cNode = tNode;
            }

            return cNode;
        }

        /// <summary>
        /// 获取指定路径的 Node ，没有则创建
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public GradleNode GetOrCreateNode(string path)
        {
            var subPath = path.Split('/');
            var cNode = this;
            foreach (var p in subPath)
            {
                if (string.IsNullOrEmpty(p))
                    throw new Exception($"Empty path in {path}");
                var tNode = cNode.FindChildNodeByName(p);
                if (tNode == null)
                {
                    tNode = new GradleNode(p);
                    cNode.AppendChildNode(tNode);
                }

                cNode = tNode;
            }

            return cNode;
        }

        public GradleNode FindChildNodeByName(string name)
        {
            foreach (var n in m_children)
            {
                if (n is GradleCommentNode || n is GradleContentNode)
                    continue;
                if (n.NAME == name)
                    return n;
            }

            return null;
        }

        //替换内容
        public bool ReplaceContentStartsWith(string patten, string value)
        {
            foreach (var n in m_children)
            {
                if (!(n is GradleContentNode))
                    continue;
                if (n.m_name.StartsWith(patten))
                {
                    n.m_name = value;
                    return true;
                }
            }

            return false;
        }

        public GradleContentNode ReplaceContentOrAddStartsWith(string patten, string value)
        {
            foreach (var n in m_children)
            {
                if (!(n is GradleContentNode))
                    continue;
                if (n.m_name.StartsWith(patten))
                {
                    n.m_name = value;
                    return (GradleContentNode) n;
                }
            }

            return AppendContentNode(value);
        }

        private GradleContentNode FindContentNodeWithVersionMatcher(string versionMatcher)
        {
            var parts = GradleContentNode.ExtractVersionMatcherParts(versionMatcher);
            return m_children.OfType<GradleContentNode>().FirstOrDefault(n => n.m_name.Contains(parts[0]) && n.m_name.Contains(parts[1]));
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <param name="content"></param>
        /// <param name="versionMatcher"></param>
        /// <returns></returns>
        public GradleContentNode AppendContentNode(string content, string versionMatcher = null)
        {
            if (!string.IsNullOrEmpty(versionMatcher))
            {
                var findNode = FindContentNodeWithVersionMatcher(versionMatcher);
                if (findNode == null)
                {
                    Debug.Log($"GradleParser: Cant find node with pattern {versionMatcher}, adding new node.");
                    var currNode = new GradleContentNode(content, this);
                    AppendChildNode(currNode);
                    return currNode;
                }

                var version = GradleContentNode.ExtractVersion(content, versionMatcher);
                var currVersion = GradleContentNode.ExtractVersion(findNode.NAME, versionMatcher);
                if (GradleContentNode.CompareVersion(currVersion, version) >= 0)
                {
                    Debug.Log($"GradleParser: incoming version {content} is lower than or the same as current version {findNode.NAME}! ignore");
                    return findNode;
                }

                Debug.Log($"GradleParser: replace {findNode.NAME} with higher version {content}");
                findNode.m_name = content;
                return findNode;
            }

            if (m_children.OfType<GradleContentNode>().Any(n => n.m_name == content))
            {
                Debug.Log("GradleParser: GradleContentNode with " + content + " already exists! Ignore");
                return null;
            }

            Debug.Log($"GradleParser: append new node {content}");
            var newNode = new GradleContentNode(content, this);
            AppendChildNode(newNode);
            return newNode;
        }

        public bool RemoveContentNode(string contentPattern)
        {
            for (int i = 0; i < m_children.Count; i++)
            {
                if (!(m_children[i] is GradleContentNode))
                    continue;
                if (m_children[i].m_name.Contains(contentPattern))
                {
                    m_children.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class GradleContentNode : GradleNode, IComparable<GradleContentNode>
    {
        public static string[] ExtractVersionMatcherParts(string versionMatcher)
        {
            var parts = versionMatcher.Split(new[] {"{0}"}, StringSplitOptions.None);
            Assert.IsTrue(parts.Length == 2, $"Expect {versionMatcher} to be ***{{0}}***");
            return parts;
        }

        private static List<int> ParseVersion(string version)
        {
            return version.Split('.').Select(e => e == "+" ? int.MaxValue : int.Parse(e)).ToList();
        }

        public static List<int> ExtractVersion(string content, string versionMatcher)
        {
            var parts = ExtractVersionMatcherParts(versionMatcher);
            var versionStr = content.Replace(parts[0], string.Empty).Replace(parts[1], string.Empty);
            return ParseVersion(versionStr);
        }

        public static int CompareVersion(List<int> va, List<int> vb)
        {
            if (va.Count == 0 && vb.Count == 0)
                return 0;

            for (var i = 0; i < va.Count && i < vb.Count; i++)
            {
                var diff = va[i] - vb[i];
                if (diff == 0)
                    continue;
                return diff;
            }

            return va.Count - vb.Count;
        }

        private List<int> _version;

        public GradleContentNode(string content, GradleNode parent) : base(content, parent)
        {
        }

        public void SetContent(string content)
        {
            m_name = content;
        }

        public void SetVersion(string version)
        {
            _version = ParseVersion(version);
        }

        public int CompareTo(GradleContentNode other)
        {
            return CompareVersion(_version ?? new List<int>(), other._version ?? new List<int>());
        }
    }

    public sealed class GradleCommentNode : GradleNode
    {
        public bool SuffixComment;

        public GradleCommentNode(string content, GradleNode parent) : base(content, parent)
        {
        }

        public string GetComment()
        {
            return m_name;
        }
    }
}
