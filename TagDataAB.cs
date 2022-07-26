using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    //Tag Data Allan Bradley
    public class TagDataAB
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsAdded { get; set; }
        public string FolderName { get; set; }
        public TagDataAB()
        {
            Name = "";
            DataType = "";
            IsAdded = false;
            FolderName = "";
        }
        public TagDataAB(string name, string dataType)
        {
            Name = name;
            DataType = dataType;
            IsAdded = false;
            FolderName = "";
        }
    }
}
