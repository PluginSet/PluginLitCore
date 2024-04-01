using System;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public static class ManifestMerge
    {
        public class MergeFail : Exception
        {
            public MergeFail(string msg)
                : base(msg)
            {
                
            }
        }

        public static void SetMetaData(this XmlDocument main, string name, string value, string msg = null)
        {
            Debug.Log($"SetMetaData:::::::::::: {name} = {value} ::: {msg ?? string.Empty}");
            var parent = main.findOrCreateElemet(AndroidConst.META_DATA_PARENT);

            var nodes = main.findElements(AndroidConst.META_DATA_PATH, AndroidConst.NS_PREFIX, "name", name);
            if (nodes.Count > 0)
            {
                if (nodes.Count > 1)
                    Debug.LogError($"More then one same name:{name} metadata has been set!");

                nodes[0].SetAttribute("value", AndroidConst.NS_URI, value);
                
                if (!string.IsNullOrEmpty(msg))
                    nodes[0].insertComment(msg);
            }
            else
            {
                var node = main.createElementWithPath(AndroidConst.META_DATA_PATH);
                node.SetAttribute("name", AndroidConst.NS_URI, name);
                node.SetAttribute("value", AndroidConst.NS_URI, value);
                parent.AppendChild(node);
                
                if (!string.IsNullOrEmpty(msg))
                    node.insertComment(msg);
            }
        }

        public static XmlElement AddUsePermission(this XmlDocument main, string name, string msg = null)
        {
            foreach (var ele in main.findElements(AndroidConst.USES_PERMISSION_PATH, AndroidConst.NS_PREFIX, "name", name))
            {
                Debug.Log($"The same use-permission {name} has been set");
                return ele;
            }
            var parent = main.createElementWithPath(AndroidConst.USES_PERMISSION_PARENT);
            var node = main.createElementWithPath(AndroidConst.USES_PERMISSION_PATH);
            node.SetAttribute("name", AndroidConst.NS_URI, name);
            parent.AppendChild(node);
            
            if (!string.IsNullOrEmpty(msg))
                node.insertComment(msg);

            return node;
        }

        public static XmlElement FindOrAddActivity(this XmlDocument main, string activityName)
        {
            var list = main.findElements(AndroidConst.ACTIVITY_PATH, AndroidConst.NS_PREFIX, "name", activityName);
            XmlElement element;
            if (list.Count <= 0)
            {
                element = main.createElementWithPath(AndroidConst.ACTIVITY_PATH);
                element.SetAttribute("name", AndroidConst.NS_URI, activityName);
            }
            else
            {
                element = list[0];
            }

            return element;
        }

        public static XmlElement FindOrAddProvider(this XmlDocument main, string name)
        {
            var list = main.findElements(AndroidConst.PROVIDER_PATH, AndroidConst.NS_PREFIX, "name", name);
            XmlElement element;
            if (list.Count <= 0)
            {
                element = main.createElementWithPath(AndroidConst.PROVIDER_PATH);
                element.SetAttribute("name", AndroidConst.NS_URI, name);
            }
            else
            {
                element = list[0];
            }

            return element;
        }

        private static void insertComment(this XmlElement node, string msg)
        {
            var parent = node.ParentNode;
            if (parent == null)
            {
                Debug.LogWarning($"Cannot add commont {msg} for node {node}");
                return;
            }

            var lastNode = node.PreviousSibling;
            while (lastNode != null)
            {
                if (lastNode.NodeType == XmlNodeType.Comment)
                {
                    if (((XmlComment) lastNode).Value.Equals(msg))
                        return;
                    lastNode = lastNode.PreviousSibling;
                }
                else
                {
                    break;
                }
            }
            
            XmlDocument doc = node.OwnerDocument;
            if (doc == null)
            {
                throw new XmlException("Cannot find document in parent " + node);
            }
            
            var descNode = doc.CreateComment(msg);
            parent.InsertBefore(descNode, node);
        }
    }
}
