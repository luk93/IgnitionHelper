using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    public class TagEditData
    {
        public int GroupChange { get; set; }
        public int TagChange { get; set; }
        public TagEditData()
        {
            GroupChange = 0;
            TagChange = 0;
        }
    }
}
