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
using System.Text.RegularExpressions;

namespace TimeSheetParser
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : Window
    {

        string issueMarker = "** ";
        IEnumerable<Entry> readCSV;
        List<string> staffList = new List<string>();
        double minimumHoursPerWeek = 40;



        public OutputWindow()
        {
            InitializeComponent();

            readCSV = ReadInCSV(MainWindow.selectedfile); //Read in CSV and ensure is valid timesheet.
        

            createStaffList();
            runRules();


        }
        //read in CSV
        IEnumerable<Entry> ReadInCSV(string fileName)
        {
            //Ensure is processed as CSV
            //string[] lines = File.ReadAllLines(Path.ChangeExtension(fileName, ".csv"));
            string[] lines = File.ReadAllLines(fileName);
            return lines.Skip(1).Select(line =>
            {
                //string[] inputLine = line.Split(',');
                Regex CSVParse = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                string[] inputLine = CSVParse.Split(line);
                

                //Run rules. 
                if (rule1(inputLine) == "1")
                {    
                    return new Entry(inputLine[0], inputLine[1], inputLine[2], inputLine[3], inputLine[4], inputLine[5], inputLine[6], inputLine[7], inputLine[8], "1", "1");
                }
                else
                {
                    return new Entry(inputLine[0], inputLine[1], inputLine[2], inputLine[3], inputLine[4], inputLine[5], inputLine[6], inputLine[7], inputLine[8]);
                }

                return new Entry(inputLine[0], inputLine[1], inputLine[2], inputLine[3], inputLine[4], inputLine[5], inputLine[6], inputLine[7], inputLine[8]);
            });
        }
        private void runRules()
        {
              rule3();

            ruleResults();
        }

        //rule 1 function - Required fields. 
        //The main types of work are 1) BAU activity and 2) service delivery
        //BAU work requires a ticket number and a comment at the very least. 
        //Service delivery requires a comment and no ticket number in ticket reference or description (text with prefix of : CAS, IM, 6 digit Devops, INC-, 
        public string rule1(string[] row)
        {
            string res = "0";
            if (row[3] == "Support")
            {
                if (row[4] == "") // ticket number is empty
                {
                    res = "1";
                }
            }

            if (row[3] == "Service Delivery")
            {
                if (row[4] != "") // ticket number has content
                {
                    res = "1";
                }
            }
            return res;
        }

        //rule 2 function - Only one ticket present in the ticket reference
        private bool[] rule2(string[] line)
        {
            bool[] result = new bool[line.Length + 1];
            //quick loop to ensure array is full of falses. 
            for (int i = 0; i < result.Length; i++)
                result[i] = false;



            return result;
        }

        //rule 3 function - minimum 40 hours for the week (indicates not filled out timesheet)
        //need to pull in the whole lines array and check by staffname and count the hours against activities. 
        //count staff by checking each line and adding twithin an array. 
        private void rule3()
        {
            bool result = false;
            string staffName;
            string temp = "";
            int counter = 1;

            //create a dictionary to hold user and hours
            IDictionary<string, double> hoursDict = new Dictionary<string, double>();

            //Iterate through each row of CSV and add up hours for each user. 
            foreach (string name in staffList)
            {
                hoursDict.Add(name, 0); //create entry into dictionary
                foreach (Entry row in readCSV.OfType<Entry>().Where(sn => sn.StaffName == name))
                {

                    temp = row.Time;
                    temp = temp.Replace('"', ' ').Trim();
                    temp = temp.Replace('/', ' ').Trim();
                    hoursDict[name] = hoursDict[name] + Convert.ToDouble(temp);
                }
            }

            //now action if under 40 hours
            bool issuefound = false;
            foreach (var hours in hoursDict)
            {
                if (hours.Value < minimumHoursPerWeek)
                {
                    issuefound = true;
                    txtblkUnfinishedTimesheets.Text += counter + ". " + hours.Key + " - " + hours.Value.ToString() + " hrs";
                    txtblkUnfinishedTimesheets.Text += Environment.NewLine;
                    counter++;
                }
                if (!issuefound)
                    txtblkUnfinishedTimesheets.Text = "No Issues Found";
            }

        }

        private void ruleResults()
        {
            int counter = 1;
            int numOfIssues = 0;
            foreach (string name in staffList)
            {
                foreach (Entry row in readCSV.OfType<Entry>().Where(iss => iss.Issue == "1").Where(n => n.StaffName == name))
                {
                    numOfIssues++;
                    ListViewEntries.Items.Add(row);
                }
                if (numOfIssues > 0)
                {
                    txtblkStaffWithTimesheetIssues.Text += counter + ". " + name.ToString() + " - ";
                    txtblkStaffWithTimesheetIssues.Text += numOfIssues + " issues found" + Environment.NewLine;
                    counter++;
                    numOfIssues = 0;
                }
                
            }

            //output highlighted rows. 
            //foreach (Entry row in readCSV.OfType<Entry>().Where(iss => iss.Issue == "1"))
            //{
            //    if (row.IssueId == "1")
            //    {
                   
            //    }
                    
            //   // if (row.IssueId == 2)
            //   // if (row.IssueId == 3)
            //   ListViewEntries.Items.Add(row);
            //}

        }
        private string determineTicketNumberInText(string text)
        {
            string result = "";


            return result;
        }

        private int determineNumberofStaff()
        {
            int numStaff = readCSV.Select(x => x.StaffName).Distinct().Count();
            return numStaff;
        }

        private void createStaffList()
        {
            string staffName;
            foreach (Entry row in readCSV.OfType<Entry>())
            {
                //if the current row's staffname is not in the staffList array, add it at the next spot. 
                staffName = row.StaffName;
                if (!staffList.Contains(staffName))
                {
                    staffList.Add(staffName);
                }
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
            public string Issue { get; set; }
            public string IssueId { get; set; }


            public Entry(string jobName, string jobNumber, string entryDate, string taskName, string ticketNumber, string label, string staffName, string description, string time, string issue, string issueId)
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
                Issue = issue;
                IssueId = issueId;
            }
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
                Issue = "0";
                IssueId = "-1";
            }

        }
    }
}
