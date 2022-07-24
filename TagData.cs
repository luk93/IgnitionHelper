using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    public class TagData
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsAdded { get; set; }
        public TagData()
        {
            Name = "";
            DataType = "";
            IsAdded = false;
        }
        public TagData(string name, string dataType)
        {
            Name = name;
            DataType = dataType;
            IsAdded = false;
        }
    }
}
