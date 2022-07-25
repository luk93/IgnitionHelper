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
        FileInfo csvFile_g;
        FileInfo xmlFile_g;
        XmlDocument doc_g;
        public static StreamWriter textLogg_g;
        List<TagData> tagDataList;
        List<TemplateNode> tempNodeList;
        public static int PB_Progress;
        public MainWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            tagDataList = new List<TagData>();
            tempNodeList = new List<TemplateNode>();
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
                csvFile_g = new FileInfo(openFileDialog1.FileName);
                if (csvFile_g.Exists && !IsFileLocked(csvFile_g.FullName))
                {
                    L_TagsCsvPath.Content = $"{csvFile_g.FullName}";
                    TB_Status.Text += $"\n Selected: {csvFile_g.FullName}";
                    textLogg_g = new StreamWriter($"{csvFile_g.FullName}_textLogg.txt");
                    try
                    {
                        tagDataList = await ExcelOperations.loadFromExcelFile(csvFile_g);
                        TB_Status.Text += $"\n Acquired {tagDataList.Count.ToString()} tags";
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
                        await XmlOperations.CreateTemplate(doc_g.DocumentElement, tempNodeList, textLogg_g, null);
                        TB_Status.Text += $"\n Number of template nodes got: {tempNodeList.Count}";
                    }
                    catch (Exception ex)
                    {
                        TB_Status.Text = $"\n {ex.Message}";
                        TB_Status.Text += $"\n {ex.StackTrace}";
                    }
                    if (tempNodeList.Count > 0)
                    {
                        try
                        {
                            await XmlOperations.CheckXml(doc_g.DocumentElement, tagDataList, tempNodeList, textLogg_g,null);
                            TB_Status.Text += $"\n Done checking! There was aleady {tagDataList.Count(item => item.IsAdded)}/{tagDataList.Count} instances ";
                            await XmlOperations.EditXml(doc_g.DocumentElement, tagDataList, tempNodeList, textLogg_g, null);
                            TB_Status.Text += $"\n Done editing! {tagDataList.Count(item => item.IsAdded)}/{tagDataList.Count} instances done";
                            string newName = xmlFile_g.FullName.Replace(".xml", "_edit.xml");
                            doc_g.Save($"{newName}");
                            GetFoldersInfo(tagDataList, TB_Status);
                            TB_Status.Text += $"\n Saved file: {newName}";
                            foreach (var item in tagDataList)
                            {
                                textLogg_g.WriteLine($"name:{item.Name};dataType:{item.DataType};folderName:{item.FolderName};isAdded:{item.IsAdded}");
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
        public static void GetFoldersInfo(List<TagData> tagDataList, TextBlock textBlock)
        {
            List<string> folderNames = tagDataList.Select(s => s.FolderName).Distinct().ToList();

            foreach (string folderName in folderNames)
            {
                var folderCount = tagDataList.Count(tagData => tagData.FolderName == folderName && tagData.IsAdded);
                textBlock.Text += $"\n Folder name: {folderName} count: {folderCount}";
            }
        }
    }
}
