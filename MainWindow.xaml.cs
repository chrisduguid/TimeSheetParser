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
            string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
            OpenFileDialog fileSearchDialog = new OpenFileDialog();

            fileSearchDialog.Filter = "All files (*.*)|*.*";
            fileSearchDialog.InitialDirectory = path;
            //fileSearchDialog.ShowDialog();

            if (fileSearchDialog.ShowDialog() == true)
                txtSample.Text = File.ReadAllText(fileSearchDialog.FileName);
            selectedfile = fileSearchDialog.FileName;
            txtFileName.Text = selectedfile;

        }

        private void btnRunRules_Click(object sender, RoutedEventArgs e)
        {
            OutputWindow output = new OutputWindow();
            output.Show();
        }
    }



}
