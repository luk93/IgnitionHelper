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
        public static async Task CreateTemplate(XmlNode node, List<TempInstanceIgni> output, StreamWriter streamWriter, string folderName)
        {
            if (output == null)
            {
                output = new List<TempInstanceIgni>();
            }
            folderName = getFolderName(node, folderName);
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                if (childNode1.Name == "Tag")
                {
                    foreach (XmlNode childNode2 in childNode1.ChildNodes)
                    {
                        if (childNode2.Name == "Property")
                        {
                            if (!String.IsNullOrEmpty(folderName))
                            {
                                XmlAttribute xmlAttribute = childNode2.Attributes["name"];
                                if (xmlAttribute != null)
                                {
                                    if (xmlAttribute.Value == "typeId")
                                    {
                                        if (!output.Exists(item => item.Name == childNode2.InnerText))
                                        {
                                            output.Add(new TempInstanceIgni(childNode2.InnerText, childNode1, folderName));
                                            streamWriter.WriteLine($"Added Template, name: {childNode2.InnerText}, folderName; {folderName}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                await CreateTemplate(childNode1, output, streamWriter, folderName);
            }
        }
        public static async Task CheckXml(XmlNode node, List<TagDataAB> tagDataList, StreamWriter streamWriter, string folderName)
        {
            folderName = getFolderName(node, folderName);
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                if (childNode1.Name == "Tag")
                {
                    XmlAttribute xmlAttribute1 = childNode1.Attributes["type"];
                    XmlAttribute xmlAttribute2 = childNode1.Attributes["name"];
                    if (xmlAttribute1 != null && xmlAttribute2 != null)
                    {
                        if (!String.IsNullOrEmpty(folderName))
                        {
                            if (xmlAttribute1.Value == "UdtInstance")
                            {
                                TagDataAB tagData = tagDataList.Find(item => item.Name.Contains(xmlAttribute2.Value));
                                if (tagData != null)
                                {
                                    tagData.IsAdded = true;
                                    tagData.FolderName = folderName;
                                    streamWriter.WriteLine($"Set tag {tagData.Name} as IsAdded");
                                }
                            }
                        }
                    }
                }
            }
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                await CheckXml(childNode1, tagDataList, streamWriter, folderName);
            }
        }
        public static async Task EditXml(XmlNode node, List<TagDataAB> tagDataList, List<TempInstanceIgni> tempInstList, StreamWriter streamWriter, string folderName)
        {
            folderName = getFolderName(node, folderName);
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                //Find correct folder, where same instances of data type are stored 
                if (childNode1.Name == "Tag")
                {
                    XmlAttribute xmlAttribute1 = childNode1.Attributes["type"];
                    XmlAttribute xmlAttribute2 = childNode1.Attributes["name"];
                    if (xmlAttribute1 != null && xmlAttribute2 != null)
                    {
                        if (xmlAttribute1.Value == "UdtInstance")
                        {
                            if (!String.IsNullOrEmpty(folderName))
                            {
                                //Insert correct Instances from matching Template
                                foreach (TagDataAB tagData in tagDataList)
                                {
                                    if (!tagData.IsAdded)
                                    {
                                        TempInstanceIgni tempInst = tempInstList.Find(item => (item.Name.Contains(tagData.DataType) && item.FolderName == folderName));
                                        if (tempInst != null)
                                        {
                                            XmlNode newNode = tempInst.Node.CloneNode(true);
                                            tempInst.Node.Attributes["name"].Value = tagData.Name;
                                            node.InsertAfter(newNode, node.LastChild);
                                            streamWriter.WriteLine($"Added Node: {tagData.Name}");
                                            tagData.IsAdded = true;
                                            tagData.FolderName = folderName;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                await EditXml(childNode1, tagDataList, tempInstList, streamWriter, folderName);
            }
        }
        private static string getFolderName(XmlNode xmlNode, string folderName)
        {
            string result = folderName;
            if (xmlNode.Name == "Tag")
            {
                XmlAttribute xmlAttribute1 = xmlNode.Attributes["type"];
                XmlAttribute xmlAttribute2 = xmlNode.Attributes["name"];
                if (xmlAttribute1 != null && xmlAttribute2 != null)
                {
                    if (xmlAttribute1.Value == "Folder")
                    {
                        result = xmlAttribute2.Value;
                    }
                }
            }
            return result;
        }
       
    }
}
