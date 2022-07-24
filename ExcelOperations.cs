using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    public static class ExcelOperations
    {
        public static async Task<List<TagData>> loadFromExcelFile(FileInfo file)
        {
            List<TagData> output = new List<TagData>();
            var package = new ExcelPackage(file);
            await package.LoadAsync(file);
            var ws = package.Workbook.Worksheets[0];
            int row = 1;
            int col = 1;
            while (!string.IsNullOrWhiteSpace(ws.Cells[row, col].Value?.ToString()))
            {
                if (ws.Cells[row, col].Value.ToString() == "TAG")
                {
                    if (!string.IsNullOrWhiteSpace(ws.Cells[row, col + 4].Value?.ToString()))
                    {
                        if (ws.Cells[row, col + 4].Value.ToString().Contains("ud_"))
                        {
                            TagData newObj = new TagData();
                            newObj.DataType = (ws.Cells[row, col + 4].Value.ToString().Substring(3, ws.Cells[row, col + 4].Value.ToString().Length-3));
                            newObj.Name = (ws.Cells[row, col + 2].Value.ToString());
                            output.Add(newObj);
                        }
                    }
                }
                row++;
            }
            return output;
        }
    }
}
