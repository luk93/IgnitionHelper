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
        public FileInfo? tagsFile = null!;
        public FileInfo? xmlFile = null!;
        public FileInfo? jsonFile = null!;
        public XmlDocument doc;
        public JObject? json;

        public static StreamWriter textLogg = null!;
        public static List<TempInstanceVisu> tempInstList = null!;
        public static int PB_Progress;
        public static string expFolderPath = @"C:\Users\plradlig\Desktop\Lucid\Ignition\ExportFiles\App_Exp_files";

        public List<TagDataPLC> TagDataList { get; set; }
        public Func<Task> DeleteTagsFunc { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            TagDataList = new List<TagDataPLC>();
            tempInstList = new List<TempInstanceVisu>();
            doc = new();
            json = null;
            expFolderPath = CheckExportFolderPath(expFolderPath, TB_ExpFolderPath);
            textLogg = new StreamWriter(@$"{expFolderPath}\textLogg.txt");

            DeleteTagsFunc = new Func<Task>(DeleteTagsAsync);

        }
        #region UI_EventHandlers
        private async void B_SelectTagsXLSX_ClickAsync(object sender, RoutedEventArgs e)
        {
            DisableButtonAndChangeCursor(sender);
            var includedDTName = TB_FilterTextUdt.Text;
            tagsFile = SelectXlsxFileAndTryToUse("Select Exported from Studion5000 Tags Table (.xlsx)");
            if (tagsFile != null)
            {
                L_PLCTagsFilePath.Text = tagsFile.FullName;
                TB_Status.AddLine($"Selected: {tagsFile.FullName}");
                try
                {
                    if (TagDataList.Count >= 0)
                        TagDataList.Clear();
                    TagDataList = await ExcelOperations.LoadTagDataListFromExcelFile(tagsFile, includedDTName);
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
            xmlFile = SelectXmlFileAndTryToUse("Select file exported from Ignition (.xml)");
            if (xmlFile != null)
            {
                L_HMITagsFilePath.Text = xmlFile.FullName;
                doc.Load(xmlFile.FullName);
                if (doc.DocumentElement != null)
                {
                    try
                    {
                        if (tempInstList.Count >= 0)
                            tempInstList.Clear();
                        await XmlOperations.CreateTemplatesAsync(doc.DocumentElement, tempInstList, textLogg, null, null);
                        TB_Status.AddLine($"Number of template nodes got: {tempInstList.Count}");
                    }
                    catch (Exception ex)
                    {
                        TB_Status.AddLine($"{ex.Message}");
                        TB_Status.AddLine($"{ex.StackTrace}");
                    }
                    if (tempInstList.Count > 0)
                    {
                        try
                        {
                            await XmlOperations.UpdateTagDataListWithXmlDataAsync(doc.DocumentElement, TagDataList, textLogg, null, null);
                            TB_Status.AddLine($"Done checking! There was aleady {TagDataList.Count(item => item.IsAdded)}/{TagDataList.Count} instances ");
                            await XmlOperations.AddTemplatedTagsToXmlAsync(doc.DocumentElement, TagDataList, tempInstList, textLogg, null, null);
                            TB_Status.AddLine($"Done editing! {TagDataList.Count(item => item.IsAdded)}/{TagDataList.Count} instances done");
                            string newName = expFolderPath + @"\" + xmlFile.Name.Replace(".xml", "_edit.xml");
                            TB_Status.AddLine($"Found instances Added in XML and NOT CORRECT: {TagDataList.Count(item => item.IsAdded && !item.IsCorrect)}");
                            doc.Save($"{newName}");
                            TagDataList.GetFoldersInfo(TB_Status);
                            TB_Status.AddLine($"Saved file: {newName}");
                            foreach (var item in TagDataList)
                            {
                                textLogg.WriteLine($"name:{item.Name};dataTypePLC:{item.DataTypePLC};dataTypeVisu:{item.DataTypeVisu};folderName:{item.VisuFolderName};isAdded:{item.IsAdded}");
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
            String filePath = expFolderPath + @"\TagDataExport.xlsx";
            var excelPackage = ExcelOperations.CreateExcelFile(filePath, textLogg);
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
            if (doc.DocumentElement != null && xmlFile != null)
            {
                try
                {
                    AlarmEditData editData = new();
                    await XmlOperations.EditUdtAlarmsXmlAsync(doc, doc.DocumentElement, editData);
                    TB_Status.AddLine($"Done editing Alarms! Changed: {editData.AlarmChanged}, Passed: {editData.AlarmPassed}");
                    string newName = expFolderPath + @"\" + xmlFile.Name;
                    TB_Status.AddLine($"Saved edited file in: {newName}");
                    doc.Save($"{newName}");
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
            string tagGroup = TB_TagGroup.Text;
            string valueToEdit = TB_ValueToEdit.Text;
            string value = TB_EditValue.Text;
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(tagGroup) || string.IsNullOrEmpty(valueToEdit))
            {
                TB_Status.AddLine($"Fill all labels!");
                return;
            }
            if (doc.DocumentElement != null && xmlFile != null)
            {
                try
                {
                    TagPropertyEditData editData = new();
                    textLogg.WriteLine($"Started editing {xmlFile.Name}");
                    await XmlOperations.EditUdtPropertiesXmlAync(doc, doc.DocumentElement, editData, textLogg, tagGroup, valueToEdit, value);
                    TB_Status.AddLine($"Done editing! Properties in Groups -> Added:{editData.GroupPropAdded} Edited:{editData.GroupPropChange}, Properties in Tags ->Added:{editData.TagPropAdded} Edited:{editData.TagPropChanged}");
                    string newName = expFolderPath + @"\" + xmlFile.Name;
                    TB_Status.AddLine($"Saved edited file in: {newName}");
                    doc.Save($"{newName}");
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
                expFolderPath = openFolderDialog.SelectedPath;
                TB_ExpFolderPath.Text = expFolderPath;
                EB_ExpFolderSelected();
            }
        }
        private void B_OpenExpFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", expFolderPath);
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
            xmlFile = SelectXmlFileAndTryToUse("Select file exported from Ignition (.xml)");
            if (xmlFile != null)
            {
                L_SelectedFileToEditPath.Text = xmlFile.FullName;
                TB_Status.AddLine($"Selected xml file to Edit : {xmlFile.FullName}");
                doc.Load(xmlFile.FullName);
                if (doc.DocumentElement != null)
                {
                    B_EditTagUdt.IsEnabled = true;
                    B_EditAlarmUdt.IsEnabled = true;
                }
            }
        }
        private void B_TextloggClose_Click(object sender, RoutedEventArgs e)
        {
            textLogg.WriteLine("Manually closed.");
            textLogg.Close();
        }
        private void B_SelectJsonToEdit_Click(object sender, RoutedEventArgs e)
        {
            jsonFile = SelectJsonFileAndTryToUse("Select file exported from Ignition (.json)");
            if (jsonFile != null)
            {
                L_SelectedJsonToEditPath.Text = jsonFile.FullName;
                TB_Status.AddLine($"Selected json file to Edit : {jsonFile.FullName}");
                try
                {
                    json = JObject.Parse(System.IO.File.ReadAllText(jsonFile.FullName));
                }
                catch (Exception ex)
                {
                    TB_Status.AddLine($"{ex.Message}");
                }

                if (jsonFile != null)
                {
                    B_EditTagUdtJson.IsEnabled = true;
                    B_MultiplyTagUdtJson.IsEnabled = true;
                }
            }
        }
        private void B_EditTagUdtJson_Click(object sender, RoutedEventArgs e)
        {

            if (jsonFile != null && json != null)
            {
                int arrayIndexToFind = -1;
                string propertyToEdit = TB_JsonPropertyToMultiply.Text;
                if (propertyToEdit == string.Empty)
                {
                    TB_Status.AddLine("Property to edit is empty! Enter property name to edit");
                    return;
                }
                if (TB_JsonIndexToMultiply.Text == string.Empty || !int.TryParse(TB_JsonIndexToMultiply.Text, out arrayIndexToFind) || arrayIndexToFind <= -1)
                {
                    TB_Status.AddLine("Index is not correct! Must be a number and greater or equal 0!");
                    return;
                }
                var result = JsonOperations.MultiplyProperties(json, jsonFile, expFolderPath, propertyToEdit, arrayIndexToFind, textLogg);
                TB_Status.AddLine(result);
            }
        }
        private void B_MultiplyTagUdtJson_Click(object sender, RoutedEventArgs e)
        {
            if (jsonFile != null && json != null)
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
                var result = JsonOperations.MultiplyTag(json, jsonFile, expFolderPath, propertyToEdit, nodeNameToMultiply, textLogg);
                TB_Status.AddLine(result);
            }
        }
        private void B_ShowTagsWindow_Click(object sender, RoutedEventArgs e)
        {
            TagsWindow tagsWindow = new TagsWindow(this);
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
            textLogg.Close();
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
            else
            {
                TB_Status.AddLine($"File not selected!");
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
                TB_Status.AddLine($"File not exist or in use!");
                return null;
            }
            else
            {
                TB_Status.AddLine($"File not selected!");
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
                TB_Status.AddLine($"File not exist or in use!");
                return null;
            }
            else
            {
                TB_Status.AddLine($"File not selected!");
                return null;
            }
        }
        #endregion
        #region Externally Called Functions
        public async Task DeleteTagsAsync()
        {
            Cursor = Cursors.Wait;
            if (xmlFile == null)
            {
                TB_Status.AddLine("Selected XML file does not exist!");
                return;
            }
            try
            {
                await XmlOperations.DeleteSelectedTagsAsync(doc.DocumentElement, TagDataList, textLogg);
                TB_Status.AddLine($"Deleted {TagDataList.Count(item => item.Deleted)}/{TagDataList.Count(item => item.ToDelete)} tags(nodes)!");
                string newName = expFolderPath + @"\" + xmlFile.Name.Replace(".xml", "_edit.xml");
                doc.Save($"{newName}");
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
