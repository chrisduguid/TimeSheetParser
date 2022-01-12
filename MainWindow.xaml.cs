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
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace TimeSheetParser
{
    
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //common variables
        public static string selectedfile = "";
        //public static string outputFileName = "error.csv";
        //public static string outputFolderName = "Environment.GetFolderPath(Environment.SpecialFolder.Desktop)";


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
            string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Desktop";

            Microsoft.Win32.OpenFileDialog fileSearchDialog = new Microsoft.Win32.OpenFileDialog();

            fileSearchDialog.Filter = "All files (*.*)|*.*";
            fileSearchDialog.InitialDirectory = path;

            if (fileSearchDialog.ShowDialog() == true)
            {
                //prepopulate the sample box to confirm data before processing
                txtSample.Text = File.ReadAllText(fileSearchDialog.FileName); //This needs error handling as compiler stops if document open. 
                selectedfile = fileSearchDialog.FileName;
                txtFileName.Text = selectedfile;
                ValidateInput(selectedfile);
            }
            

            
        }  

        private void ValidateInput(string filename)
        {
            string[] headers = new string[9];
            headers[0] = "[Job] Name";
            headers[1] = "[Job] Job No.";
            headers[2] = "[Time] Date";
            headers[3] = "[Task] Name";
            headers[4] = "[Time] Reference / Ticket #";
            headers[5] = "[Task] Label";
            headers[6] = "[Staff] Name";
            headers[7] = "[Time] Note";
            headers[8] = "[Time] Time";

            string[] lines = File.ReadAllLines(filename);
            Regex CSVParse = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            string[] headerElements = CSVParse.Split(lines[0]);

            List<string> validationIssues = headers.ToList<string>();
            //iterate through the header elements, compare them against the expected elements 
            for (int i = 0; i < headerElements.Length; i++) //aligns with Entry properties
            {
                for (int w = 0; w < headers.Length; w++) //cycle through each inbound element from the inputfile
                {
                    if (headers[w] == headerElements[i].Trim('"'))
                    {
                        validationIssues.Remove(headers[w]); //remove the headers that exist., 
                    }
                }
            }

            //test for issues
            if (validationIssues.Count > 0) //if errors exists
            {
                //display on screen and stop. 
                btnRunRules.IsEnabled = false;
                lblValidationError.Content = "Missing column(s): " + Environment.NewLine;
                foreach (string item in validationIssues)
                {
                    lblValidationError.Content = lblValidationError.Content + item + Environment.NewLine;
                }
            }
            else
            {
                btnRunRules.IsEnabled = true;
                lblValidationError.Content = "";
            }
        }

        private void btnRunRules_Click(object sender, RoutedEventArgs e)
        {
            OutputWindow output = new OutputWindow();
            output.Show();
        }

        private void btnSeletOutputFolder_Click(object sender, RoutedEventArgs e)
        {
       
        }
    }



}
