using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IgnitionHelper
{
    public static class XmlOperations
    {
        public static async Task CreateTemplate(XmlDocument xmlDoc, XmlNode node, List<TemplateNode> output, StreamWriter streamWriter)
        {
            if (output == null)
            {
                output = new List<TemplateNode>();
            }
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                if (childNode1.Name == "Tag")
                {
                    foreach (XmlNode childNode2 in childNode1.ChildNodes)
                    {
                        if (childNode2.Name == "Property")
                        {
                            XmlAttribute xmlAttribute = childNode2.Attributes["name"];
                            if (xmlAttribute != null)
                            {
                                if (xmlAttribute.Value == "typeId")
                                {
                                    if (!output.Exists(item => item.Name == childNode2.InnerText))
                                    {
                                        output.Add(new TemplateNode(childNode2.InnerText, childNode1));
                                        streamWriter.WriteLine($"Added Template, name: {childNode2.InnerText}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                await CreateTemplate(xmlDoc, childNode1, output, streamWriter);
            }
        }
        public static async Task CheckXml(XmlDocument xmlDoc, XmlNode node, List<TagData> tagDataList, List<TemplateNode> tempNodeList, StreamWriter streamWriter)
        {
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                if (childNode1.Name == "Tag")
                {
                    XmlAttribute xmlAttribute1 = childNode1.Attributes["type"];
                    XmlAttribute xmlAttribute2 = childNode1.Attributes["name"];
                    if (xmlAttribute1 != null && xmlAttribute2 != null)
                    {
                        if (xmlAttribute1.Value == "UdtInstance")
                        {
                            TagData tagData = tagDataList.Find(item => item.Name.Contains(xmlAttribute2.Value));
                            if (tagData != null)
                            {
                                tagData.IsAdded = true;
                                streamWriter.WriteLine($"Set tag {tagData.Name} as IsAdded");
                            }
                        }
                    }
                }
            }
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                await CheckXml(xmlDoc, childNode1, tagDataList, tempNodeList, streamWriter);
            }
        }
        public static async Task EditXml(XmlDocument xmlDoc, XmlNode node, List<TagData> tagDataList, List<TemplateNode> tempNodeList, StreamWriter streamWriter)
        {
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                if (childNode1.Name == "Tag")
                {
                    XmlAttribute xmlAttribute = childNode1.Attributes["type"];
                    if (xmlAttribute.Value == "Folder")
                    {
                        XmlNode childNode2 = childNode1.FirstChild;
                        foreach (TagData tagData in tagDataList)
                        {
                            if (!tagData.IsAdded)
                            {
                                //Hard set name to change
                                tagData.IsAdded = true;
                                TemplateNode tempNode = tempNodeList.Find(item => item.Name.Contains(tagData.DataType));
                                if (tempNode != null)
                                {
                                    XmlNode newNode = tempNode.Node.CloneNode(true);
                                    tempNode.Node.Attributes["name"].Value = tagData.Name;
                                    childNode2.InsertAfter(newNode, childNode2.FirstChild);
                                    streamWriter.WriteLine($"Added Node: {tagData.Name}");
                                }
                            }
                        }
                    }
                }
            }
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                await EditXml(xmlDoc, childNode1, tagDataList, tempNodeList, streamWriter);
            }
        }
    }
}
