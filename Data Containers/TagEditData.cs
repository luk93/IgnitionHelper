using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    public class TagPropertyEditData
    {
        public int GroupPropChange { get; set; }
        public int GroupPropAdded { get; set; }
        public int TagPropChanged { get; set; }
        public int TagPropAdded { get; set; }
        public TagPropertyEditData()
        {
            GroupPropChange = 0;
            TagPropChanged = 0;
            GroupPropAdded = 0;
            TagPropAdded = 0;
        }
    }
}
