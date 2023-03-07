using System;
using System.Windows.Forms;
using System.Threading;
using FUTEK_USB_DLL;
using Zaber.Motion;
using Zaber.Motion.Ascii;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using Spinnaker;
using System.IO;
using System.Collections.Generic;
using Zaber.Motion.Binary;
using System.Threading.Tasks;




namespace LoadCell_OwnProgram
{
    public partial class MainForm : Form
    {
        private IntPtr DeviceHandle;
        private int DeviceStatus;
        private string t_Off_Val;
        private string t_Full_Val;
        private string t_FullLoad_Val;
        private string t_Deci_Point;
        private string t_NormData;
        private string t_UnitCode;
        public Int32 UnitCode;
        public Int32 OffsetVal;
        public Int32 FullVal;
        public Int32 FullLoadVal;
        public Int32 DeciPoint;
        public Int32 NormalVal;
        public Double CalcVal;
        public Int32 act_count;
        public StreamWriter writer;
        private bool exportFlag = true;
        private decimal lastReading = 0;

        //making it so that Initialize() doesn't run until after we press initialize button
        public bool initializeButtonPressed = false;

        //defining geometries
        public decimal length = 5;
        public decimal width = 1;
        public decimal thick = 1;

        //intaking strain rate and converting that to linear velocity in um/s
        public decimal strainrate = 0.001m; // default is 10^-3 [1/s]

        //Acquisition Rate
        private decimal acq_rate = 0.2m; //default acquisiton rate set to 0.2 Hz

        //This method is the very beginning of me trying to move the 'export data' functionality into it's own method. Code should run fine with this commented out
        //Will need to add this thread / call this thread in "Start Button" area
        private void ExportData()
        {
            // Create a new thread to run the data export loop
            Thread exportThread = new Thread(() =>
            {
                // Open the file for writing
                using (var writer = new StreamWriter("data.csv"))
                {
                    // Write the headers to the file
                    writer.WriteLine($"'Time', 'Force [N]', 'Stress [MPa]'"); //What the .csv is actually 
                    Console.WriteLine("THIS LINE RAN");

                    // Export data until the export flag is set to false
                    while (exportFlag)
                    {
                        // Get the current time and load
                        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        var load = (float)lastReading;

                        // Write the data to the file
                        writer.WriteLine($"{time},{load}");

                        // Wait for the specified interval before exporting the next data point
                        Thread.Sleep(200);
                    }
                    writer.Flush();
                    writer.Close();
                }
            });

            // Start the export thread
            exportThread.Start();
        }

        //Main method. Activated when start button is pressed. Connects and reads in load cell data, outputs time, stress, and force to GUI, exports data to .csv
        private void Running()
        {
            FUTEK_USB_DLL.USB_DLL futek = new FUTEK_USB_DLL.USB_DLL();
            // Connect to load cell
            futek.Open_Device_Connection("538827");
            DeviceStatus = futek.DeviceStatus;
            if (DeviceStatus == 0)
            {
            }
            DeviceHandle = futek.DeviceHandle;

            //Creating .csv file to write output data to 
            //writer = new StreamWriter("data.csv"); // Create a new StreamWriter object to write to the CSV file
            //writer.WriteLine($"'Time', 'Force [N]', 'Stress [MPa]'"); //What the .csv is actually 
            bool isFirstExport = true; //This is so that it exports the data at time 0

            // Initialize the first and most recent export time
            DateTime lastExportTime = DateTime.Now;
            DateTime firstExportTime = DateTime.Now;

            while (true) //main loop that runs the functions
            {
                //Load cell initialization
                t_Off_Val = futek.Get_Offset_Value(DeviceHandle);
                OffsetVal = Int32.Parse(t_Off_Val);
                t_Full_Val = futek.Get_Fullscale_Value(DeviceHandle);
                FullVal = Int32.Parse(t_Full_Val);
                t_FullLoad_Val = futek.Get_Fullscale_Load(DeviceHandle);
                FullLoadVal = Int32.Parse(t_FullLoad_Val);
                t_Deci_Point = futek.Get_Decimal_Point(DeviceHandle);
                DeciPoint = Int32.Parse(t_Deci_Point);
                t_NormData = futek.Normal_Data_Request(DeviceHandle);
                NormalVal = Int32.Parse(t_NormData);
                t_UnitCode = futek.Get_Unit_Code(DeviceHandle);
                UnitCode = Int32.Parse(t_UnitCode);

                //Calculate the force in lbf from load cell 
                CalcVal = (double)(NormalVal - OffsetVal) / (FullVal - OffsetVal) * FullLoadVal / Math.Pow(10, DeciPoint);
                decimal NewtForce = Convert.ToDecimal(CalcVal) * Convert.ToDecimal(4.4482189159); //Convert to Newton from lbf
                decimal stress = Convert.ToDecimal(NewtForce) / (Convert.ToDecimal(width) * Convert.ToDecimal(thick)); //Convert from Newtons to MPa

                //Calculating time delay based on acquistion rate, calcualting total time elapsed and delta 
                int time_between_data = (int)(1 / Convert.ToDecimal(acq_rate));//Calculating sleep time based on user entered acquisition rate
                TimeSpan timeSinceFirstExport = DateTime.Now - firstExportTime;//total time elapsed since first data point
                TimeSpan timeSinceLastExport = DateTime.Now - lastExportTime;//time elapsed between data points

                //Print Time in s to GUI
                if (InvokeRequired)
                {
                    Invoke(new Action(() => TimeOutput.Text = timeSinceFirstExport.ToString("mm\\:ss\\.ff")));
                }
                else
                {
                    TimeOutput.Text = timeSinceFirstExport.ToString("mm\\:ss\\.ff");
                }

                //Print Force in Newtons to GUI
                if (InvokeRequired)
                {
                    Invoke(new Action(() => ResultsLab.Text = Convert.ToDecimal(NewtForce).ToString("n2")));
                }
                else
                {
                    ResultsLab.Text = Convert.ToDecimal(NewtForce).ToString("n2");
                }
                //Print Stress in MPa to GUI
                if (InvokeRequired)
                {
                    Invoke(new Action(() => StressOutput.Text = stress.ToString("n2")));
                }
                else
                {
                    StressOutput.Text = stress.ToString("n2");
                }

                // Export to CSV file every 5 seconds
                if (timeSinceLastExport.TotalSeconds >= time_between_data || isFirstExport) //added if statement so that it starts at 0 rather than 5
                {
                    //writer.WriteLine($"{timeSinceFirstExport}, {NewtForce}, {stress}"); //What the .csv is actually writing
                    //Console.WriteLine("timeSinceFirstExport " + timeSinceFirstExport);
                    //lastExportTime = DateTime.Now;
                    //isFirstExport = false; //so that it will take a data point at time 0. without this line it waits until after the first cycle so like t=5s is first data point
                }
                
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------


        //Initialize Button. Moves grip to home position and runs "Initialize" thread from testing.cs
        private void InitializeButton_Click(object sender, EventArgs e)
        {
            //Disable initialization button
            InitializeButton.Enabled = false;
            //Enable start button
            StartButton.Enabled = true;
            //Calls on Initialize from TestingClass
            TestingClass initProcess = new TestingClass();
            initProcess.Initialize();
        }


        //Start Button. Runs RunningTest and StoringData from TestingClass.cs
        private void StartButton_Click(object sender, EventArgs e)
        {
            // Disable the start button
            StartButton.Enabled = false;

            // Create a new instance of the TestingClass
            TestingClass testingClass = new TestingClass();

            // Create two separate tasks to run StoringData and RunningTest
            Task storingDataTask = Task.Run(() => testingClass.StoringData());
            Task runningTestTask = Task.Run(() => testingClass.RunningTest());

        }

        //Stop Button.
        private void StopButton_Click(object sender, EventArgs e)
        {
            //closes csv file
            exportFlag = false;

            //closes GUI
            this.Close();
            Application.Exit();
        }


        //this next part only matters if the user makes a change, otherwise it is just the above three lines 
        public void numericUpDown1_ValueChanged(object sender, EventArgs e)//taking user input to determine gauge length
        {
            length = (decimal)numericUpDown1.Value;
        }
        public void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            width = (decimal)numericUpDown2.Value;
        }
        public void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            thick = (decimal)numericUpDown3.Value;
        }

        public void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            strainrate = (decimal)numericUpDown4.Value;
        }




        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e) //pointless - move to end
        {

        }

        private void label2_Click(object sender, EventArgs e) //pointless, you wont click on the label
        {

        }
        private void label6_Click(object sender, EventArgs e) //pointless - move to end
        {

        }

        private void ResultsLab_Click(object sender, EventArgs e) //pointless - move to end
        {

        }

        //Acquisition Rate
        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            acq_rate = (decimal)numericUpDown5.Value; //if user input is entered then this changes acq rate variable
        }

        //trying to make the .csv file able to be renamed / relocated
        private void saveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "CSV file (*.csv)|*.csv|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = saveFileDialog1.FileName;
                // Save the file with the chosen file name and location
            }

        }
        //Time output to GUI
        private void TimeOutput_Click(object sender, EventArgs e)
        {

        }

        //Displacement Output
        private void DispOutput_Click(object sender, EventArgs e)
        {

        }
    }
}
