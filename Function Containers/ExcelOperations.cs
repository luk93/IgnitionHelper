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
        public static async Task<List<TagDataPLC>> loadFromExcelFile(FileInfo file)
        {
            List<TagDataPLC> output = new List<TagDataPLC>();
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
                            TagDataPLC newObj = new TagDataPLC();
                            newObj.DataTypePLC = (ws.Cells[row, col + 4].Value.ToString());
                            newObj.Name = (ws.Cells[row, col + 2].Value.ToString());
                            output.Add(newObj);
                        }
                    }
                }
                row++;
            }
            return output;
        }
        public static ExcelPackage CreateExcelFile(string path, StreamWriter streamWriter)
        {
            var file = new FileInfo(path);
            if (file.Exists)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception e)
                {
                    streamWriter.WriteLine($"Error trying to create excel file: {e.Message}");
                    return null;
                }
            }
            return new ExcelPackage(file);
        }
        public static async Task SaveExcelFile(ExcelPackage excelPackage)
        {
            await excelPackage.SaveAsync();
            excelPackage.Dispose();
        }
    }
   
}
