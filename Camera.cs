using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using System.Threading;
using System.IO;
using System.Windows.Forms;

namespace LoadCell_OwnProgram
{
    internal class Camera
    {
        static int ConfigGVCPHeartBeat(IManagedCamera cam, bool enable)
        {
            INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();

            // Retrieve GenICam nodemap
            INodeMap nodeMap = cam.GetNodeMap();

            IEnum iDeviceType = nodeMapTLDevice.GetNode<IEnum>("DeviceType");
            IEnumEntry iDeviceTypeGEV = iDeviceType.GetEntryByName("GigEVision");
            // We first need to confirm that we're working with a GEV camera
            if (iDeviceType != null && iDeviceType.IsReadable)
            {
                if (iDeviceType.Value == iDeviceTypeGEV.Value)
                {
                    if (enable)
                    {
                        MessageBox.Show("Resetting heartbeat");
                    }
                    else
                    {
                        MessageBox.Show("Disabling heartbeat");
                    }
                    IBool iGEVHeartbeatDisable = nodeMap.GetNode<IBool>("GevGVCPHeartbeatDisable");
                    if (iGEVHeartbeatDisable == null || !iGEVHeartbeatDisable.IsWritable)
                    {
                        MessageBox.Show("Unable to disable heartbeat on camera. Continuing with execution as this may be non-fatal...");
                    }
                    else
                    {
                        iGEVHeartbeatDisable.Value = enable;

                        if (!enable)
                        {
                            MessageBox.Show("WARNING: Heartbeat has been disabled for the rest of this example run.");
                            MessageBox.Show("         Heartbeat will be reset upon the completion of this run.  If the ");
                            MessageBox.Show("         example is aborted unexpectedly before the heartbeat is reset, the");
                            MessageBox.Show("         camera may need to be power cycled to reset the heartbeat.\n");
                        }
                        else
                        {
                            MessageBox.Show("Heartbeat has been reset.\n");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Unable to access TL device nodemap. Aborting...");
                return -1;
            }

            return 0;

        }

        static int ResetGVCPHeartbeat(IManagedCamera cam)
        {
            return ConfigGVCPHeartBeat(cam, true);
        }
        static int DisableGVCPHeartbeat(IManagedCamera cam)
        {
            return ConfigGVCPHeartBeat(cam, false);
        }

        static int AcquireImage(IManagedCamera cam, INodeMap nodeMap, INodeMap nodeMapTLDevice, Boolean whileCheck)
        {
            int result = 0;
            MessageBox.Show("About To Start Image Accquistion");

            try
            {
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable || !iAcquisitionMode.IsReadable)
                {
                    MessageBox.Show("Unable to set acquisition mode to continuous (node retrieval). Aborting...");
                    return -1;
                }

                // Retrieve entry node from enumeration node
                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                if (iAcquisitionModeContinuous == null || !iAcquisitionModeContinuous.IsReadable)
                {
                    MessageBox.Show("Unable to set acquisition mode to continuous (enum entry retrieval). Aborting...");
                    return -1;
                }

                // Set symbolic from entry node as new value for enumeration node
                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                MessageBox.Show("Acquisition mode set to continuous...");

                cam.BeginAcquisition();

                MessageBox.Show("Acquiring images...");

                String deviceSerialNumber = "";

                IString iDeviceSerialNumber = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");
                if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
                {
                    deviceSerialNumber = iDeviceSerialNumber.Value;

                    MessageBox.Show("Device serial number retrieved as {0}...", deviceSerialNumber);
                }
                IManagedImageProcessor processor = new ManagedImageProcessor();
                processor.SetColorProcessing(ColorProcessingAlgorithm.HQ_LINEAR);

                int imageCnt = 0;
                while (whileCheck == false)
                {
                    try
                    {
                        using (IManagedImage rawImage = cam.GetNextImage(1000))
                        {
                            if (rawImage.IsIncomplete)
                            {
                                MessageBox.Show("Image was incomplete: " + rawImage.ImageStatus);
                            }
                            else
                            {
                                uint width = rawImage.Width;
                                uint height = rawImage.Height;
                                using (IManagedImage convertedImage = processor.Convert(rawImage, PixelFormatEnums.Mono8))
                                {
                                    String filename = "Accquistion C-Sharp";
                                    if (deviceSerialNumber != "")
                                    {
                                        filename = filename + deviceSerialNumber + "-";
                                    }
                                    filename = filename + imageCnt + ".jpg";
                                    convertedImage.Save(filename);
                                }

                            }
                        }
                    }
                    catch (SpinnakerException ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                        result = -1;
                    }
                    Thread.Sleep(5000);
                    imageCnt++;
                }
                cam.EndAcquisition();
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                result = -1;
            }
            return result;
        }


    }
}