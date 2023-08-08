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
using IgnitionHelper.Data_Containers;
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
        public FileInfo? TagsFile;
        public FileInfo? XmlFile;
        public FileInfo? JsonFile;
        public XmlDocument XmlDoc;
        public JObject? Json;

        private static StreamWriter _textLogg = null!;
        private static List<TempInstanceVisu> _tempInstList = null!;
        private static string _expFolderPath = @"C:\Users\plradlig\Desktop\Lucid\Ignition\ExportFiles\App_Exp_files";

        public List<TagDataPLC> TagDataList { get; set; }
        public Func<Task> DeleteTagsFunc { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            TagDataList = new List<TagDataPLC>();
            _tempInstList = new List<TempInstanceVisu>();
            XmlDoc = new();
            Json = null;
            _expFolderPath = CheckExportFolderPath(_expFolderPath, TB_ExpFolderPath);
            _textLogg = new StreamWriter(@$"{_expFolderPath}\textLogg.txt");
            DeleteTagsFunc = DeleteTagsAsync;
        }
        #region UI_EventHandlers
        private async void B_SelectTagsXLSX_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            var includedDTName = TB_FilterTextUdt.Text;
            TagsFile = SelectXlsxFileAndTryToUse("Select Exported from Studion5000 Tags Table (.xlsx)");
            if (TagsFile != null)
            {
                L_PLCTagsFilePath.Text = TagsFile.FullName;
                TB_Status.AddLine($"Selected: {TagsFile.FullName}");
                try
                {
                    if (TagDataList.Count >= 0)
                        TagDataList.Clear();
                    TagDataList = await ExcelOperations.LoadTagDataListFromExcelFile(TagsFile, includedDTName);
                    TB_Status.AddLine($"Acquired {TagDataList.Count} tags");
                    B_GenerateXml.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    TB_Status.AddLine($"{ex.Message}");
                    TB_Status.AddLine($"{ex.StackTrace}");
                }
            }
            EnableButtonAndChangeCursor(sender);
        }
        private async void B_GenerateXml_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            XmlFile = SelectXmlFileAndTryToUse("Select file exported from Ignition (.xml)");
            if (XmlFile != null)
            {
                L_HMITagsFilePath.Text = XmlFile.FullName;
                XmlDoc.Load(XmlFile.FullName);
                if (XmlDoc.DocumentElement != null)
                {
                    try
                    {
                        if (_tempInstList.Count >= 0)
                            _tempInstList.Clear();
                        await XmlOperations.CreateTemplatesAsync(XmlDoc.DocumentElement, _tempInstList, _textLogg, null, null);
                        TB_Status.AddLine($"Number of template nodes got: {_tempInstList.Count}");
                    }
                    catch (Exception ex)
                    {
                        TB_Status.AddLine($"{ex.Message}");
                        TB_Status.AddLine($"{ex.StackTrace}");
                    }
                    if (_tempInstList.Count > 0)
                    {
                        try
                        {
                            bool isNameTolerance = CB_ExtendNameToleration.IsChecked ?? false;
                            await XmlOperations.UpdateTagDataListWithXmlDataAsync(XmlDoc.DocumentElement, TagDataList, _textLogg, null, null, isNameTolerance);
                            TB_Status.AddLine($"Done checking! There was aleady {TagDataList.Count(item => item.IsAdded)}/{TagDataList.Count} instances ");
                            await XmlOperations.AddTemplatedTagsToXmlAsync(XmlDoc.DocumentElement, TagDataList, _tempInstList, _textLogg, null, null, isNameTolerance);
                            TB_Status.AddLine($"Done editing! {TagDataList.Count(item => item.IsAdded)}/{TagDataList.Count} instances done");
                            string newName = _expFolderPath + @"\" + XmlFile.Name.Replace(".xml", "_edit.xml");
                            TB_Status.AddLine($"Found instances Added in XML and NOT CORRECT: {TagDataList.Count(item => item.IsAdded && !item.IsCorrect)}");
                            XmlDoc.Save($"{newName}");
                            TagDataList.GetFoldersInfo(TB_Status);
                            TB_Status.AddLine($"Saved file: {newName}");
                            foreach (var item in TagDataList)
                            {
                                _textLogg.WriteLine($"name:{item.Name};dataTypePLC:{item.DataTypePLC};dataTypeVisu:{item.DataTypeVisu};folderName:{item.VisuFolderName};isAdded:{item.IsAdded}");
                            }
                            B_GenExcelTagData.IsEnabled = true;
                            B_ShowTagsWindow.IsEnabled = true;
                        }
                        catch (Exception ex)
                        {
                            TB_Status.AddLine($"{ex.Message}");
                            TB_Status.AddLine($"{ex.StackTrace}");
                        }
                    }
                }
            }
            EnableButtonAndChangeCursor(sender);
        }
        private async void B_GenExcelTagData_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            String filePath = _expFolderPath + @"\TagDataExport.xlsx";
            var excelPackage = ExcelOperations.CreateExcelFile(filePath, _textLogg);
            if (excelPackage == null)
            {
                TB_Status.AddLine($"Failed to create (.xlsx) file!");
                return;
            }
            var ws = excelPackage.Workbook.Worksheets.Add("Tag Data List");
            var range = ws.Cells["A1"].LoadFromCollection(TagDataList, true);
            range.AutoFitColumns();
            await ExcelOperations.SaveExcelFile(excelPackage);
            TB_Status.AddLine($"Created file : {filePath}");
            EnableButtonAndChangeCursor(sender);
        }
        private async void B_EditAlarmUdt_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            if (XmlDoc.DocumentElement != null && XmlFile != null)
            {
                try
                {
                    AlarmEditData editData = new();
                    await XmlOperations.EditUdtAlarmsXmlAsync(XmlDoc, XmlDoc.DocumentElement, editData);
                    TB_Status.AddLine($"Done editing Alarms! Changed: {editData.AlarmChanged}, Passed: {editData.AlarmPassed}");
                    string newName = _expFolderPath + @"\" + XmlFile.Name;
                    TB_Status.AddLine($"Saved edited file in: {newName}");
                    XmlDoc.Save($"{newName}");
                }
                catch (Exception ex)
                {
                    TB_Status.AddLine($"{ex.Message}");
                    TB_Status.AddLine($"{ex.StackTrace}");
                }
            }
            else
                TB_Status.AddLine($"Xml File is not correct!");
            EnableButtonAndChangeCursor(sender);
        }
        private async void B_EditTagUdt_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            TagGroupPath tagGroup = new TagGroupPath(TB_TagGroup.Text);
            string valueToEdit = TB_ValueToEdit.Text;
            string value = TB_EditValue.Text;
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(tagGroup.SourcePath) || string.IsNullOrEmpty(valueToEdit))
            {
                TB_Status.AddLine($"Fill all labels!");
                return;
            }
            if (XmlDoc.DocumentElement != null && XmlFile != null)
            {
                try
                {
                    TagPropertyEditData editData = new();
                    _textLogg.WriteLine($"Started editing {XmlFile.Name}");
                    await XmlOperations.EditUdtPropertiesXmlAync(XmlDoc, XmlDoc.DocumentElement, editData, _textLogg, tagGroup, valueToEdit, value);
                    TB_Status.AddLine($"Done editing! Properties in Groups -> Added:{editData.GroupPropAdded} Edited:{editData.GroupPropChange}, Properties in Tags ->Added:{editData.TagPropAdded} Edited:{editData.TagPropChanged}");
                    string newName = _expFolderPath + @"\" + XmlFile.Name;
                    TB_Status.AddLine($"Saved edited file in: {newName}");
                    XmlDoc.Save($"{newName}");
                }
                catch (Exception ex)
                {
                    TB_Status.AddLine($"{ex.Message}");
                    TB_Status.AddLine($"{ex.StackTrace}");
                }
            }
            else
                TB_Status.AddLine($"Xml File is not correct!");
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
                _expFolderPath = openFolderDialog.SelectedPath;
                TB_ExpFolderPath.Text = _expFolderPath;
                EB_ExpFolderSelected();
            }
        }
        private void B_OpenExpFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", _expFolderPath);
            }
            catch (Exception ex)
            {
                TB_Status.AddLine($"{ex.Message}");
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
            XmlFile = SelectXmlFileAndTryToUse("Select file exported from Ignition (.xml)");
            if (XmlFile != null)
            {
                L_SelectedFileToEditPath.Text = XmlFile.FullName;
                TB_Status.AddLine($"Selected xml file to Edit : {XmlFile.FullName}");
                XmlDoc.Load(XmlFile.FullName);
                if (XmlDoc.DocumentElement != null)
                {
                    B_EditTagUdt.IsEnabled = true;
                    B_EditAlarmUdt.IsEnabled = true;
                }
            }
        }
        private void B_TextloggClose_Click(object sender, RoutedEventArgs e)
        {
            _textLogg.WriteLine("Manually closed.");
            _textLogg.Close();
        }
        private void B_SelectJsonToEdit_Click(object sender, RoutedEventArgs e)
        {
            JsonFile = SelectJsonFileAndTryToUse("Select file exported from Ignition (.json)");
            if (JsonFile != null)
            {
                L_SelectedJsonToEditPath.Text = JsonFile.FullName;
                TB_Status.AddLine($"Selected json file to Edit : {JsonFile.FullName}");
                try
                {
                    Json = JObject.Parse(System.IO.File.ReadAllText(JsonFile.FullName));
                }
                catch (Exception ex)
                {
                    TB_Status.AddLine($"{ex.Message}");
                }

                if (JsonFile != null)
                {
                    B_EditTagUdtJson.IsEnabled = true;
                    B_MultiplyTagUdtJson.IsEnabled = true;
                }
            }
        }
        private void B_EditTagUdtJson_Click(object sender, RoutedEventArgs e)
        {

            if (JsonFile != null && Json != null)
            {
                string propertyToEdit = TB_JsonPropertyToMultiply.Text;
                if (propertyToEdit == string.Empty)
                {
                    TB_Status.AddLine("Property to edit is empty! Enter property name to edit");
                    return;
                }
                if (TB_JsonIndexToMultiply.Text == string.Empty || !int.TryParse(TB_JsonIndexToMultiply.Text, out var arrayIndexToFind) || arrayIndexToFind <= -1)
                {
                    TB_Status.AddLine("Index is not correct! Must be a number and greater or equal 0!");
                    return;
                }
                var result = JsonOperations.MultiplyProperties(Json, JsonFile, _expFolderPath, propertyToEdit, arrayIndexToFind, _textLogg);
                TB_Status.AddLine(result);
            }
        }
        private void B_MultiplyTagUdtJson_Click(object sender, RoutedEventArgs e)
        {
            if (JsonFile != null && Json != null)
            {
                string propertyToEdit = TB_JsonPropertyToMultiply.Text;
                string nodeNameToMultiply = TB_NodeNameToMultiply.Text;
                if (propertyToEdit == string.Empty)
                {
                    TB_Status.AddLine("Property to edit is empty! Enter property name to edit");
                    return;
                }
                if (nodeNameToMultiply == "nodeNameToMultiply")
                {
                    TB_Status.AddLine("Edit Node name to multiply!");
                    return;
                }
                var result = JsonOperations.MultiplyTag(Json, JsonFile, _expFolderPath, propertyToEdit, nodeNameToMultiply, _textLogg);
                TB_Status.AddLine(result);
            }
        }
        private void B_ShowTagsWindow_Click(object sender, RoutedEventArgs e)
        {
            TagsWindow tagsWindow = new(this);
            tagsWindow.Show();
        }
        private void TB_FilterTextUdt_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Init bypass
            if (B_SelectTagsXLSX == null)
                return;

            TextBox tb = (TextBox)sender;
            if (tb.Text != string.Empty)
            {
                B_SelectTagsXLSX.IsEnabled = true;
                return;
            }
            TB_Status.AddLine("Fill TextBox 'Strings included in Data Type name'!");
            B_SelectTagsXLSX.IsEnabled = false;
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _textLogg.Close();
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
                return toReturn;
            }
            var openFolderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            {
                openFolderDialog.Description = "Select export Directory:";
            };
            var result = openFolderDialog.ShowDialog();
            if (result == true)
            {
                expFolderPath = openFolderDialog.SelectedPath;
                expFolderPath = Tools.OverridePathWithDateTimeSubfolder(expFolderPath);
                toReturn = expFolderPath;
                tb.Text = expFolderPath;
                EB_ExpFolderSelected();
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
                TB_Status.AddLine($"File not exist or in use!");
                return null;
            }
            TB_Status.AddLine($"File not selected!");
            return null;
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
                TB_Status.AddLine($"File not exist or in use!");
                return null;
            }
            TB_Status.AddLine($"File not selected!");
            return null;
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
                TB_Status.AddLine($"File not exist or in use!");
                return null;
            }
            TB_Status.AddLine($"File not selected!");
            return null;
        }
        #endregion
        #region Externally Called Functions
        public async Task DeleteTagsAsync()
        {
            Cursor = Cursors.Wait;
            if (XmlFile == null)
            {
                TB_Status.AddLine("Selected XML file does not exist!");
                return;
            }
            try
            {
                if (XmlDoc.DocumentElement == null)
                {
                    TB_Status.AddLine("Selected XML has incorrect content");
                    return;
                }
                await XmlOperations.DeleteSelectedTagsAsync(XmlDoc.DocumentElement, TagDataList, _textLogg);
                TB_Status.AddLine($"Deleted {TagDataList.Count(item => item.Deleted)}/{TagDataList.Count(item => item.ToDelete)} tags(nodes)!");
                string newName = _expFolderPath + @"\" + XmlFile.Name.Replace(".xml", "_edit.xml");
                XmlDoc.Save($"{newName}");
                TB_Status.AddLine($"Saved file: {newName}");
            }
            catch (Exception ex)
            {
                TB_Status.AddLine($"{ex.Message}");
                TB_Status.AddLine($"{ex.StackTrace}");
            }
            Cursor = Cursors.Arrow;
        }

    }
    #endregion
}
