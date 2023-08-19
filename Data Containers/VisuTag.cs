using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper.Data_Containers
{
    public class VisuTag
    {
        public string? UdtName { get; set; }
        public string? Name { get; set; }
        public string? DemandedPath { get; set; }
        public string? DefinedPath { get; set; }
        public bool? ArePathsSame 
        { 
            get
            {
                PrepareDefinedPath();
                return DemandedPath == DefinedPath;
            }
            set { }
        }
        public VisuTag(string? udtName, string? name, string? demandedPath, string? definedPath)
        {
            UdtName = udtName;
            Name = name;
            DemandedPath = demandedPath;
            DefinedPath = definedPath;
        }
        private void PrepareDefinedPath()
        {
            int iof = (int)DefinedPath.IndexOf('.');
            if (iof == -1)
                return;
            int lenght = (int)DefinedPath.Length;
            DefinedPath = DefinedPath.Substring(iof, lenght - iof);
            lenght = (int)DefinedPath.Length;
            iof = (int)DefinedPath.IndexOf('{');
            if (iof == -1)
                return;
            iof = (iof - 1 > 0) ? iof - 1 : iof;
            DefinedPath = DefinedPath.Substring(0, iof);
            DefinedPath = DefinedPath.Replace('.', '/').Replace('[', '/').Replace("]",string.Empty);
        }
        public override string ToString() => $"UDT: {UdtName}, Tag Name:{Name}, Demanded Path: {DemandedPath}, Defined Path: {DefinedPath}, Are Same: {ArePathsSame}"; 
    }
}
