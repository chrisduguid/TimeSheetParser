using System;
using System.Collections.Generic;
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
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Xml;


namespace TimeSheetParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //common variables
        public static string selectedfile = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            //Set default path to downloads. 
            string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
            
            
            OpenFileDialog fileSearchDialog = new OpenFileDialog();

            fileSearchDialog.Filter = "All files (*.*)|*.*";
            fileSearchDialog.InitialDirectory = path;

            if (fileSearchDialog.ShowDialog() == true)
            {
                //prepopulate the sample box to confirm data before processing
                txtSample.Text = File.ReadAllText(fileSearchDialog.FileName); //This needs error handling as compiler stops if document open. 
                selectedfile = fileSearchDialog.FileName;
                txtFileName.Text = selectedfile;
            }
        }

        private void btnRunRules_Click(object sender, RoutedEventArgs e)
        {
            OutputWindow output = new OutputWindow();
            output.Show();
        }
    }



}
