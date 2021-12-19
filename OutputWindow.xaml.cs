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
using System.Windows.Shapes;
using System.IO;
using Path = System.IO.Path;

namespace TimeSheetParser
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : Window
    {
        

        public OutputWindow()
        {
            InitializeComponent();
            ListViewEntries.ItemsSource = ReadCSV(MainWindow.selectedfile);

            //read in CSV
            IEnumerable<Entry> ReadCSV(string fileName)
            {
                //Ensure is processed as CSV
                //string[] lines = File.ReadAllLines(Path.ChangeExtension(fileName, ".csv"));
                string[] lines = File.ReadAllLines(fileName);
                return lines.Select(line =>
                {
                    string[] data = line.Split(',');
                    return new Entry(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8]);
                });
            }
        }

        //create an object for each time entry
        public class Entry
        {
            public string JobName { get; set; }
            public string JobNumber { get; set; }
            public string EntryDate { get; set; }
            public string TaskName { get; set; }
            public string TicketNumber { get; set; }
            public string Label { get; set; }
            public string StaffName { get; set; }
            public string Description { get; set; }
            public string Time { get; set; }

            public Entry(string jobName, string jobNumber, string entryDate, string taskName, string ticketNumber, string label, string staffName, string description, string time)
            {
                JobName = jobName;
                JobNumber = jobNumber;
                EntryDate = entryDate;
                TaskName = taskName;
                TicketNumber = ticketNumber;
                Label = label;
                StaffName = staffName;
                Description = description;
                Time = time;
            }

            
        }
    }
}
