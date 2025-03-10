﻿using IgnitionHelper.Extensions;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IgnitionHelper.Data_Containers;
using System.Security.RightsManagement;

namespace IgnitionHelper
{
    public static class ExcelOperations
    {
        public static async Task<List<TagDataPLC>> LoadTagDataListFromExcelFile(FileInfo file, string inludedDTname)
        {
            List<TagDataPLC> output = new();
            var package = new ExcelPackage(file);
            await package.LoadAsync(file);
            var ws = package.Workbook.Worksheets[0];
            int row = 1;
            int col = 1;
            if (ws != null)
            {
                while (!string.IsNullOrWhiteSpace(ws.Cells[row, col].Value?.ToString()))
                {
                    if (ws.Cells[row, col].Value.ToString() == "TAG")
                    {
                        if (!string.IsNullOrWhiteSpace(ws.Cells[row, col + 4].Value?.ToString()))
                        {
                            if (ws.Cells[row, col + 4].Value.ToString().ContainsMany(inludedDTname, StringComparison.OrdinalIgnoreCase))
                            {
                                var currentId = output.Count;
                                TagDataPLC newObj = new(currentId)
                                {
                                    DataTypePLC = (ws.Cells[row, col + 4].Value.ToString()),
                                    Name = (ws.Cells[row, col + 2].Value.ToString())
                                };
                                output.Add(newObj);
                            }
                        }
                    }
                    row++;
                }
            }
            return output;
        }
        public static async Task<Dictionary<int,string>?> LoadDefLocFromExcelFile(FileInfo file)
        {
            Dictionary<int, string> output = new();
            var package = new ExcelPackage(file);
            await package.LoadAsync(file);
            var ws = package.Workbook.Worksheets[0];
            int row = 2;
            int col = 1;
            if (ws == null)
                return null;
            
            while (!string.IsNullOrWhiteSpace(ws.Cells[row, col].Value?.ToString()))
            { 
                if (string.IsNullOrWhiteSpace(ws.Cells[row, col + 1].Value?.ToString()))
                {
                    row++;
                    continue;
                }
                if (int.TryParse(ws.Cells[row, col].Value?.ToString(), out int dicKey))
                {
                    string dicValue = ws.Cells[row, col + 1].Value?.ToString();
                    output.Add(dicKey, dicValue);
                }
                row++;
            }
            return output;
        }
        public static ExcelPackage? CreateExcelFile(string path, StreamWriter streamWriter)
        {
            if (streamWriter == null) throw new ArgumentNullException(nameof(streamWriter));
            var file = new FileInfo(path);
            if (file.Exists)
            {
                try
                {
                    file.Delete();
                }
                catch
                {
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
