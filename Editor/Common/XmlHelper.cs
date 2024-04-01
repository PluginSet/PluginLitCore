using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using JetBrains.Annotations;
using UnityEditor.Rendering;

namespace PluginLit.Core.Editor
{
    public class XmlDocumentTypeEx : XmlDocumentType
    {
        protected internal XmlDocumentTypeEx(string name, string publicId, string systemId, string internalSubset, [NotNull] XmlDocument doc) : base(name, publicId, systemId, null, doc)
        {
        }
    }

    public class XmlDocumentEx : XmlDocument
    {
        public override XmlDocumentType CreateDocumentType(string name, string publicId, string systemId, string internalSubset)
        {
          return new XmlDocumentTypeEx(name, publicId, systemId, internalSubset, this);
        }
    }
    
    public static class XmlHelper
    {
        public static XmlDocument LoadPlistFromFile(string file)
        {
            if (!File.Exists(file))
                throw new Exception("Cannot find file at " + file);
            
            var doc = new XmlDocumentEx();
            doc.Load(file);
            return doc;
        }
        
        public static XmlNode cloneNode(this XmlDocument doc, XmlNode node)
        {
            var clone = doc.CreateNode(node.NodeType, node.Name, null);
            node.cloneTo(clone);
            return clone;
        }

        public static void copyAttributesTo(this XmlElement src, XmlElement dst)
        {
            if (src == null || dst == null)
                return;
            
            if (src.HasAttributes)
            {
                foreach (XmlAttribute attr in src.Attributes)
                {
                    if (!attr.Prefix.Equals("xmlns") && dst.GetAttributeNode(attr.LocalName, attr.NamespaceURI) == null)
                    {
                        dst.SetAttribute(attr.LocalName, attr.NamespaceURI, attr.Value);
                    }
                }
            }
        }
        
        public static void cloneTo(this XmlNode src, XmlNode dst)
        {
            if (src.NodeType != dst.NodeType)
                throw new XmlException("Cannot clone node between different node type");
            
            if (src.OwnerDocument == null || dst.OwnerDocument == null)
                throw new XmlException("Cannot clone node without Owner");
            
            switch (src.NodeType)
            {
                case XmlNodeType.Text:
                    ((XmlText) dst).Value = ((XmlText) src).Value;
                    break;
                case XmlNodeType.Attribute:
                    ((XmlAttribute) dst).Value = ((XmlAttribute) src).Value;
                    break;
                case XmlNodeType.Element:
                    copyAttributesTo(src as XmlElement, dst as XmlElement);
                    break;
            }
            
            if (src.HasChildNodes)
            {
                if (src.OwnerDocument != dst.OwnerDocument)
                {
                    var temp = dst.OwnerDocument.CreateNode(src.NodeType, "Temp", null);
                    temp.InnerXml = src.OuterXml;
                    src = temp.FirstChild;
                }
                for (var srcIter = src.FirstChild; srcIter != null; srcIter = srcIter.NextSibling)
                {
                    bool found = false;
                    for (var dstIter = dst.FirstChild; dstIter != null; dstIter = dstIter.NextSibling)
                    {
                        if (compareNode(dstIter, srcIter, false))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        dst.AppendChild(srcIter.Clone());
                    }
                }
            }
        }
        
        public static void tryMergeTo(this XmlNode src, XmlNode dst, bool warn, string ns, params string[] keyArray)
        {
            if (src.NodeType != dst.NodeType)
                throw new XmlException("Cannot clone node between different node type");
            
            if (src.OwnerDocument == null || dst.OwnerDocument == null)
                throw new XmlException("Cannot clone node without Owner");
            
            switch (src.NodeType)
            {
                case XmlNodeType.Text:
                    if (warn)
                    {
                        if (((XmlText) dst).Value != ((XmlText) src).Value)
                            throw new XmlException("try merge text fail, different values");
                    }
                    else
                    {
                        ((XmlText) dst).Value = ((XmlText) src).Value;
                    }
                    break;
                case XmlNodeType.Attribute:
                    if (warn)
                    {
                        if (((XmlText) dst).Value != ((XmlText) src).Value)
                            throw new XmlException("try merge attribute fail, different values");
                    }
                    else
                    {
                        ((XmlAttribute) dst).Value = ((XmlAttribute) src).Value;
                    }
                    break;
                case XmlNodeType.Element:
                    if (((XmlElement) src).HasAttributes)
                    {
                        var dstNode = (XmlElement) dst;
                        foreach (XmlAttribute attr in ((XmlElement)src).Attributes)
                        {
                            if (!attr.Prefix.Equals("xmlns"))
                            {
                                if (dstNode.GetAttributeNode(attr.LocalName, attr.NamespaceURI) == null)
                                {
                                    dstNode.SetAttribute(attr.LocalName, attr.NamespaceURI, attr.Value);
                                }
                                else if (warn && !dstNode.GetAttribute(attr.LocalName, attr.NamespaceURI)
                                             .Equals(attr.Value))
                                {
                                    throw new XmlException($"try merge element fail, different attribute values with name {attr.LocalName} in element {src.OuterXml}");
                                }
                            }
                        }
                    }
                    break;
            }
            
            if (src.HasChildNodes)
            {
                if (src.OwnerDocument != dst.OwnerDocument)
                {
                    var temp = dst.OwnerDocument.CreateNode(src.NodeType, "Temp", null);
                    temp.InnerXml = src.OuterXml;
                    src = temp.FirstChild;
                }
                for (var srcIter = src.FirstChild; srcIter != null; srcIter = srcIter.NextSibling)
                {
                    bool found = false;
                    for (var dstIter = dst.FirstChild; dstIter != null; dstIter = dstIter.NextSibling)
                    {
                        if (compareNode(dstIter, srcIter, false))
                        {
                            found = true;
                        }

                        if (srcIter.Name == dstIter.Name && srcIter.NodeType == dstIter.NodeType)
                        {
                            if (srcIter.NodeType == XmlNodeType.Element)
                            {
                                XmlElement srcElem = (XmlElement) srcIter;
                                XmlElement dstElem = (XmlElement) dstIter;

                                if (srcElem.HasAttributes && dstElem.HasAttributes)
                                {
                                    foreach (var key in keyArray)
                                    {
                                        string srcAttr = srcElem.GetAttribute(key, ns);
                                        if (string.IsNullOrEmpty(srcAttr))
                                            continue;

                                        string dstAttr = dstElem.GetAttribute(key, ns);
                                        if (string.IsNullOrEmpty(dstAttr) || !srcAttr.Equals(dstAttr))
                                            continue;
                                        
                                        tryMergeTo(srcElem, dstElem, warn, ns, keyArray);
                                        found = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    tryMergeTo(srcElem, dstElem, warn, ns, keyArray);
                                    found = true;
                                }
                            }
                        }

                        if (found)
                            break;
                    }

                    if (!found)
                    {
                        dst.AppendChild(srcIter.Clone());
                    }
                }
            }
        }

        public static XmlElement createElementWithPath(this XmlDocument doc, string path)
        {
            int pos = path.LastIndexOf('/');
            if (pos == 0)
            {
                return doc.findOrCreateElemet(path);
            }

            return doc.findOrCreateElemet(path.Substring(0, pos))
                .createSubElement(path.Substring(pos + 1));
        }

        public static XmlElement createSubElement(this XmlNode element, string name)
        {
            var doc = element.OwnerDocument;
            if (doc == null)
                throw new XmlException("createNode OwnerDocument is null");
            
            var node = doc.CreateElement(name);
            element.AppendChild(node);
            return node;
        }

        public static XmlElement findOrCreateElemet(this XmlDocument doc, string path)
        {
            XmlElement ele = doc.findFirstElement(path);
            if (ele != null)
                return ele;

            return createElementWithPath(doc, path);
        }
        
        public static XmlElement findFirstElement(this XmlDocument doc, string path)
        {
            var node = doc.SelectSingleNode(path);
            if (node?.NodeType == XmlNodeType.Element)
                return node as XmlElement;
            return null;
        }

        public static List<XmlElement> findElements(this XmlDocument doc, string path, string nsPrefix = null, string attrName = null, string attrValue = null)
        {
            var root = doc.DocumentElement;
            if (root == null)
                throw new XmlException("Doc has no root element!");
            
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            if (attrName != null)
            {
                if (attrValue == null)
                    throw new XmlException("findElements attrValue must be no-null when attrName is not null");

                if (string.IsNullOrEmpty(nsPrefix))
                {
                    path = $"{path}[{attrName}='{attrValue}']";
                }
                else
                {
                    path = $"{path}[@{nsPrefix}:{attrName}='{attrValue}']";
                    nsMgr.AddNamespace(nsPrefix, root.GetNamespaceOfPrefix(nsPrefix));
                }
                    
            }
            
            List<XmlElement> elements = new List<XmlElement>();
            var nodes = doc.SelectNodes(path, nsMgr);
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        elements.Add(node as XmlElement);
                    }
                }
            }
            
            return elements;
        }

        public static void addQueries(this XmlDocument doc, string packageName)
        {
            const string path = "/manifest/queries/package";
            const string attrName = "name";
            var list = doc.findElements(path, AndroidConst.NS_PREFIX, attrName, packageName);
            if (list.Count <= 0)
            {
                var elem = doc.createElementWithPath(path);
                elem.SetAttribute(attrName, AndroidConst.NS_URI, packageName);
            }
        }
        

        public static bool compareNode(this XmlNode e1, XmlNode e2, bool nextSiblings)
        {
            while (true)
            {
                e1 = getCompareNode(e1);
                e2 = getCompareNode(e2);
                if ((e1 == e2) || (e1 == null && e2 == null))
                    return true;
                
                if ((e1 == null && e2 != null) || (e1 != null && e2 == null))
                    break;

                if (e1.NodeType != e2.NodeType)
                    break;

                if (e1.Name != e2.Name)
                    break;

                if (e1.Value?.Trim() != e2.Value?.Trim())
                    break;

                if (!compareNodeAttrs(e1.Attributes, e2.Attributes))
                    break;

                if (e1.NodeType == XmlNodeType.Element && e1.ChildNodes.Count != e2.ChildNodes.Count &&
                    !compareNode(e1.FirstChild, e2.FirstChild, true))
                    return false;

                if (nextSiblings)
                {
                    e1 = e1.NextSibling;
                    e2 = e2.NextSibling;
                    continue;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private static XmlNode getCompareNode(XmlNode node)
        {
            while (node != null)
            {
                var t = node.NodeType;
                if (t == XmlNodeType.Comment)
                {
                    node = node.NextSibling;
                }
                else if (t == XmlNodeType.Text)
                {
                    string s = node.Value.Trim();
                    if (string.IsNullOrEmpty(s))
                        node = node.NextSibling;
                    else
                        break;
                }
                else
                {
                    break;
                }
            }

            return node;
        }

        private static bool compareNodeAttrs(XmlAttributeCollection attrs1, XmlAttributeCollection attrs2)
        {
            if (attrs1.Count != attrs2.Count)
                return false;

            foreach (var a in attrs1)
            {
                XmlNode attr1 = (XmlNode) a;
                if (attr1 == null)
                    return false;
                    
                XmlNode attr2 = attrs2[attr1.LocalName, attr1.NamespaceURI];
                if (attr2 == null)
                    return false;

                if (attr1.Value?.Trim() != attr2.Value?.Trim())
                    return false;
            }
            
            return true;
        }
    }
}