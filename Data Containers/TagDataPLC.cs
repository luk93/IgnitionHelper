namespace IgnitionHelper.Data_Containers
{
    //Tag Data Allan Bradley PLC
    public class TagDataPLC
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? DataTypePLC { get; set; }
        public string DataTypeVisu { get; set; }
        public bool IsAdded { get; set; }
        public bool IsCorrect { get; set; }
        public bool Deleted { get; set; }
        public bool ToDelete { get; set; }
        public string VisuFolderName { get; set; }
        public string? VisuPath { get; set; }

        //Constructor used to DeepClone
        public TagDataPLC()
        {
        }

        public TagDataPLC(int id)
        {
            Id = id;
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

        public TagDataPLC(string name, string? dataTypePLC, string dataTypeVisu, int id)
        {
            Id = id;
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
