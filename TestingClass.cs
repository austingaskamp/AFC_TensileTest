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
using System.Threading.Tasks; //trying to make it so both can run concurrently


namespace LoadCell_OwnProgram
{
    public class TestingClass
    {
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

        //Called upon whenever the initialize button is pressed. Connects to actuator, tells it to move to home position, and prompts user to load sample. 
        public void Initialize()
        {
            using (var connection = Zaber.Motion.Ascii.Connection.OpenSerialPort("COM8")) //change this based on what computer you're using - port that your controller is connected to 
            {
                connection.EnableAlerts(); //connecting to actuator
                var deviceList = connection.DetectDevices();
                Console.WriteLine($"Found {deviceList.Length} devices.");
                var device = deviceList[0];
                var axis = device.GetAxis(1);
                if (!axis.IsHomed())
                {
                    axis.Home();
                }
                axis.MoveAbsolute(60, Units.Length_Millimetres);
                MessageBox.Show("Grips are now at home position. Load the sample and press Start button when you are ready to begin test.");

            }
        }


        //Creating an instance of ActuatorClass, LoadCellClass within TestingClass
        private ActuatorClass actuator = new ActuatorClass();
        private LoadCellClass loadCell = new LoadCellClass();
        //Making it a global variable so that LoadCellClass can reference it
        public DateTime FirstExportTime { get; private set; }

        //Called upon whenever the start button is pressed. 
        public void RunningTest()
        {
            DateTime FirstExportTime = DateTime.Now;//Timer starts when button is pressed
            Task loadCellTask = Task.Run(() => loadCell.LoadCell(width, thick, FirstExportTime));
            Task actuatorTask = Task.Run(() => actuator.LinearActuator(length, strainrate));

        }
        public void StoringData()
        {
            // Create a new timer with a period of 5000 milliseconds
            var timer = new System.Threading.Timer(
                callback: (_) =>
                {
                    // This code will run every 5000 milliseconds
                    Console.WriteLine("this RAN");
                },
                state: null,
                dueTime: 0,
                period: 5000);
            var stopped = new ManualResetEvent(false); // Wait for the timer to stop before returning. This will keep the StoringData method running until the timer is stopped
            stopped.WaitOne();
            timer.Dispose(); // Dispose the timer to free up resources
        }






        //Called upon whenever the stop button is pressed.
        public void StoppingTest()
        {
            Console.WriteLine("The test has been stopped");
        }
    
        
    }
}
