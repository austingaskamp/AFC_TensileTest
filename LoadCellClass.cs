using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoadCell_OwnProgram
{
    public class LoadCellClass
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
        public void LoadCell(decimal width, decimal thick, DateTime FirstExportTime)
        {
            FUTEK_USB_DLL.USB_DLL futek = new FUTEK_USB_DLL.USB_DLL();
            // Connect to load cell
            futek.Open_Device_Connection("538827");
            DeviceStatus = futek.DeviceStatus;
            if (DeviceStatus == 0)
            {
            }
            DeviceHandle = futek.DeviceHandle;
            int count = 0;

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
                count += 1;

                TimeSpan timeSinceFirstExport = DateTime.Now - FirstExportTime;//total time elapsed since first data point
                //Console.WriteLine(timeSinceFirstExport);
            }
        }
    }
}
