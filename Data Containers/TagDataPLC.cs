using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    //Tag Data Allan Bradley PLC
    public class TagDataPLC
    {
        public string Name { get; set; }
        public string DataTypePLC { get; set; }
        public string DataTypeVisu { get; set; }
        public bool IsAdded { get; set; }
        public bool IsCorrect { get; set; }
        public bool Deleted { get; set; }
        public bool ToDelete { get; set; }
        public string VisuFolderName { get; set; }
        public string? VisuPath { get; set; }
        public TagDataPLC()
        {
            Name = string.Empty;
            DataTypePLC = string.Empty;
            DataTypeVisu = string.Empty;
            IsAdded = false;
            IsCorrect = false;
            VisuFolderName = string.Empty;
            VisuPath = string.Empty;
            Deleted = false;
            ToDelete = false;
        }
        public TagDataPLC(string name, string dataTypePLC, string dataTypeVisu)
        {
            Name = name;
            DataTypePLC = dataTypePLC;
            DataTypeVisu = dataTypeVisu;
            IsAdded = false;
            IsCorrect = false;
            VisuFolderName = string.Empty;
            VisuPath = string.Empty;
            Deleted = false;
            ToDelete = false;
        }
    }
}
