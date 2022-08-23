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
        public static async Task CreateTemplate(XmlNode node, List<TempInstanceVisu> output, StreamWriter streamWriter, string folderName, string path)
        {
            if (output == null)
            {
                output = new List<TempInstanceVisu>();
            }
            path = getPath(node, path);
            folderName = getFolderName(path);
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
                                            output.Add(new TempInstanceVisu(name, childNode1, folderName, path));
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
                await CreateTemplate(childNode1, output, streamWriter, folderName, path);
            }
        }
        public static async Task setPLCTagInHMIStatus(XmlNode node, List<TagDataPLC> tagDataList, StreamWriter streamWriter, string folderName, string path)
        {
            path = getPath(node, path);
            folderName = getFolderName(path);

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
                                TagDataPLC tagData = tagDataList.Find(item => item.Name == xmlAttribute2.Value);
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
                                                        tagData.VisuFolderName = folderName;
                                                        tagData.VisuPath = path;
                                                        tagData.DataTypeVisu = name;
                                                        streamWriter.WriteLine($"Set tag {tagData.Name} as IsAdded and Correct");
                                                    }
                                                    else
                                                    {
                                                        tagData.IsAdded = true;
                                                        tagData.IsCorrect = false;
                                                        tagData.VisuFolderName = folderName;
                                                        tagData.VisuPath = path;
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
                await setPLCTagInHMIStatus(childNode1, tagDataList, streamWriter, folderName, path);
            }
        }
        public static async Task EditXml(XmlNode node, List<TagDataPLC> tagDataList, List<TempInstanceVisu> tempInstList, StreamWriter streamWriter, string folderName, string path)
        {
            path = getPath(node, path);
            folderName = getFolderName(path);
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
                                            tagData.VisuFolderName = folderName;
                                            tagData.VisuPath = path;
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
                await EditXml(childNode1, tagDataList, tempInstList, streamWriter, folderName, path);
            }
        }
        private static string getFolderName(string path)
        {
            string output = "";
            if (!String.IsNullOrEmpty(path))
                output = path.Substring(path.LastIndexOf(@"/") + 1, path.Length - path.LastIndexOf(@"/") - 1);
            return output;
        }
        private static string getPath(XmlNode xmlNode, string path)
        {
            string result = path;
            if (xmlNode.Name == "Tag")
            {
                XmlAttribute xmlAttribute1 = xmlNode.Attributes["type"];
                XmlAttribute xmlAttribute2 = xmlNode.Attributes["name"];
                if (xmlAttribute1 != null && xmlAttribute2 != null)
                {
                    if (xmlAttribute1.Value == "Folder")
                    {
                        result += $@"/{xmlAttribute2.Value}";
                    }
                }
            }
            return result;
        }
    }
}
