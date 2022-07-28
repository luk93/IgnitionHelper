using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    public class DataType
    {
        public string Name { get; set; }
        public List<DataTypeTag> DTTagList { get; set; }
        public DataType(string name, List<DataTypeTag> dtTagList)
        {
            Name = name;
            DTTagList = dtTagList;
        }
        public DataType(string name)
        {
            Name = name;
            DTTagList = new List<DataTypeTag>() ;
        }
    }
}
