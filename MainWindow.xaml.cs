using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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
using IgnitionHelper.Extensions;
using IgnitionHelper.Function_Containers;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using static System.Net.WebRequestMethods;
using static OfficeOpenXml.ExcelErrorValue;

namespace IgnitionHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileInfo? tagsFile_g = null!;
        FileInfo? xmlFile_g = null!;
        FileInfo? jsonFile_g = null!;
        XmlDocument doc_g;
        JObject? json_g;
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
            doc_g = new();
            json_g = null;
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
                TB_Status.AddLine($"\n Selected: {tagsFile_g.FullName}");
                try
                {
                    if (tagDataABList.Count >= 0)
                        tagDataABList.Clear();
                    tagDataABList = await ExcelOperations.LoadFromExcelFile(tagsFile_g);
                    TB_Status.AddLine($"\n Acquired {tagDataABList.Count} tags");
                    B_GenerateXml.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    TB_Status.AddLine($"\n{ex.Message}");
                    TB_Status.AddLine($"\n{ex.StackTrace}");
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
                        TB_Status.AddLine($"\n Number of template nodes got: {tempInstList.Count}");
                    }
                    catch (Exception ex)
                    {
                        TB_Status.AddLine($"\n {ex.Message}");
                        TB_Status.AddLine($"\n {ex.StackTrace}");
                    }
                    if (tempInstList.Count > 0)
                    {
                        try
                        {
                            await XmlOperations.SetPLCTagInHMIStatusAsync(doc_g.DocumentElement, tagDataABList, textLogg_g, null, null);
                            TB_Status.AddLine($"\n Done checking! There was aleady {tagDataABList.Count(item => item.IsAdded)}/{tagDataABList.Count} instances ");
                            await XmlOperations.EditXmlAsync(doc_g.DocumentElement, tagDataABList, tempInstList, textLogg_g, null, null);
                            TB_Status.AddLine($"\n Done editing! {tagDataABList.Count(item => item.IsAdded)}/{tagDataABList.Count} instances done");
                            string newName = expFolderPath + @"\" + xmlFile_g.Name.Replace(".xml", "_edit.xml");
                            TB_Status.AddLine($"\n Found instances Added in XML and NOT CORRECT: {tagDataABList.Count(item => item.IsAdded && !item.IsCorrect)}");
                            doc_g.Save($"{newName}");
                            TagsOperations.GetFoldersInfo(tagDataABList, TB_Status);
                            TB_Status.AddLine($"\n Saved file: {newName}");
                            foreach (var item in tagDataABList)
                            {
                                textLogg_g.WriteLine($"name:{item.Name};dataType:{item.DataTypePLC};folderName:{item.VisuFolderName};isAdded:{item.IsAdded}");
                            }
                            B_GenExcelTagData.IsEnabled = true;
                        }
                        catch (Exception ex)
                        {
                            TB_Status.AddLine($"\n {ex.Message}");
                            TB_Status.AddLine($"\n {ex.StackTrace}");
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
                TB_Status.AddLine("\n Failed to create (.xlsx) file!");
                return;
            }
            var ws = excelPackage.Workbook.Worksheets.Add("Tag Data List");
            var range = ws.Cells["A1"].LoadFromCollection(tagDataABList, true);
            range.AutoFitColumns();
            await ExcelOperations.SaveExcelFile(excelPackage);
            TB_Status.AddLine($"\n Created file : {filePath}");
            EnableButtonAndChangeCursor(sender);
        }
        private async void B_EditAlarmUdt_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            if (doc_g.DocumentElement != null && xmlFile_g != null)
            {
                try
                {
                    AlarmEditData editData = new();
                    await XmlOperations.EditUdtAlarmsXmlAsync(doc_g, doc_g.DocumentElement, editData);
                    TB_Status.AddLine($"\n Done editing Alarms! Changed: {editData.AlarmChanged}, Passed: {editData.AlarmPassed}");
                    string newName = expFolderPath + @"\" + xmlFile_g.Name;
                    TB_Status.AddLine($"\n Saved edited file in: {newName}");
                    doc_g.Save($"{newName}");
                }
                catch (Exception ex)
                {
                    TB_Status.AddLine($"\n {ex.Message}");
                    TB_Status.AddLine($"\n {ex.StackTrace}");
                }
            }
            else
                TB_Status.AddLine($"\n Xml File is not correct!");
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
                TB_Status.AddLine("\n Fill all labels!");
                return;
            }
            if (doc_g.DocumentElement != null && xmlFile_g != null)
            {
                try
                {
                    TagPropertyEditData editData = new();
                    textLogg_g.WriteLine($"Started editing {xmlFile_g.Name}");
                    await XmlOperations.EditUdtPropertiesXmlAync(doc_g, doc_g.DocumentElement, editData, textLogg_g, tagGroup, valueToEdit, value);
                    TB_Status.AddLine($"\n Done editing! Properties in Groups -> Added:{editData.GroupPropAdded} Edited:{editData.GroupPropChange}, Properties in Tags ->Added:{editData.TagPropAdded} Edited:{editData.TagPropChanged}");
                    string newName = expFolderPath + @"\" + xmlFile_g.Name;
                    TB_Status.AddLine($"\n Saved edited file in: {newName}");
                    doc_g.Save($"{newName}");
                }
                catch (Exception ex)
                {
                    TB_Status.AddLine($"\n {ex.Message}");
                    TB_Status.AddLine($"\n {ex.StackTrace}");
                }
            }
            else
                TB_Status.AddLine($"\n Xml File is not correct!");
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
                TB_Status.AddLine($"\n {ex.Message}");
                TB_Status.AddLine(ex.StackTrace != null ? $"\n {ex.StackTrace}" : "");
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
                TB_Status.AddLine($"\n Selected xml file to Edit : {xmlFile_g.FullName}");
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
        private void B_SelectJsonToEdit_Click(object sender, RoutedEventArgs e)
        {
            jsonFile_g = SelectJsonFileAndTryToUse("Select file exported from Ignition (.json)");
            if (jsonFile_g != null)
            {
                L_SelectedJsonToEditPath.Text = jsonFile_g.FullName;
                TB_Status.AddLine($"\n Selected json file to Edit : {jsonFile_g.FullName}");
                json_g = JObject.Parse(System.IO.File.ReadAllText(jsonFile_g.FullName));
                if (jsonFile_g != null)
                {
                    B_EditTagUdtJson.IsEnabled = true;
                }
            }
        }
        private void B_EditTagUdtJson_Click(object sender, RoutedEventArgs e)
        {
           
            if (jsonFile_g != null && json_g != null)
            {
                int arrayIndexToFind = -1;
                string propertyToEdit = TB_JsonPropertyToMultiply.Text;
                if (propertyToEdit == string.Empty)
                {
                    TB_Status.AddLine("\nProperty to edit is empty! Enter property name to edit");
                    return;
                }
                if (TB_JsonIndexToMultiply.Text == string.Empty || !int.TryParse(TB_JsonIndexToMultiply.Text, out arrayIndexToFind) || arrayIndexToFind <= -1)
                {
                    TB_Status.AddLine("\nIndex is not correct! Must be a number and greater or equal 0!");
                    return;
                }
                var result = JsonOperations.MultiplyProperties(json_g, jsonFile_g, expFolderPath, propertyToEdit, arrayIndexToFind, textLogg_g);
                TB_Status.AddLine(result);
            }
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
        public FileInfo? SelectXmlFileAndTryToUse(string title)
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
                FileInfo xmlFile = new(openFileDialog1.FileName);
                if (xmlFile.Exists && !Tools.IsFileLocked(xmlFile.FullName))
                {
                    return xmlFile;
                }
                TB_Status.AddLine("\n File not exist or in use!");
                return null;
            }
            else
            {
                TB_Status.AddLine("\n File not selected!");
                return null;
            }
        }
        public FileInfo? SelectJsonFileAndTryToUse(string title)
        {
            OpenFileDialog openFileDialog1 = new()
            {
                InitialDirectory = @"c:\Users\localadm\Desktop",
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "json",
                Filter = "Json file (*.json)|*.json",
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                ShowReadOnly = true,
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                FileInfo xmlFile = new(openFileDialog1.FileName);
                if (xmlFile.Exists && !Tools.IsFileLocked(xmlFile.FullName))
                {
                    return xmlFile;
                }
                TB_Status.AddLine("\n File not exist or in use!");
                return null;
            }
            else
            {
                TB_Status.AddLine("\n File not selected!");
                return null;
            }
        }
        public FileInfo? SelectXlsxFileAndTryToUse(string title)
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
                FileInfo xmlFile = new(openFileDialog1.FileName);
                if (xmlFile.Exists && !Tools.IsFileLocked(xmlFile.FullName))
                {
                    return xmlFile;
                }
                TB_Status.AddLine("\n File not exist or in use!");
                return null;
            }
            else
            {
                TB_Status.AddLine("\n File not selected!");
                return null;
            }
        }
        #endregion
    }
}
