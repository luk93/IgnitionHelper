using IgnitionHelper.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace IgnitionHelper
{
    /// <summary>
    /// Interaction logic for TagsWindow.xaml
    /// </summary>
    public partial class TagsWindow : Window
    {
        private static ObservableCollection<TagDataPLC>? _tagDataObsCol = null;
        private static List<TagDataPLC>? _tagDataShorten = null;
        private MainWindow _mainWindow;
        public TagsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;            
            _tagDataShorten = _mainWindow.TagDataList.Where(t => !t.IsCorrect && t.DataTypeVisu != string.Empty).ToList().DeepCopy();
            _tagDataObsCol = new ObservableCollection<TagDataPLC>(_tagDataShorten); 
            LV_TagData.ItemsSource = _tagDataObsCol;
        }
        private void B_ApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            if(_tagDataObsCol!= null) 
            {
                _mainWindow.TagDataList.GetDataFromObservableCollection(_tagDataObsCol);
                _mainWindow.DeleteTagsFunc();
            }
            ClearObjects();
            this.Close();
        }
        private void B_CancelChanges_Click(object sender, RoutedEventArgs e)
        {
            ClearObjects();
            this.Close();
        }
        private void ClearObjects()
        {
            _tagDataShorten = null;
            _tagDataObsCol = null;
        }
        
    }
}
