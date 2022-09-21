using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    public class AlarmEditData
    {
        public int AlarmChanged { get; set; }
        public int AlarmPassed { get; set; }
        public AlarmEditData()
        {
            AlarmChanged = 0;
            AlarmPassed = 0;
        }
    }
}
