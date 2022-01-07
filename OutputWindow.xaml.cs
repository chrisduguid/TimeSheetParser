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
        List<Entry> readCSV = new List<Entry>();
        List<string> staffList = new List<string>();
        double minimumHoursPerWeek = 40;


        static string folderLocation = "Environment.GetFolderPath(Environment.SpecialFolder.Desktop)";
        static string outputCSVFileName = "csv.csv";
        static string outputErrorsFilename = "errors.csv";
        string outputPathCSV = Path.Combine(folderLocation, outputCSVFileName);
        string outputPathErrors = Path.Combine(folderLocation, outputErrorsFilename);


        

        public OutputWindow()
        {
            InitializeComponent();


            ReadInCSV(MainWindow.selectedfile); //Read in CSV and ensure is valid timesheet. 
            CreateStaffList();
            RunRules();
            ruleResults();

        }
        //read in CSV
        private void ReadInCSV(string fileName)
        {
            //Ensure is processed as CSV
            StringBuilder csv = new StringBuilder();
            if (File.Exists(outputPathCSV))
            {
                File.Delete(outputPathCSV);
            }

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
            rule1(); //ticket number present
            rule2(); //One ticket number per entry. 
            rule3(); //description present
            rule4(); //ticket number format. 
            rule10(); //check for 40+ hours entered in timesheet. 
        }

        //rule 1 function - Required fields. 
        //The main types of work are 1) BAU activity and 2) service delivery
        //BAU work requires a ticket number and a comment at the very least. 
        //Service delivery requires a comment and no ticket number in ticket reference or description (text with prefix of : CAS, IM, 6 digit Devops, INC-)
        //returns a 1 if support has no ticket number. Returns a 2 if Service delivery and has a ticket number.
        private void rule1()
        {
            foreach (Entry row in readCSV)
            {
                if (row.TaskName == "Support")
                {
                    if (row.TicketNumber == "") // ticket number is empty
                    {
                        row.IssueComment = "Ticket # Needed. ";
                        row.IssueID = 1;
                    }
                }

                if (row.TaskName == "Service Delivery")
                {
                    if (row.TicketNumber != "") // ticket number has content
                    {
                        row.IssueComment = "Remove Ticket #. ";
                        row.IssueID = 2;
                    }
                }
            }
        }

        //rule 2 function - Only one ticket present in the ticket reference
        private void rule2()
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

        //rule 3 function - make sure there is a description
        private void rule3()
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
        private void rule4()
        {
            string casTicketPattern = @"^([Cc][Aa][Ss]-)\d{5}(-).{6}$";
            string invTicketPattern = @"^([Ii][Nn][Cc]-)\d{5}$"; //INC-ddddd
            string devOpsTicketPattern = @"^\d{6}$";
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
                        row.IssueComment = row.IssueComment + "Bad ticket/s format. ";
                    }
                    badFormat = true;
                }
            }


            //Regex testForTicketNumbers = @  
        }

        //rule 10 function - minimum 40 hours for the week (indicates not filled out timesheet)
        //need to pull in the whole lines array and check by staffname and count the hours against activities. 
        //count staff by checking each line and adding twithin an array. 
        private void rule10()
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
                    //rawTime = row.Time.ToString();
                    //rawTime = rawTime.Replace('"', ' ').Trim();
                    //rawTime = rawTime.Replace('/', ' ').Trim();
                    //hoursDict[name] = hoursDict[name] + Convert.ToDouble(rawTime);
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

        private void ruleResults()
        {
            int counter = 1;
            int numOfIssues = 0;
            foreach (string name in staffList)

            //updates the list of staff with timesheet issues
            {
                foreach (Entry row in readCSV.OfType<Entry>().Where(iss => iss.IssueID != 0).Where(n => n.StaffName == name))
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
