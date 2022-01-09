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
using System.Globalization;
using CsvHelper;

namespace TimeSheetParser
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : Window
    {
        List<Entry> readCSV = new List<Entry>();
        List<string> staffList = new List<string>();
        double minimumHoursPerWeek = 40;

        string folderLocation = "Environment.GetFolderPath(Environment.SpecialFolder.Desktop)";
        static string outputCSVFileName = "csv.csv";
        static string outputErrorsFilename = 
            DateTime.Now.ToString("yyyy") + 
            DateTime.Now.ToString("mm") + 
            DateTime.Now.ToString("dd") +
            DateTime.Now.ToString("ss") +
            " - " + "errors.csv";
        //string outputPathCSV = Path.Combine(folderLocation, outputCSVFileName);
        string outputPathErrors = Path.Combine(@"C:\temp\", outputErrorsFilename);


        

        public OutputWindow()
        {
            InitializeComponent();


            ReadInCSV(MainWindow.selectedfile); //Read in CSV and ensure is valid timesheet. 
            CreateStaffList();
            RunRules();
            RuleResults();

        }
        //read in CSV
        private void ReadInCSV(string fileName)
        {
            //Ensure is processed as CSV
            //StringBuilder csv = new StringBuilder();
            //if (File.Exists(outputPathCSV))
            //{
            //    File.Delete(outputPathCSV);
            //}

            string[] lines = File.ReadAllLines(fileName);
            foreach(string line in lines.Skip(1))
            {
                Regex CSVParse = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                string[] inputLine = CSVParse.Split(line);

                CreateOutputFile(inputLine, outputCSVFileName);
                Entry ent = new Entry(inputLine[0], inputLine[1], inputLine[2], inputLine[3], inputLine[4], inputLine[5], inputLine[6], inputLine[7], Convert.ToDouble(inputLine[8]));
                readCSV.Add(ent);
            }
        }

        //create CSV output file. 
        private void CreateOutputFile(string[] line, string fileName)
        {
            string enteredRow = "";
            foreach (string item in line)
            {
                enteredRow = enteredRow + item + ",";
            }
            enteredRow = enteredRow + Environment.NewLine;
            File.AppendAllText(fileName, enteredRow.ToString());
        }

        private void RunRules()
        {
            Rule1(); //ticket number present
            Rule2(); //One ticket number per entry. 
            Rule3(); //description present
            Rule4(); //ticket number format in ticketnumber entry. 
            Rule10(); //check for 40+ hours entered in timesheet. 
        }

        //rule 1 function - Required fields. 
        //The main types of work are 1) BAU activity and 2) service delivery
        //BAU work requires a ticket number and a comment at the very least. 
        //Service delivery requires a comment and no ticket number in ticket reference or description (text with prefix of : CAS, IM, 6 digit Devops, INC-)
        //returns a 1 if support has no ticket number. Returns a 2 if Service delivery and has a ticket number.
        private void Rule1()
        {
            foreach (Entry row in readCSV)
            {
                if (row.TaskName == "Support")
                {
                    if (row.TicketNumber == "") // ticket number is empty
                    {
                        row.IssueComment = "Support - Ticket # Needed. ";
                        row.IssueID = 1;
                    }
                }

                if (row.TaskName == "Service Delivery")
                {
                    if (row.TicketNumber != "") // ticket number has content
                    {
                        row.IssueComment = "SD - Remove Ticket #. ";
                        row.IssueID = 1;
                    }
                }
            }
        }
        //rule 2 function - Only one ticket present in the ticket reference
        private void Rule2()
        {
            int numberOfTicketNumbers = 0;
            foreach (Entry row in readCSV)
            {
                //Test for multiple ticket Numbers
                //Break ticket number field into individual words
                string[] lineToWords = Regex.Split(row.TicketNumber, @"\s+");
                //create error
                if (CountTicketsInEntry(lineToWords) > 1)
                {
                    row.IssueID = 2;
                    row.IssueComment = row.IssueComment + "Only 1 Ticket # per entry. ";
                }
            }           
        }
        //rule 3 function - make sure there is a description
        private void Rule3()
        {
            foreach (Entry row in readCSV)
            {
               if(row.Description == "")
                {
                    row.IssueID = 3;
                    row.IssueComment = row.IssueComment + "Description needed. ";
                }
            }
        }
        //rule 4 function - make sure ticket numbers are valid
        private void Rule4()
        {
            string casTicketPattern = @"^([Cc][Aa][Ss]-)\d{5}(-).{6}$"; //CAS-ddddd-******
            string invTicketPattern = @"^([Ii][Nn][Cc]-)\d{5}$"; //INC-ddddd
            string devOpsTicketPattern = @"^\d{6}$"; //dddddd
            foreach (Entry row in readCSV)
            {
                
                //Break ticket number field into individual 'words' as string[]
                string[] lineToWords = Regex.Split(row.TicketNumber, @"\s+");
                if (CountTicketsInEntry(lineToWords) > 0)
                {
                    bool badFormat = true;  
                    foreach(string word in lineToWords)
                    {
                        //test patterns against each word
                        string str = word.Trim('"');
                        
                        if (Regex.IsMatch(str, devOpsTicketPattern))
                        {
                            badFormat = false;
                        }
                        if (Regex.IsMatch(str, casTicketPattern))
                        {
                            badFormat = false;
                        }
                        if (Regex.IsMatch(word, invTicketPattern))
                        {
                            badFormat = false;
                        }
                    }
                    if (badFormat)
                    {
                        row.IssueID = 4;
                        row.IssueComment = row.IssueComment + "Bad ticket(s) format. ";
                    }
                    badFormat = true;
                }
            }
        }
        //rule 10 function - minimum 40 hours for the week (indicates not filled out timesheet)
        //need to pull in the whole lines array and check by staffname and count the hours against activities. 
        //count staff by checking each line and adding twithin an array. 
        private void Rule10()
        {
            bool result = false;
            string staffName;
            string rawTime = "";
            int counter = 1;

            //create a dictionary to hold user and hours
            IDictionary<string, double> hoursDict = new Dictionary<string, double>();

            //Iterate through each row of CSV and add up hours for each user. 
            foreach (string name in staffList)
            {
                hoursDict.Add(name, 0); //create entry into dictionary
                foreach (Entry row in readCSV.OfType<Entry>().Where(sn => sn.StaffName == name))
                {
                    hoursDict[name] = hoursDict[name] + row.Time;
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

        //Returns the results to the output window and to the Error document. 
        private void RuleResults()
        {
            int counter = 1;
            int numOfIssues = 0;

            StringBuilder errorCsv = new StringBuilder();

            foreach (string name in staffList)
            {
                //updates the list of staff with timesheet issues
                //output to error file for use later. 
                errorCsv.Append(name + ",");
                foreach (Entry row in readCSV.OfType<Entry>().Where(iss => iss.IssueID != 0).Where(n => n.StaffName == name))
                {
                    numOfIssues++;

                    //output to screen
                    ListViewEntries.Items.Add(row);
                    //output to error file for use later. 
                    errorCsv.Append(Environment.NewLine);
                    errorCsv.Append(row.JobName.ToString() + ",");
                    errorCsv.Append(row.JobNumber.ToString() + ",");
                    errorCsv.Append(row.EntryDate.ToString() + ",");
                    errorCsv.Append(row.TaskName.ToString() + ",");
                    errorCsv.Append(row.TicketNumber.ToString() + ",");
                    errorCsv.Append(row.Label.ToString() + ",");
                    errorCsv.Append(row.StaffName.ToString() + ",");
                    errorCsv.Append(row.Description.ToString() + ",");
                    errorCsv.Append(row.Time.ToString() + ",");
                    errorCsv.Append(row.IssueComment.ToString() + ",");    
                }


                if (numOfIssues > 0)
                {
                    errorCsv.Append(Environment.NewLine);
                    errorCsv.Append(Environment.NewLine);
                    txtblkStaffWithTimesheetIssues.Text += counter + ". " + name.ToString() + " - ";
                    txtblkStaffWithTimesheetIssues.Text += numOfIssues + " issues found" + Environment.NewLine;
                    counter++;
                    
                }
                if (numOfIssues == 0)
                {
                    errorCsv.Append("--- No Issues --- ");
                    errorCsv.Append(Environment.NewLine);
                    errorCsv.Append(Environment.NewLine);
                }
                File.WriteAllText(outputPathErrors, errorCsv.ToString());
                numOfIssues = 0;

            }
        }
        
        
        //Counts how many ticket numbers there are in an entry and returns an int value.
        //input is the entry data as string array (string[]) for one cell to check. 
        private int CountTicketsInEntry(string[] ticketNumberEntry)
        {
            int res = 0;
            foreach (string word in ticketNumberEntry)
            {
                if (word != "")
                {
                    res++;
                }
            }
            return res;
        }


        private int DetermineNumberofStaff()
        {
            int numStaff = readCSV.Select(x => x.StaffName).Distinct().Count();
            return numStaff;
        }

        private void CreateStaffList()
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
            public double Time { get; set; }
            public int IssueID { get; set; }
            public string IssueComment { get; set; }


            public Entry(string jobName, string jobNumber, string entryDate, string taskName, string ticketNumber, string label, string staffName, string description, double time, int issueID, string issueComment)
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
                IssueID = issueID;
                IssueComment = issueComment;
            }
            public Entry(string jobName, string jobNumber, string entryDate, string taskName, string ticketNumber, string label, string staffName, string description, double time)
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
                IssueID = 0;
                IssueComment = "";
            }

        }
    }
}
