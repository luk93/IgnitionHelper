using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using static OfficeOpenXml.ExcelErrorValue;

namespace IgnitionHelper
{
    public static class XmlOperations
    {
        private static void CreateTemplate(XmlNode node, List<TempInstanceVisu> output, StreamWriter streamWriter, string folderName, string path)
        {
            if (output == null)
            {
                output = new List<TempInstanceVisu>();
            }
            path = GetPath(node, path);
            folderName = GetFolderName(path);
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
                                XmlAttribute? xmlAttribute = childNode2.Attributes["name"];
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
                CreateTemplate(childNode1, output, streamWriter, folderName, path);
            }
        }
        private static void SetPLCTagInHMIStatus(XmlNode node, List<TagDataPLC> tagDataList, StreamWriter streamWriter, string folderName, string path)
        {
            path = GetPath(node, path);
            folderName = GetFolderName(path);

            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                if (childNode1.Name == "Tag")
                {
                    XmlAttribute? xmlAttribute1 = childNode1.Attributes["type"];
                    XmlAttribute? xmlAttribute2 = childNode1.Attributes["name"];
                    if (xmlAttribute1 != null && xmlAttribute2 != null)
                    {
                        if (!String.IsNullOrEmpty(folderName))
                        {
                            if (xmlAttribute1.Value == "UdtInstance")
                            {
                                //Search for Instance Of Data Block in Xml by the Name got from Excel
                                TagDataPLC? tagData = tagDataList.Find(item => item.Name == xmlAttribute2.Value);
                                if (tagData != null)
                                {
                                    //Search for Instance Of Data Block Data Type in Xml by the Data Type got from Excel
                                    foreach (XmlNode childNode2 in childNode1.ChildNodes)
                                    {
                                        if (childNode2.Name == "Property")
                                        {
                                            XmlAttribute? xmlAttribute = childNode2.Attributes["name"];
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
                SetPLCTagInHMIStatus(childNode1, tagDataList, streamWriter, folderName, path);
            }
        }
        private static void EditXml(XmlNode node, List<TagDataPLC> tagDataList, List<TempInstanceVisu> tempInstList, StreamWriter streamWriter, string folderName, string path)
        {
            path = GetPath(node, path);
            folderName = GetFolderName(path);
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                //Find correct folder, where same instances of data type are stored 
                if (childNode1.Name == "Tag")
                {
                    XmlAttribute? xmlAttribute1 = childNode1.Attributes["type"];
                    XmlAttribute? xmlAttribute2 = childNode1.Attributes["name"];
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
                                        TempInstanceVisu? tempInst = tempInstList.Find(item => (StringExt.Contains(item.Name, tagData.DataTypePLC, StringComparison.OrdinalIgnoreCase) ||
                                                                                            StringExt.Contains(tagData.DataTypePLC, item.Name, StringComparison.OrdinalIgnoreCase)) && item.FolderName == folderName);
                                        if (tempInst != null)
                                        {
                                            XmlNode? newNode = tempInst.Node.CloneNode(true);
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
                EditXml(childNode1, tagDataList, tempInstList, streamWriter, folderName, path);
            }
        }
        private static void EditUdtPropertiesXml(XmlDocument doc, XmlNode node, TagPropertyEditData editData, StreamWriter streamWriter, string tagGroup, string valueToEdit, string value)
        {
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                if (childNode1.Name == "Tag")
                {
                    XmlAttribute? tagType = childNode1.Attributes["type"];
                    XmlAttribute? tagName = childNode1.Attributes["name"];
                    if (tagType != null && tagName != null)
                    {
                        //Find correct group of Tags in Each Tag (for example CB)
                        if ((tagType.Value == "Folder" || tagType.Value == "AtomicTag") && tagName.Value == tagGroup)
                        {
                            bool groupPropFound = false;
                            foreach (XmlNode childNode2 in childNode1.ChildNodes)
                            {
                                //Change to valueToEdit to value for group of Tags
                                if (childNode2.Name == "Property")
                                {
                                    XmlAttribute? valueToEditAtt = childNode2.Attributes["name"];
                                    if (valueToEditAtt != null)
                                    {
                                        if (valueToEditAtt.Value == valueToEdit)
                                        {
                                            groupPropFound = true;
                                            editData.GroupPropChange++;
                                            childNode2.InnerText = value;
                                            streamWriter.WriteLineAsync($"Changed property in group: {tagGroup}, ValueToEdit:{valueToEdit}, EditValue: {value}");
                                        }
                                    }
                                }
                                if (childNode2.Name == "Tags")
                                {
                                    foreach (XmlNode childNode3 in childNode2.ChildNodes)
                                    {
                                        if (childNode3.Name == "Tag")
                                        {
                                            //Edit All tags from this group 
                                            string childTagNameValue = "";
                                            var childTagName = childNode3.Attributes["name"];
                                            if (childTagName != null)
                                            {
                                                childTagNameValue = childTagName.Value;
                                            }
                                            bool tagPropFound = false;
                                            foreach (XmlNode childNode4 in childNode3.ChildNodes)
                                            {
                                                if (childNode4.Name == "Property")
                                                {
                                                    XmlAttribute? valueToEditAtt = childNode4.Attributes["name"];
                                                    if (valueToEditAtt != null)
                                                    {
                                                        if (valueToEditAtt.Value == valueToEdit)
                                                        {
                                                            tagPropFound = true;
                                                            editData.TagPropChanged++;
                                                            childNode4.InnerText = value;
                                                            streamWriter.WriteLineAsync($"Changed property in Tag: {childTagNameValue}, ValueToEdit:{valueToEdit}, EditValue: {value}");
                                                        }
                                                    }
                                                }
                                            }
                                            if (!tagPropFound)
                                            {
                                                //Create new node with attribute
                                                XmlNode newNode = doc.CreateNode(XmlNodeType.Element, "Property", "");
                                                XmlAttribute xmlAttribute = doc.CreateAttribute("name");
                                                xmlAttribute.Value = valueToEdit;
                                                newNode.Attributes.SetNamedItem(xmlAttribute);
                                                newNode.InnerText = value;
                                                editData.TagPropAdded++;
                                                childNode3.InsertAfter(newNode, childNode3.LastChild);
                                                streamWriter.WriteLineAsync($"Added property in Tag: {childTagNameValue}, ValueToEdit:{valueToEdit}, EditValue: {value}");
                                            }
                                        }
                                    }
                                }
                            }
                            if (!groupPropFound)
                            {
                                //Create new node with attribute
                                XmlNode newNode = doc.CreateNode(XmlNodeType.Element, "Property", "");
                                XmlAttribute xmlAttribute = doc.CreateAttribute("name");
                                xmlAttribute.Value = valueToEdit;
                                newNode.Attributes.SetNamedItem(xmlAttribute);
                                newNode.InnerText = value;
                                editData.GroupPropAdded++;
                                childNode1.InsertAfter(newNode, childNode1.LastChild);
                                streamWriter.WriteLineAsync($"Added property in Group: {tagGroup}, ValueToEdit:{valueToEdit}, EditValue: {value}");
                            }
                        }
                    }
                }
            }
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                EditUdtPropertiesXml(doc, childNode1, editData, streamWriter, tagGroup, valueToEdit, value);
            }
        }
        private static void EditUdtAlarmsXml(XmlDocument doc, XmlNode node, AlarmEditData editData)
        {
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                if (childNode1.Name == "CompoundProperty")
                {
                    XmlAttribute? tagName = childNode1.Attributes["name"];
                    if (tagName != null)
                    {
                        //Find Alarm
                        if (tagName.Value == "alarms")
                        {
                            foreach (XmlNode childNode2 in childNode1.ChildNodes)
                            {
                                //Operate on PropertySet Node
                                if (childNode2.Name == "PropertySet")
                                {
                                    //Look for "displayPath
                                    bool labelPathFound = false;
                                    foreach (XmlNode childNode3 in childNode2.ChildNodes)
                                    {
                                        tagName = null;
                                        tagName = childNode3.Attributes["name"];
                                        if (tagName != null)
                                        {
                                            if (tagName.Value == "label")
                                            {
                                                labelPathFound = true;
                                                break;
                                            }

                                        }
                                    }
                                    //If no displayPathFound do needed operations
                                    if (!labelPathFound)
                                    {
                                        foreach (XmlNode childNode3 in childNode2.ChildNodes)
                                        {
                                            tagName = null;
                                            tagName = childNode3.Attributes["name"];
                                            if (tagName != null)
                                            {
                                                if (tagName.Value == "displayPath")
                                                {
                                                    //Copy "displayPath" node
                                                    XmlNode newNode = childNode3.CloneNode(true);
                                                    //Edit "displayPath node
                                                    string cutName = "{TagName}";
                                                    string textToCut = childNode3.InnerText;
                                                    int cutPosition = textToCut.IndexOf(cutName) + cutName.Length;
                                                    childNode3.InnerText = textToCut[..cutPosition];
                                                    //Edit new Node "label"
                                                    newNode.Attributes["name"].Value = "label";
                                                    newNode.InnerText = textToCut[cutPosition..];
                                                    //Insert new node
                                                    childNode2.InsertAfter(newNode, childNode2.LastChild);
                                                    editData.AlarmChanged++;
                                                    break;
                                                }

                                            }
                                        }
                                    }
                                    else
                                    {
                                        editData.AlarmPassed++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                EditUdtAlarmsXml(doc, childNode1, editData);
            }
        }

        public static Task CreateTemplateAsync(XmlNode node, List<TempInstanceVisu> output, StreamWriter streamWriter, string folderName, string path)
        {
            return Task.Run(() => CreateTemplate(node, output,streamWriter,folderName, path));
        }
        public static Task SetPLCTagInHMIStatusAsync(XmlNode node, List<TagDataPLC> tagDataList, StreamWriter streamWriter, string folderName, string path)
        {
            return Task.Run(() => SetPLCTagInHMIStatus( node, tagDataList,  streamWriter,  folderName,  path));
        }
        public static Task EditXmlAsync(XmlNode node, List<TagDataPLC> tagDataList, List<TempInstanceVisu> tempInstList, StreamWriter streamWriter, string folderName, string path)
        {
            return Task.Run(() => EditXml( node, tagDataList, tempInstList, streamWriter, folderName, path));
        }
        public static Task EditUdtPropertiesXmlAync(XmlDocument doc, XmlNode node, TagPropertyEditData editData, StreamWriter streamWriter, string tagGroup, string valueToEdit, string value)
        {
            return Task.Run(() => EditUdtPropertiesXml(doc, node, editData, streamWriter, tagGroup, valueToEdit, value));
        }
        public static Task EditUdtAlarmsXmlAsync(XmlDocument doc, XmlNode node, AlarmEditData editData)
        {
            return Task.Run(() => EditUdtAlarmsXml(doc, node, editData));
        }

        private static string GetFolderName(string path)
        {
            string output = "";
            if (!String.IsNullOrEmpty(path))
                output = path.Substring(path.LastIndexOf(@"/") + 1, path.Length - path.LastIndexOf(@"/") - 1);
            return output;
        }
        private static string GetPath(XmlNode xmlNode, string path)
        {
            string result = path;
            if (xmlNode.Name == "Tag")
            {
                XmlAttribute? xmlAttribute1 = xmlNode.Attributes["type"];
                XmlAttribute? xmlAttribute2 = xmlNode.Attributes["name"];
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
