using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
    
namespace IgnitionHelper.Extensions
{
    static class TagDataListExt
    {
        public static void GetFoldersInfo(this List<TagDataPLC> tagDataList, TextBlock textBlock)
        {
            List<string> folderNames = tagDataList.Select(s => s.VisuFolderName).Distinct().ToList();

            foreach (string folderName in folderNames)
            {
                var folderCount = tagDataList.Count(tagData => tagData.VisuFolderName == folderName && tagData.IsAdded);
                textBlock.AddLine($"\n Folder name: {folderName} count: {folderCount}");
            }
        }
        public static void GetDataFromObservableCollection(this List<TagDataPLC> tagDataList, ObservableCollection<TagDataPLC> tagDataObsCol)
        {
            foreach(TagDataPLC tagData in tagDataObsCol) 
            {
                var tag = tagDataList.FirstOrDefault(x => x.Name == tagData.Name);
                if(tag != null)
                    tag.ToDelete = tagData.ToDelete;
            }
        }
        public static List<TagDataPLC>? DeepCopy(this List<TagDataPLC> tagDataList)
        {
            var serializedTagDataList = JsonConvert.SerializeObject(tagDataList);
            return JsonConvert.DeserializeObject<List<TagDataPLC>>(serializedTagDataList);
        }
    }
}
