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
        public static async Task CreateTemplate(XmlNode node, List<TempInstanceVisu> output, StreamWriter streamWriter, string folderName)
        {
            if (output == null)
            {
                output = new List<TempInstanceVisu>();
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
                                        string name = childNode2.InnerText.Substring(childNode2.InnerText.LastIndexOf(@"/") + 1, childNode2.InnerText.Length - childNode2.InnerText.LastIndexOf(@"/") - 1);
                                        if (!output.Exists(item => item.Name == name))
                                        {
                                            output.Add(new TempInstanceVisu(name, childNode1, folderName));
                                            streamWriter.WriteLine($"Added Template, name: {name}, folderName; {folderName}");
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
        public static async Task CheckXml(XmlNode node, List<TagDataPLC> tagDataList, StreamWriter streamWriter, string folderName)
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
                                //Search for Instance Of Data Block in Xml by the Name got from Excel
                                TagDataPLC tagData = tagDataList.Find(item => item.Name.Contains(xmlAttribute2.Value));
                                if (tagData != null)
                                {
                                    //Search for Instance Of Data Block Data Type in Xml by the Data Type got from Excel
                                    foreach (XmlNode childNode2 in childNode1.ChildNodes)
                                    {
                                        if (childNode2.Name == "Property")
                                        {
                                            XmlAttribute xmlAttribute = childNode2.Attributes["name"];
                                            if (xmlAttribute != null && xmlAttribute.Value == "typeId")
                                            {
                                                string name = childNode2.InnerText.Substring(childNode2.InnerText.LastIndexOf(@"/") + 1, childNode2.InnerText.Length - childNode2.InnerText.LastIndexOf(@"/") - 1);
                                                if (name != null)
                                                {
                                                    //string tolerance is both side:
                                                    if ((StringExt.Contains(name, tagData.DataTypePLC, StringComparison.OrdinalIgnoreCase) || StringExt.Contains(tagData.DataTypePLC, name, StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        tagData.IsAdded = true;
                                                        tagData.IsCorrect = true;
                                                        tagData.FolderName = folderName;
                                                        tagData.DataTypeVisu = name;
                                                        streamWriter.WriteLine($"Set tag {tagData.Name} as IsAdded and Correct");
                                                    }
                                                    else
                                                    {
                                                        tagData.IsAdded = true;
                                                        tagData.IsCorrect = false;
                                                        tagData.FolderName = folderName;
                                                        tagData.DataTypeVisu = name;
                                                        streamWriter.WriteLine($"Set tag {tagData.Name} as IsAdded and NOT Correct");
                                                    }
                                                }
                                            }
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
                await CheckXml(childNode1, tagDataList, streamWriter, folderName);
            }
        }
        public static async Task EditXml(XmlNode node, List<TagDataPLC> tagDataList, List<TempInstanceVisu> tempInstList, StreamWriter streamWriter, string folderName)
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
                                foreach (TagDataPLC tagData in tagDataList)
                                {
                                    if (!tagData.IsAdded)
                                    {
                                        //tolerance in names extended
                                        TempInstanceVisu tempInst = tempInstList.Find(item => (StringExt.Contains(item.Name,tagData.DataTypePLC,StringComparison.OrdinalIgnoreCase) ||
                                                                                            StringExt.Contains(tagData.DataTypePLC, item.Name, StringComparison.OrdinalIgnoreCase)) && item.FolderName == folderName);
                                        if (tempInst != null)
                                        {
                                            XmlNode newNode = tempInst.Node.CloneNode(true);
                                            tempInst.Node.Attributes["name"].Value = tagData.Name;
                                            node.InsertAfter(newNode, node.LastChild);
                                            streamWriter.WriteLine($"Added Node: {tagData.Name}");
                                            tagData.IsAdded = true;
                                            tagData.IsCorrect = true;
                                            tagData.DataTypeVisu = tempInst.Name;
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
