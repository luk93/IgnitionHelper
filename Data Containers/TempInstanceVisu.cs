using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IgnitionHelper
{
    //Template Instance of Data Type Ignition Visu
    public class TempInstanceVisu
    {
        public string Name { get; set; }
        public string FolderName { get; set; }
        public XmlNode Node { get; set; }
        public TempInstanceVisu(string name, XmlNode node, string folderName)
        {
            Name = name;
            Node = node;
            FolderName = folderName;
        }
    }
}
