using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Microsoft.Win32;
using OfficeOpenXml;

namespace IgnitionHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileInfo tagsFile_g;
        FileInfo xmlFile_g;
        XmlDocument doc_g;
        public static StreamWriter textLogg_g;
        List<TagDataPLC> tagDataABList;
        List<TempInstanceVisu> tempInstList;
        public static int PB_Progress;
        public MainWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            tagDataABList = new List<TagDataPLC>();
            tempInstList = new List<TempInstanceVisu>();
            doc_g = new XmlDocument();
        }
        private async void B_SelectTagsXLSX_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new()
            {
                InitialDirectory = @"c:\Users\localadm\Desktop",
                Title = "Select Exported from Studion5000 Tags Table (.xlsx)",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "xlsx",
                Filter = "Excel file (*.xlsx)|*.xlsx",
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                ShowReadOnly = true,
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                tagsFile_g = new FileInfo(openFileDialog1.FileName);
                if (tagsFile_g.Exists && !IsFileLocked(tagsFile_g.FullName))
                {
                    L_TagsCsvPath.Content = $"{tagsFile_g.FullName}";
                    TB_Status.Text += $"\n Selected: {tagsFile_g.FullName}";
                    textLogg_g = new StreamWriter($"{tagsFile_g.FullName.Substring(0, tagsFile_g.FullName.Length - 5)}_textLogg.txt");
                    try
                    {
                        tagDataABList = await ExcelOperations.loadFromExcelFile(tagsFile_g);
                        TB_Status.Text += $"\n Acquired {tagDataABList.Count.ToString()} tags";
                    }
                    catch (Exception ex)
                    {
                        TB_Status.Text = ex.Message;
                        TB_Status.Text += ex.StackTrace;
                    }

                }
                else
                {
                    TB_Status.Text = "File not exist or in use!";
                }
            }
        }
        private async void B_GenerateXml_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new()
            {
                InitialDirectory = @"c:\Users\localadm\Desktop",
                Title = "Select file exported from Ignition (.xml)",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "xml",
                Filter = "Xml file (*.xml)|*.xml",
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                ShowReadOnly = true,
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                xmlFile_g = new FileInfo(openFileDialog1.FileName);
                if (xmlFile_g.Exists && !IsFileLocked(xmlFile_g.FullName))
                {
                    doc_g.Load(xmlFile_g.FullName);
                    try
                    {
                        await XmlOperations.CreateTemplate(doc_g.DocumentElement, tempInstList, textLogg_g, null);
                        TB_Status.Text += $"\n Number of template nodes got: {tempInstList.Count}";
                    }
                    catch (Exception ex)
                    {
                        TB_Status.Text = $"\n {ex.Message}";
                        TB_Status.Text += $"\n {ex.StackTrace}";
                    }
                    if (tempInstList.Count > 0)
                    {
                        try
                        {
                            await XmlOperations.CheckXml(doc_g.DocumentElement, tagDataABList, textLogg_g, null);
                            TB_Status.Text += $"\n Done checking! There was aleady {tagDataABList.Count(item => item.IsAdded)}/{tagDataABList.Count} instances ";
                            await XmlOperations.EditXml(doc_g.DocumentElement, tagDataABList, tempInstList, textLogg_g, null);
                            TB_Status.Text += $"\n Done editing! {tagDataABList.Count(item => item.IsAdded)}/{tagDataABList.Count} instances done";
                            string newName = xmlFile_g.FullName.Replace(".xml", "_edit.xml");
                            TB_Status.Text += $"\n Found instances Added in XML and NOT CORRECT: {tagDataABList.Count(item => item.IsAdded && !item.IsCorrect)}";
                            doc_g.Save($"{newName}");
                            GetFoldersInfo(tagDataABList, TB_Status);
                            TB_Status.Text += $"\n Saved file: {newName}";
                            foreach (var item in tagDataABList)
                            {
                                textLogg_g.WriteLine($"name:{item.Name};dataType:{item.DataTypePLC};folderName:{item.FolderName};isAdded:{item.IsAdded}");
                            }
                        }
                        catch (Exception ex)
                        {
                            TB_Status.Text = $"\n {ex.Message}";
                            TB_Status.Text += $"\n {ex.StackTrace}";
                        }
                    }
                    else
                    {
                        TB_Status.Text = "\n No templates found in XML";
                    }
                }
                else
                {
                    TB_Status.Text = "\n File not exist or in use!";
                }
            }
            textLogg_g.Close();
        }
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                var stream = File.OpenRead(filePath);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
        public static void GetFoldersInfo(List<TagDataPLC> tagDataList, TextBlock textBlock)
        {
            List<string> folderNames = tagDataList.Select(s => s.FolderName).Distinct().ToList();

            foreach (string folderName in folderNames)
            {
                var folderCount = tagDataList.Count(tagData => tagData.FolderName == folderName && tagData.IsAdded);
                textBlock.Text += $"\n Folder name: {folderName} count: {folderCount}";
            }
        }
        private async void B_GenExcelTagData_Click(object sender, RoutedEventArgs e)
        {
            String filePath = tagsFile_g.FullName.Substring(0, tagsFile_g.FullName.LastIndexOf(@"\")) + @"\TagDataExport.xlsx";
            var excelPackage = ExcelOperations.CreateExcelFile(filePath, textLogg_g);
            if (excelPackage == null)
            {
                TB_Status.Text += "Failed to create (.xlsx) file!";
                return;
            }
            var ws = excelPackage.Workbook.Worksheets.Add("Tag Data List");
            var range = ws.Cells["A1"].LoadFromCollection(tagDataABList, true);
            range.AutoFitColumns();
            await ExcelOperations.SaveExcelFile(excelPackage);
            TB_Status.Text += $"\n Created file : {filePath}";
        }

        private void B_GetDTFromIgni_Click(object sender, RoutedEventArgs e)
        {

        }

        private void B_GetDTFromAB_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
