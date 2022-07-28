using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    public class DataTypeTag
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public DataTypeTag(string name, string dataType)
        {
            Name = name;
            DataType = dataType;
        }
    }
}
