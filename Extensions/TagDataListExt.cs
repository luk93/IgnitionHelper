using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
