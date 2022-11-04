using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using IgnitionHelper.Function_Containers;
using Microsoft.Win32;
using OfficeOpenXml;
using static System.Net.WebRequestMethods;
using static OfficeOpenXml.ExcelErrorValue;

namespace IgnitionHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileInfo tagsFile_g = null!;
        FileInfo xmlFile_g = null!;
        XmlDocument doc_g;
        public static StreamWriter textLogg_g = null!;
        List<TagDataPLC> tagDataABList;
        List<TempInstanceVisu> tempInstList;
        public static int PB_Progress;
        public static string expFolderPath = @"C:\Users\plradlig\Desktop\Lucid\Ignition\ExportFiles\App_Exp_files";
        public MainWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            tagDataABList = new List<TagDataPLC>();
            tempInstList = new List<TempInstanceVisu>();
            doc_g = new XmlDocument();
            expFolderPath = CheckExportFolderPath(expFolderPath, TB_ExpFolderPath);
            textLogg_g = new StreamWriter(@$"{expFolderPath}\textLogg.txt");
        }
        #region UI_EventHandlers
        private async void B_SelectTagsXLSX_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            tagsFile_g = SelectXlsxFileAndTryToUse("Select Exported from Studion5000 Tags Table (.xlsx)");
            if (tagsFile_g != null)
            {
                L_PLCTagsFilePath.Text = tagsFile_g.FullName;
                TextblockAddLine(TB_Status, $"\n Selected: {tagsFile_g.FullName}");
                try
                {
                    if (tagDataABList.Count >= 0)
                        tagDataABList.Clear();
                    tagDataABList = await ExcelOperations.LoadFromExcelFile(tagsFile_g);
                    TextblockAddLine(TB_Status, $"\n Acquired {tagDataABList.Count} tags");
                    B_GenerateXml.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    TextblockAddLine(TB_Status, $"\n{ex.Message}");
                    TextblockAddLine(TB_Status, $"\n{ex.StackTrace}");
                }
            }
            EnableButtonAndChangeCursor(sender);
        }
        private async void B_GenerateXml_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            xmlFile_g = SelectXmlFileAndTryToUse("Select file exported from Ignition (.xml)");
            if (xmlFile_g != null)
            {
                L_HMITagsFilePath.Text = xmlFile_g.FullName;
                doc_g.Load(xmlFile_g.FullName);
                if (doc_g.DocumentElement != null)
                {
                    try
                    {
                        if (tempInstList.Count >= 0)
                            tempInstList.Clear();
                        await XmlOperations.CreateTemplateAsync(doc_g.DocumentElement, tempInstList, textLogg_g, null, null);
                        TextblockAddLine(TB_Status, $"\n Number of template nodes got: {tempInstList.Count}");
                    }
                    catch (Exception ex)
                    {
                        TextblockAddLine(TB_Status, $"\n {ex.Message}");
                        TextblockAddLine(TB_Status, $"\n {ex.StackTrace}");
                    }
                    if (tempInstList.Count > 0)
                    {
                        try
                        {
                            await XmlOperations.SetPLCTagInHMIStatusAsync(doc_g.DocumentElement, tagDataABList, textLogg_g, null, null);
                            TextblockAddLine(TB_Status, $"\n Done checking! There was aleady {tagDataABList.Count(item => item.IsAdded)}/{tagDataABList.Count} instances ");
                            await XmlOperations.EditXmlAsync(doc_g.DocumentElement, tagDataABList, tempInstList, textLogg_g, null, null);
                            TextblockAddLine(TB_Status, $"\n Done editing! {tagDataABList.Count(item => item.IsAdded)}/{tagDataABList.Count} instances done");
                            string newName = expFolderPath + @"\" + xmlFile_g.Name.Replace(".xml", "_edit.xml");
                            TextblockAddLine(TB_Status, $"\n Found instances Added in XML and NOT CORRECT: {tagDataABList.Count(item => item.IsAdded && !item.IsCorrect)}");
                            doc_g.Save($"{newName}");
                            TagsOperations.GetFoldersInfo(tagDataABList, TB_Status);
                            TextblockAddLine(TB_Status, $"\n Saved file: {newName}");
                            foreach (var item in tagDataABList)
                            {
                                textLogg_g.WriteLine($"name:{item.Name};dataType:{item.DataTypePLC};folderName:{item.VisuFolderName};isAdded:{item.IsAdded}");
                            }
                            B_GenExcelTagData.IsEnabled = true;
                        }
                        catch (Exception ex)
                        {
                            TextblockAddLine(TB_Status, $"\n {ex.Message}");
                            TextblockAddLine(TB_Status, $"\n {ex.StackTrace}");
                        }
                    }
                }
            }
            EnableButtonAndChangeCursor(sender);
        }
        private async void B_GenExcelTagData_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            String filePath = expFolderPath + @"\TagDataExport.xlsx";
            var excelPackage = ExcelOperations.CreateExcelFile(filePath, textLogg_g);
            if (excelPackage == null)
            {
                TextblockAddLine(TB_Status, "\n Failed to create (.xlsx) file!");
                return;
            }
            var ws = excelPackage.Workbook.Worksheets.Add("Tag Data List");
            var range = ws.Cells["A1"].LoadFromCollection(tagDataABList, true);
            range.AutoFitColumns();
            await ExcelOperations.SaveExcelFile(excelPackage);
            TextblockAddLine(TB_Status, $"\n Created file : {filePath}");
            EnableButtonAndChangeCursor(sender);
        }
        private async void B_EditAlarmUdt_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            if (doc_g.DocumentElement != null)
            {
                try
                {
                    AlarmEditData editData = new();
                    await XmlOperations.EditUdtAlarmsXmlAsync(doc_g, doc_g.DocumentElement, editData);
                    TextblockAddLine(TB_Status, $"\n Done editing Alarms! Changed: {editData.AlarmChanged}, Passed: {editData.AlarmPassed}");
                    string newName = expFolderPath + @"\" + xmlFile_g.Name;
                    TextblockAddLine(TB_Status, $"\n Saved edited file in: {newName}");
                    doc_g.Save($"{newName}");
                }
                catch (Exception ex)
                {
                    TextblockAddLine(TB_Status, $"\n {ex.Message}");
                    TextblockAddLine(TB_Status, $"\n {ex.StackTrace}");
                }
            }
            else
                TextblockAddLine(TB_Status, $"\n Xml File is not correct!");
            EnableButtonAndChangeCursor(sender);
        }
        private async void B_EditTagUdt_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            string tagGroup = TB_TagGroup.Text;
            string valueToEdit = TB_ValueToEdit.Text;
            string value = TB_EditValue.Text;
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(tagGroup) || string.IsNullOrEmpty(valueToEdit))
            {
                TextblockAddLine(TB_Status, "\n Fill all labels!");
                return;
            }
            if (doc_g.DocumentElement != null)
            {
                try
                {
                    TagPropertyEditData editData = new();
                    textLogg_g.WriteLine($"Started editing {xmlFile_g.Name}");
                    await XmlOperations.EditUdtPropertiesXmlAync(doc_g, doc_g.DocumentElement, editData, textLogg_g, tagGroup, valueToEdit, value);
                    TextblockAddLine(TB_Status, $"\n Done editing! Properties in Groups -> Added:{editData.GroupPropAdded} Edited:{editData.GroupPropChange}, Properties in Tags ->Added:{editData.TagPropAdded} Edited:{editData.TagPropChanged}");
                    string newName = expFolderPath + @"\" + xmlFile_g.Name;
                    TextblockAddLine(TB_Status, $"\n Saved edited file in: {newName}");
                    doc_g.Save($"{newName}");
                }
                catch (Exception ex)
                {
                    TextblockAddLine(TB_Status, $"\n {ex.Message}");
                    TextblockAddLine(TB_Status, $"\n {ex.StackTrace}");
                }
            }
            else
                TextblockAddLine(TB_Status, $"\n Xml File is not correct!");
            EnableButtonAndChangeCursor(sender);
        }
        private void B_SelectExpFolder_Click(object sender, RoutedEventArgs e)
        {
            var openFolderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            {
                openFolderDialog.Description = "Select export Directory:";
            };
            var result = openFolderDialog.ShowDialog();
            if (result == true)
            {
                expFolderPath = openFolderDialog.SelectedPath + @"\";
                DirectoryInfo directory = new DirectoryInfo(expFolderPath);
                TB_ExpFolderPath.Text = expFolderPath;
                EB_ExpFolderSelected();
            }
        }
        private void B_OpenExpFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", @expFolderPath);
            }
            catch (Exception ex)
            {
                TextblockAddLine(TB_Status, $"\n {ex.Message}");
                TextblockAddLine(TB_Status, ex.StackTrace != null ? $"\n {ex.StackTrace}" : "");
            }
        }
        private void B_SelectDTFromAB_Click(object sender, RoutedEventArgs e)
        {
        }
        private void B_SelectDTFromHMI_Click(object sender, RoutedEventArgs e)
        {
        }
        private void B_SelectFileToEdit_Click(object sender, RoutedEventArgs e)
        {
            xmlFile_g = SelectXmlFileAndTryToUse("Select file exported from Ignition (.xml)");
            if (xmlFile_g != null)
            {
                L_SelectedFileToEditPath.Text = xmlFile_g.FullName;
                TextblockAddLine(TB_Status, $"\n Selected xml file to Edit : {xmlFile_g.FullName}");
                doc_g.Load(xmlFile_g.FullName);
                if (doc_g.DocumentElement != null)
                {
                    B_EditTagUdt.IsEnabled = true;
                    B_EditAlarmUdt.IsEnabled = true;
                }
            }
        }
        private void B_TextloggClose_Click(object sender, RoutedEventArgs e)
        {
            textLogg_g.WriteLine("Manually closed.");
            textLogg_g.Close();
        }

        #endregion
        #region UI Functions
        public void DisableButtonAndChangeCursor(object sender)
        {
            Cursor = Cursors.Wait;
            Button button = (Button)sender;
            button.IsEnabled = false;
        }
        public void EnableButtonAndChangeCursor(object sender)
        {
            Cursor = Cursors.Arrow;
            Button button = (Button)sender;
            button.IsEnabled = true;
        }
        public void EB_ExpFolderSelected()
        {
            B_OpenExpFolder.IsEnabled = true;
            B_SelectTagsXLSX.IsEnabled = true;
            B_SelectDTFromAB.IsEnabled = true;
        }
        public string CheckExportFolderPath(string expFolderPath, TextBlock tb)
        {
            string toReturn = expFolderPath;
            if (Directory.Exists(expFolderPath))
            {
                expFolderPath = Tools.OverridePathWithDateTimeSubfolder(expFolderPath);
                tb.Text = expFolderPath;
                toReturn = expFolderPath;
                EB_ExpFolderSelected();
            }
            else
            {
                var openFolderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                {
                    openFolderDialog.Description = "Select export Directory:";
                };
                var result = openFolderDialog.ShowDialog();
                if (result == true)
                {
                    expFolderPath = openFolderDialog.SelectedPath + @"\";
                    expFolderPath = Tools.OverridePathWithDateTimeSubfolder(expFolderPath);
                    toReturn = expFolderPath;
                    tb.Text = expFolderPath;
                    EB_ExpFolderSelected();
                }
            }
            return toReturn;
        }
        public static void TextblockAddLine(TextBlock tb, string text)
        {
            if (!string.IsNullOrEmpty(text))
                tb.Inlines.InsertBefore(tb.Inlines.FirstInline, new Run(text));
        }
        public FileInfo SelectXmlFileAndTryToUse(string title)
        {
            OpenFileDialog openFileDialog1 = new()
            {
                InitialDirectory = @"c:\Users\localadm\Desktop",
                Title = title,
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
                FileInfo xmlFile = new FileInfo(openFileDialog1.FileName);
                if (xmlFile.Exists && !Tools.IsFileLocked(xmlFile.FullName))
                {
                    return xmlFile;
                }
                TextblockAddLine(TB_Status, "\n File not exist or in use!");
                return null;
            }
            else
            {
                TextblockAddLine(TB_Status, "\n File not selected!");
                return null;
            }
        }
        public FileInfo SelectXlsxFileAndTryToUse(string title)
        {
            OpenFileDialog openFileDialog1 = new()
            {
                InitialDirectory = @"c:\Users\localadm\Desktop",
                Title = title,
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
                FileInfo xmlFile = new FileInfo(openFileDialog1.FileName);
                if (xmlFile.Exists && !Tools.IsFileLocked(xmlFile.FullName))
                {
                    return xmlFile;
                }
                TextblockAddLine(TB_Status, "\n File not exist or in use!");
                return null;
            }
            else
            {
                TextblockAddLine(TB_Status, "\n File not selected!");
                return null;
            }
        }
        #endregion
    }
}
