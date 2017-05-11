using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using LibUsbDotNet.Info;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.WinUsb;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;




namespace WindowsFormsApplication4
{
    public partial class Form1 : Form
    {
        static SSD2828 bridge_setting = new SSD2828();
        public bool bUSBConnect = false;
        static MIPI_DPHY mipi = new MIPI_DPHY(8, 8, 16, 1920, 80, 80, 80, 1080, 60, 24, 4);
        public static UsbDevice MyUsbDevice;
        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x1FC9, 0x008A);
        public static string strMIPIBRGInitFile = "";
        public static string strDDIInitFile = "";
        public static int FolderCnt, SDheight, SDwidth;
        public static int[] File_index = new int[2048];
        public static char[,] folder_name = new char[2048, 16];
        public static byte[,] SDCARD_ROOTDATA = new byte[2048, 20];
        public static byte[,,] SDCARD_DATA = new byte[2048, 2048, 20];
        public static byte[] PixelBuffer;
        public bool SI_SUCCESS = true;
        public static string[] strAPIName = new string[] { "", "", "", "", "",
                                                           "", "", "", "", "" };

        public static string[] strParameters = new string[] { "", "", "", "", "",
                                                              "", "", "", "", ""};
        

        enum SD_Para
        {
            cFolder = 0,
            cFile,
        };

        enum Schedule
        {
            IDEL_MSG = 0x00,
            GETFWINFO_MSG,
            READPIC_MSG,
            READPICDATE_MSG,
            WRITEPIC_MSG,
            WRITEPICDATE_MSG,
            FPGA_PROG_MSG,
            FPGA_PROGDATA_MSG,
            READREG_MSG,
            WRITEREG_MSG,
            INTERFACE_MSG,
            DEVICE_READ_MSG,
            DEVICE_WRITE_MSG,
            BRG_RESET_MSG,
            Bridge_Initial_MSG,
            PLL1_Config_MSG,
            PLL2_Config_MSG,
            POWERPIN_MSG,
            POWERPINSCAN_MSG,
            FPGARESET_MSG,
            PANELRESET_MSG,
            SDINI_MSG,
            SD_SEARCH_MSG,
            SD_SUBSEARCH_MSG,
            SD_PICSEARCH_MSG,
            SDLOADPIC_MSG,
            BRG168_WRITEREG_MSG,
            BRG268_WRITEREG_MSG,
            BRG168_READREG_MSG,
            BRG268_READREG_MSG,
            POWERWRITE_MSG,
            POWERREAD_MSG,
            READ_VGH_MSG,
            CS_MSG,
            SPIPINDEF_MSG,
            POWERSEQUENCE_MSG,
            ADINI_MSG,
            CS_LEVEL_MSG,
            CS_TOGGLE_MSG,
            RELAYCTRL_MSG,
            I2C_CLOCKSEL_MSG,
            IO0_MSG,
            SD_init_MSG,                    //0x2A
            SD_Read_1st_MSG,                //0x2B
            SD_Read_2nd_MSG,                //0x2C
            SD_list_MSG,                    //0x2D
            SD_list_1st_MSG,                //0x2E
            CORE_RST_MSG,                   //0x2F
            SD_CHANGE_DIR_MSG,              //0x30
            IO1_MSG,
            TEST_MSG,
            TPPOWERON_MSG,
            TPPAINT_MSG,
            TPREADPOINT_MSG,
            CDCSTART_MSG,
            CDCINIT_MSG,
            Delay_MSG,
            SD_SEARCH1_MSG,
            SD_SUBSEARCH2_MSG,

        };

        enum Interface
        {
            IF_DEFSPI = 0x00,
            IF_FPGA,
            IF_SSDSPI3W1,
            IF_SSDMCU24BIT1,
            IF_SSDSPI3W2,
            IF_SSDMCU24BIT2,
            IF_I2C,
            IF_SPI3W9BIT,
            IF_SPI4W8BIT,
        };

        static class Constants
        {
            public const string SW_VERSION = "ILITEK_LPC1857_SYSTEM V1.0";
            public const byte FW_VERSION = 0x07;
            public const int BufSize = 4096;
        }

        public void PictureLoadtoPanel2()
        {
            UInt32 PixelCnt = 54;
            int Y, X;            
            progressBar1.Maximum = SDheight;
            progressBar1.Value = 0;
            progressBar1.Step = 1;
            pictureBox2.Image = null;

            int R_Data, G_Data, B_Data;
            Bitmap myBitmap = new Bitmap(SDwidth, SDheight, PixelFormat.Format24bppRgb);


            for (Y = (SDheight - 1); Y >= 0; Y--)
            {
                for (X = 0; X < SDwidth; X++)
                {
                    B_Data = PixelBuffer[PixelCnt];
                    G_Data = PixelBuffer[PixelCnt + 1];
                    R_Data = PixelBuffer[PixelCnt + 2];

                    myBitmap.SetPixel(X, Y, Color.FromArgb(R_Data, G_Data, B_Data));
                    PixelCnt += 3;
                }
                progressBar1.Value += progressBar1.Step;
            }
            pictureBox2.Image = myBitmap;

        }


        public void PictureLoadtoPanel()
        {
            UInt32 PixelCnt = 54;
            int Y, X;


            progressBar1.Maximum = SDheight;
            progressBar1.Value = 0;
            progressBar1.Step = 1;
            pictureBox2.Image = null;


            Bitmap source = new Bitmap(SDwidth, SDheight, PixelFormat.Format24bppRgb);

            BitmapData sourceData = source.LockBits(new Rectangle(0, 0, SDwidth, SDheight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            IntPtr source_scan = sourceData.Scan0;

            byte RData, GData, BData;

            unsafe
            {
                byte* source_p = (byte*)source_scan.ToPointer();

                for (Y = 0; Y < SDheight; Y++)
                {
                    for (X = 0; X < SDwidth; X++)
                    {
                        BData = PixelBuffer[PixelCnt];
                        GData = PixelBuffer[PixelCnt + 1];
                        RData = PixelBuffer[PixelCnt + 2];

                        source_p[0] = BData;
                        source_p++;
                        source_p[0] = GData;
                        source_p++;
                        source_p[0] = RData;
                        source_p++;
                        PixelCnt += 3;
                    }
                    progressBar1.Value += progressBar1.Step;
                }
            }
            source.UnlockBits(sourceData);

            source.RotateFlip(RotateFlipType.Rotate180FlipX);

            pictureBox2.Image = source;

        }

        public void ReadPixelData(UInt32 pixelSize)
        {
            UInt32 BufIndex;
            byte[] WriteBuffer = new byte[Constants.BufSize];
            byte[] ReadBuffer = new byte[Constants.BufSize];
            char[] CharTemp = new char[5];
            int BuffCNT = 0;

            for (BufIndex = 0; BufIndex < pixelSize; BufIndex += Constants.BufSize)
            {
                if (BufIndex == 0)
                {
                    System.Threading.Thread.Sleep(50);
                    if (DeviceRead(Constants.BufSize, ReadBuffer))
                    {
                        if (ReadBuffer[0] == 0x99)
                        {
                            MessageBox.Show("open error!");
                            brnLoadPic.Enabled = true;
                            UsbDevice.Exit();
                            return;

                        }
                        else if (ReadBuffer[0] == 0xAA)
                        {
                            MessageBox.Show("read error!");
                            brnLoadPic.Enabled = true;
                            UsbDevice.Exit();
                            return;

                        }
                        else
                        {
                            SDwidth = ReadBuffer[21] << 24 | ReadBuffer[20] << 16 | ReadBuffer[19] << 8 | ReadBuffer[18] << 0;
                            SDheight = ReadBuffer[25] << 24 | ReadBuffer[24] << 16 | ReadBuffer[23] << 8 | ReadBuffer[22] << 0;
                            if (ReadBuffer[28] != 24)
                            {
                                MessageBox.Show("Only pixel 24 bit is supported!");
                                brnLoadPic.Enabled = true;
                                UsbDevice.Exit();
                                return;
                            }
                            for (int i = 0; i < Constants.BufSize; i++)
                            {
                                PixelBuffer[BufIndex + i] = ReadBuffer[i];

                            }

                        }
                    }
                    else
                    {
                        MessageBox.Show("Read Error.");
                        brnLoadPic.Enabled = true;
                        UsbDevice.Exit();
                        return;
                    }

                }
                else if ((BufIndex + Constants.BufSize) < pixelSize)
                {
                    WriteBuffer[0] = (byte)Schedule.SD_Read_2nd_MSG;
                    if (DeviceWrite(Constants.BufSize, WriteBuffer))
                    {
                        if (DeviceRead(Constants.BufSize, ReadBuffer))
                        {

                            for (int i = 0; i < 5; i++)
                            {
                                CharTemp[i] = (char)ReadBuffer[i];
                            }

                            if (CharTemp.ToString().IndexOf("ERROR") != -1)
                            {
                                MessageBox.Show(CharTemp.ToString());

                            }
                            else
                            {

                                for (int i = 0; i < Constants.BufSize; i++)
                                {

                                    PixelBuffer[BufIndex + i] = ReadBuffer[i];

                                }
                            }

                        }
                        else
                        {
                            MessageBox.Show("Read Error.");
                            brnLoadPic.Enabled = true;
                            UsbDevice.Exit();
                            return;

                        }

                    }
                    else
                    {
                        MessageBox.Show("Write Error.");
                        brnLoadPic.Enabled = true;
                        UsbDevice.Exit();
                        return;
                    }

                }

            }

            BuffCNT = (int)(pixelSize % Constants.BufSize);

            if (BuffCNT != 0)
            {

                WriteBuffer[0] = (byte)Schedule.SD_Read_2nd_MSG;
                if (DeviceWrite(Constants.BufSize, WriteBuffer))
                {
                    if (DeviceRead(Constants.BufSize, ReadBuffer))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            CharTemp[i] = (char)ReadBuffer[i];
                        }

                        if (CharTemp.ToString().IndexOf("ERROR") != -1)
                        {
                            MessageBox.Show(CharTemp.ToString());

                        }
                        else
                        {
                            BufIndex -= Constants.BufSize;
                            for (int i = 0; i < BuffCNT; i++)
                            {

                                PixelBuffer[BufIndex++] = ReadBuffer[i];

                            }

                        }

                    }
                    else
                    {
                        MessageBox.Show("Read Error.");
                        brnLoadPic.Enabled = true;
                        UsbDevice.Exit();
                        return;

                    }

                }
                else
                {


                    MessageBox.Show("Write Error.");
                    brnLoadPic.Enabled = true;
                    UsbDevice.Exit();
                    return;

                }
            }


        }

        public void MCU_Reset()
        {
            byte[] writeBuffer = new byte[Constants.BufSize];

            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return;
            }

            writeBuffer[0] = (byte)Schedule.CORE_RST_MSG;

            if (!DeviceWrite(Constants.BufSize, writeBuffer))
            {
                MessageBox.Show("Write Error.");
                UsbDevice.Exit();
            }


            System.Threading.Thread.Sleep(500);

        }


        public bool DeviceOpen()
        {
            MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);
            bool Status = true;

            // If the device is open and ready
            if (MyUsbDevice == null)
            {
                MessageBox.Show("Device Not Found.");
                Status = false;
            }
            else
            {
                IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.

                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);

                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);


                }
                Status = true;
            }

            return Status;

        }

        public bool DeviceWrite(int Size, byte[] Data)
        {
            int bytesWritten;
            ErrorCode ec = ErrorCode.None;
            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return false;
             
            }
            // open write endpoint 1.
            UsbEndpointWriter writer = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);

            ec = writer.Write(Data, 2000, out bytesWritten);

            if (ec != ErrorCode.None)
                return false;
            else
                return true;

        }

        bool DeviceRead(int Size, byte[] Data)
        {
            ErrorCode ec = ErrorCode.None;
            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return false;

            }
            // open read endpoint 1.
            UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);            
            int bytesRead;
            // If the device hasn't sent data in the last 1000 milliseconds,
            // a timeout error (ec = IoTimedOut) will occur. 
            ec = reader.Read(Data, 2000, out bytesRead);
            ec = reader.Read(Data, 2000, out bytesRead);
            if (ec != ErrorCode.None)
                return false;
            else
                return true;

        }



        public Form1()
        {
            InitializeComponent();
        }



        private void btnUSBConn_Click(object sender, EventArgs e)
        {
            ErrorCode ec = ErrorCode.None;
            byte[] writeBuffer = new byte[Constants.BufSize];
            byte[] readBuffer = new byte[Constants.BufSize];


            try
            {
                // Find and open the usb device.
                MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);


                // If the device is open and ready
                if (MyUsbDevice == null)
                    throw new Exception("Device Not Found.");


                IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.

                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);

                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);
                }

                // open read endpoint 1.
                UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

                // open write endpoint 1.
                UsbEndpointWriter writer = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);

                int bytesWritten;
                writeBuffer[0] = (byte)Schedule.GETFWINFO_MSG;
                ec = writer.Write(writeBuffer, 2000, out bytesWritten);

                if (ec != ErrorCode.None)
                    throw new Exception("USB Connected Fail");

                //System.Threading.Thread.Sleep(50);

                int bytesRead;
                // If the device hasn't sent data in the last 1000 milliseconds,
                // a timeout error (ec = IoTimedOut) will occur. 
                ec = reader.Read(readBuffer, 1000, out bytesRead);
                ec = reader.Read(readBuffer, 1000, out bytesRead);

                if (readBuffer[0] == Constants.FW_VERSION)
                    Form1.ActiveForm.Text = Constants.SW_VERSION + "    F/W = Ver " + Constants.FW_VERSION;
                else
                    Form1.ActiveForm.Text = Constants.SW_VERSION + "    Status：F/W = Ver " + readBuffer[0] + "，  Please update F/W = Ver " + Constants.FW_VERSION;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (MyUsbDevice != null)
                {
                    if (MyUsbDevice.IsOpen)
                    {
                        // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                        // it exposes an IUsbDevice interface. If not (WinUSB) the 
                        // 'wholeUsbDevice' variable will be null indicating this is 
                        // an interface of a device; it does not require or support 
                        // configuration and interface selection.
                        IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                        if (!ReferenceEquals(wholeUsbDevice, null))
                        {
                            // Release interface #0.
                            wholeUsbDevice.ReleaseInterface(0);
                        }

                        MyUsbDevice.Close();
                    }
                    MyUsbDevice = null;

                    // Free usb resources
                    UsbDevice.Exit();

                }
            }

        }

        private void btnSDIni_Click(object sender, EventArgs e)
        {

            btnSDIni.Enabled = false;
            MCU_Reset();


            byte[] writeBuffer = new byte[Constants.BufSize];
            byte[] readBuffer = new byte[Constants.BufSize];


            Array.Clear(readBuffer, 0, Constants.BufSize);
            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return;
            }

            writeBuffer[0] = (byte)Schedule.SD_init_MSG;

            if (DeviceWrite(Constants.BufSize, writeBuffer))
            {

                for (int readindex = 0; readindex < 10; readindex++)
                {

                    System.Threading.Thread.Sleep(10);
                    DeviceRead(Constants.BufSize, readBuffer);
                    if (readBuffer[1] == 0x11)
                    {
                        if (readBuffer[2] == 0x22)
                        {
                            if (readBuffer[3] == 0x33)
                            {
                                if (readBuffer[4] == 0x44)
                                {
                                    break;
                                }
                            }
                        }
                    }


                }

            }
            else
            {
                MessageBox.Show("Write Error.");
                UsbDevice.Exit();
            }

            btnSDIni.Enabled = true;

            UsbDevice.Exit();




        }





        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            TreeNode parent = node.Parent;

            byte[] BMPSize = new byte[4];
            Array.Clear(BMPSize, 0, 4);
            //int test = node.Index;

            if (node.ImageIndex == (int)SD_Para.cFolder)
            {
                labBMPLenData.Text = "None";
            }
            else if (node.ImageIndex == (int)SD_Para.cFile)
            {
                if (node.Level == 1)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        BMPSize[i] = SDCARD_ROOTDATA[node.Index, i + 13];
                        labBMPLenData.Text = ((BMPSize[3] * 0x1000000 + BMPSize[2] * 0x10000 + BMPSize[1] * 0x100 + BMPSize[0]) / 1024).ToString();
                    }
                }
                else if (node.Level == 2)
                {

                    for (int FolderIndex = 0; FolderIndex < FolderCnt; FolderIndex++)
                    {
                        string StrTemp = null;
                        for (int i = 0; i < 8; i++)
                        {
                            StrTemp += folder_name[FolderIndex, i];
                        }


                        if (parent.Text.IndexOf(StrTemp) != -1)
                        {

                            for (int i = 0; i < 4; i++)
                            {
                                BMPSize[i] = SDCARD_DATA[FolderIndex, node.Index, i];
                            }

                            labBMPLenData.Text = ((BMPSize[3] * 0x1000000 + BMPSize[2] * 0x10000 + BMPSize[1] * 0x100 + BMPSize[0]) / 1024).ToString();
                        }

                    }
                }

            }



        }

        private void btnSolomonWriteBridge_Click(object sender, EventArgs e)
        {
            byte[] writeBuffer = new byte[Constants.BufSize];
            int Addr, Data;

            Array.Clear(writeBuffer, 0, Constants.BufSize);


            Addr = int.Parse(edtSolomonBridgeAddr.Text, System.Globalization.NumberStyles.HexNumber);
            Data = int.Parse(edtSolomonBridgePara.Text, System.Globalization.NumberStyles.HexNumber);

            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return;
            }


            writeBuffer[0] = (byte)Schedule.WRITEREG_MSG;
            writeBuffer[1] = 0x02;
            writeBuffer[2] = 0x00;
            writeBuffer[3] = 0x00;
            writeBuffer[4] = 0x00;
            writeBuffer[5] = (byte)Addr;
            writeBuffer[10] = (byte)(Data & 0xFF);
            writeBuffer[11] = (byte)((Data >> 8) & 0xFF);

            if (Bridge1_Sel.Checked)
            {
                if (DSI_Command_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT1;
                else if (DSI_Video_Mode_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W1;

            }
            else if (Bridge2_Sel.Checked)
            {
                if (DSI_Command_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT2;
                else if (DSI_Video_Mode_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W2;

            }
            else
            {
                if (DSI_Command_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT1;
                else if (DSI_Video_Mode_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W1;
                if (!DeviceWrite(Constants.BufSize, writeBuffer))
                {
                    MessageBox.Show("Write Error.");
                    UsbDevice.Exit();
                    return;
                }
                if (DSI_Command_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT2;
                else if (DSI_Video_Mode_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W2;

            }


            if (DeviceWrite(Constants.BufSize, writeBuffer))
            {
               
                SSD2828TextBox.Text += "WriteToBridge\t[" + edtSolomonBridgeAddr.Text + "H] = 0x" + edtSolomonBridgePara.Text + "\n";
               
            }
            else
            {
                MessageBox.Show("Write Error.");
                UsbDevice.Exit();

            }

            UsbDevice.Exit();
        }

        private void bbtnPanel_Click(object sender, EventArgs e)
        {
            byte[] writeBuffer = new byte[Constants.BufSize];

            Array.Clear(writeBuffer, 0, Constants.BufSize);
            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return;
            }

            writeBuffer[0] = (byte)Schedule.PANELRESET_MSG;

            if (!DeviceWrite(Constants.BufSize, writeBuffer))
            {
                MessageBox.Show("Write Error.");
                UsbDevice.Exit();

            }

            UsbDevice.Exit();

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            bridge_setting.Set_Command_mode();
            bridge_setting.Select_Bridge_1();
            //WriteBridge(0xb1, 0x1234, bridge_setting);
            if (!DeviceWrite(Constants.BufSize, bridge_setting.Write_Bridge(0xb1 , 0x1234)))
            {

                MessageBox.Show("Write Error.");
                UsbDevice.Exit();

            }

            UsbDevice.Exit();

        }

        private void SSD2828TextBox_TextChanged(object sender, EventArgs e)
        {
            SSD2828TextBox.SelectionStart = SSD2828TextBox.TextLength;
            SSD2828TextBox.ScrollToCaret();
        }

        private void SSD2828TextBoxClear_Click(object sender, EventArgs e)
        {
            SSD2828TextBox.Clear();
        }

        private void btnSolomonReadBridge_Click(object sender, EventArgs e)
        {
            byte[] writeBuffer = new byte[Constants.BufSize];
            byte[] readBuffer = new byte[Constants.BufSize];
            int Addr, Data;

            Array.Clear(writeBuffer, 0, Constants.BufSize);
            Array.Clear(writeBuffer, 0, Constants.BufSize);


            Addr = int.Parse(edtSolomonBridgeAddr.Text, System.Globalization.NumberStyles.HexNumber);
          

            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return;
            }


            writeBuffer[0] = (byte)Schedule.READREG_MSG;
            writeBuffer[1] = 0x02;
            writeBuffer[2] = 0x00;
            writeBuffer[3] = 0x00;
            writeBuffer[4] = 0x00;
            writeBuffer[5] = (byte)Addr;
           

            if (Bridge1_Sel.Checked)
            {
                if (DSI_Command_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT1;
                else if (DSI_Video_Mode_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W1;

            }
            else if (Bridge2_Sel.Checked)
            {
                if (DSI_Command_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT2;
                else if (DSI_Video_Mode_SEL.Checked)
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W2;

            }

            

            if (DeviceWrite(Constants.BufSize, writeBuffer))
            {
                System.Threading.Thread.Sleep(50);
                if(DeviceRead(Constants.BufSize, readBuffer))
                {
                    
                    edtSolomonBridgePara.Text = (readBuffer[1] * 0x100 + readBuffer[0]).ToString("X4");
                    SSD2828TextBox.Text += "Read_Bridge\t[" + edtSolomonBridgeAddr.Text + "H] = 0x" + edtSolomonBridgePara.Text;
       
                }
                else
                {
                    MessageBox.Show("Write Error.");
                    UsbDevice.Exit();
                    return;

                }
                SSD2828TextBox.Text += "WriteToBridge\t[" + edtSolomonBridgeAddr.Text + "H] = 0x" + edtSolomonBridgePara.Text + "\n";

            }
            else
            {
                MessageBox.Show("Write Error.");
                UsbDevice.Exit();
                return;

            }

            UsbDevice.Exit();
        }



        private void bbtnMIPIBRGSeting_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Txt";
            dlg.Filter = "txt files (*.txt)|*.txt";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                strMIPIBRGInitFile = dlg.FileNames.GetValue(0).ToString();
            }
            else
            {

                strMIPIBRGInitFile = "";
            }

            m_txtbMIPIBRGInitFile.Text = strMIPIBRGInitFile;

            dlg.Dispose();

            if (strMIPIBRGInitFile.Length > 50)
            {
                m_txtbMIPIBRGInitFile.Text = strMIPIBRGInitFile.Substring(0, 20) + "..." + strMIPIBRGInitFile.Substring(strMIPIBRGInitFile.Length - 30, 30);
            }
            else
            {
                m_txtbMIPIBRGInitFile.Text = strMIPIBRGInitFile;
            }
        }

        private void btnDriverSeting_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Txt";
            dlg.Filter = "txt files (*.txt)|*.txt";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                strDDIInitFile = dlg.FileNames.GetValue(0).ToString();
            }
            else
            {
                strDDIInitFile = "";
            }
            m_txtbDDIInitFile.Text = strDDIInitFile;

            dlg.Dispose();
            
            if (strDDIInitFile.Length > 50)
            {
                m_txtbDDIInitFile.Text = strDDIInitFile.Substring(0, 20) + "..." + strDDIInitFile.Substring(strDDIInitFile.Length - 30, 30);
            }
            else
            {
                m_txtbDDIInitFile.Text = strDDIInitFile;
            }
        }
        private static void Analysis(string strBuf, bool boSpace)
        {

            int inCountAPIName = 0, inCountParameters = 0;

            strAPIName = new string[10]{ "", "", "", "", "",
                                         "", "", "", "", "" };
            strParameters = new string[20]{ "", "", "", "", "",
                                            "", "", "", "", "",
                                            "", "", "", "", "",
                                            "", "", "", "", "" };

            string[] equationTokens = strBuf.Split(new char[4] { ' ', '\t', '\n', ',' });
            foreach (string Tok in equationTokens)
            {
                if (inCountAPIName == 0)
                {
                    strAPIName[inCountAPIName++] = Tok.ToUpper();
                }
                else
                {
                    strParameters[inCountParameters++] = Tok.ToUpper();
                }
                
            }
        }

        private void btnMIPBRGInitial_Click(object sender, EventArgs e)
        {
            btnMIPBRGInitial.Enabled = false;

            //edtSolomonBridgeAddr.Text = "B7";
            //edtSolomonBridgePara.Text = "0250";
            //btnSolomonWriteBridge.PerformClick();

            //edtSolomonBridgeAddr.Text = "B8";
            //edtSolomonBridgePara.Text = "0000";
            //btnSolomonWriteBridge.PerformClick();

            //edtSolomonBridgeAddr.Text = "B9";
            //edtSolomonBridgePara.Text = "0000";
            //btnSolomonWriteBridge.PerformClick();

            if (File.Exists(strMIPIBRGInitFile) == false)
            {
                MessageBox.Show("File does not exist");
                return;
            }

            Match rxMatch;
            StreamReader stream = new StreamReader(strMIPIBRGInitFile); // Load Script File

            //bbtnBRGHWReset.PerformClick();

            string strLineBuf = "";
            
            while (!stream.EndOfStream)
            {
                strLineBuf = stream.ReadLine();
                if (strLineBuf != "")
                {
                    Analysis(strLineBuf.Replace("\n", ""), true); // 讀取每一行進去分析

                    if (strAPIName[0] == "BRIDGE_SEL")
                    {
                        rxMatch = Regex.Match(strParameters[0], @"^[1-3]$");
                        if (!rxMatch.Success)
                        {
                            MessageBox.Show(strLineBuf + "\nFormat Error: BRIDGE_SEL 1:BRIDGE1  2:BRIDGE2 3:BRIDGE1+2");
                            break;
                        }
                        //m_cbxSelMIPIBRG.SelectedIndex = int.Parse(strParameters[0]) - 1;
                        //m_cbxSelMIPIBRG_SelectedIndexChanged(m_cbxSelMIPIBRG, e);


                    }
                    else if (strAPIName[0] == "IOVCC")
                    {

                    }

                }


            }


            btnMIPBRGInitial.Enabled = true;
        }

        private void bbtnBRGHWReset_Click(object sender, EventArgs e)
        {
            byte[] writeBuffer = new byte[Constants.BufSize];

            Array.Clear(writeBuffer, 0, Constants.BufSize);
            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return;
            }

            writeBuffer[0] = (byte)Schedule.BRG_RESET_MSG;

            if (!DeviceWrite(Constants.BufSize, writeBuffer))
            {
                MessageBox.Show("Write Error.");
                UsbDevice.Exit();

            }

            UsbDevice.Exit();
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //MIPI_DPHY mipi = new MIPI_DPHY(8, 8, 16, 1920, 80, 80, 80, 1080, 60, 24, 4);
            textBox9.Text = mipi.bitrate.ToString();
            //tb_hsprepare_min.Text = mipi.hs_prepare_min.ToString();
            //tb_clk_prepare_max.Text = mipi.clk_prepare_max.ToString();
            //tb_clk_prepare_min.Text = mipi.clk_prepare_min.ToString();
            //tb_clk_post_min.Text = mipi.clk_post_min.ToString();
            //tb_clk_prepare_zero_min.Text = mipi.clk_prepare_zero_min.ToString();
            //tb_clk_pre_min.Text = mipi.clk_pre_min.ToString();
            //tb_clk_trail_max.Text = mipi.clk_trail_max.ToString();
            //tb_clk_trail_min.Text = mipi.clk_trail_min.ToString();
            //tb_hsprepare_min.Text = mipi.hs_prepare_min.ToString();
            //tb_hs_prepare_max.Text = mipi.hs_prepare_max.ToString();
            //tb_hs_trail_max.Text = mipi.hs_trail_max.ToString();
            //tb_hs_trail_min.Text = mipi.hs_trail_min.ToString();
            //tb_hs_prepare_zero_min.Text = mipi.hs_prepare_zero_min.ToString();
            tb_clk_post.Text = mipi.clk_post.ToString();
            tb_clk_pre.Text = mipi.clk_pre.ToString();
            tb_clk_prepare.Text = mipi.clk_prepare.ToString();
            tb_clk_trail.Text = mipi.clk_trail.ToString();
            tb_clk_zero.Text = mipi.clk_zero.ToString();
            tb_hs_prepare.Text = mipi.hs_prepare.ToString();
            tb_hs_zero.Text = mipi.hs_zero.ToString();
            tb_hs_trail.Text = mipi.hs_trail.ToString();

            
        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void textBox18_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox12_TextChanged(object sender, EventArgs e)
        {

        }

        private void label21_Click(object sender, EventArgs e)
        {

        }

        private void label20_Click(object sender, EventArgs e)
        {

        }

        private void label19_Click(object sender, EventArgs e)
        {

        }

        private void textBox31_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox29_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (int[] input in bridge_setting.Cal_Timing_Setting())
            {
                Write_Data_To_Bridge(input);
            }
        }

        private void brnLoadPic_Click(object sender, EventArgs e)
        {
            UInt32 pixelSize = Convert.ToUInt32(labBMPLenData.Text) * 1024 + 54;
            TreeNode node;
            byte[] WriteBuffer = new byte[Constants.BufSize];
            byte[] ReadBuffer = new byte[Constants.BufSize];
            PixelBuffer = new byte[pixelSize];
            string Lv1NodeName, Lv2ParentName, Lv2NodeName;
            char[] StrTemp;
            char[] CharTemp = new char[5];
            Array.Clear(WriteBuffer, 0, Constants.BufSize);
            Array.Clear(ReadBuffer, 0, Constants.BufSize);




            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return;
            }

            brnLoadPic.Enabled = false;


            node = treeView1.SelectedNode;

            if (node.ImageIndex == (int)SD_Para.cFile)
            {
                if (node.Level == 1)
                {
                    Lv1NodeName = node.Text;
                    StrTemp = Lv1NodeName.ToCharArray();
                    WriteBuffer[0] = (byte)Schedule.SD_CHANGE_DIR_MSG;
                    WriteBuffer[1] = 0x2E;
                    WriteBuffer[2] = 0x2E;

                    if (!DeviceWrite(Constants.BufSize, WriteBuffer))
                    {
                        MessageBox.Show("Write Error.");
                        brnLoadPic.Enabled = true;
                        UsbDevice.Exit();
                    }

                    Array.Clear(WriteBuffer, 0, Constants.BufSize);

                    WriteBuffer[0] = (byte)Schedule.SD_Read_1st_MSG;

                    for (int i = 0; i < Lv1NodeName.Length; i++)
                    {

                        WriteBuffer[i + 1] = (byte)StrTemp[i];
                    }

                    if (!DeviceWrite(Constants.BufSize, WriteBuffer))
                    {
                        MessageBox.Show("Write Error.");
                        brnLoadPic.Enabled = true;
                        UsbDevice.Exit();
                    }

                    ReadPixelData(pixelSize);


                }
                if (node.Level == 2)
                {
                    Lv2ParentName = node.Parent.Text;

                    Lv2NodeName = node.Text;
                    StrTemp = Lv2ParentName.ToCharArray();

                    WriteBuffer[0] = (byte)Schedule.SD_CHANGE_DIR_MSG;
                    WriteBuffer[1] = 0x2E;
                    WriteBuffer[2] = 0x2E;

                    if (!DeviceWrite(Constants.BufSize, WriteBuffer))
                    {
                        MessageBox.Show("Write Error.");
                        brnLoadPic.Enabled = true;
                        UsbDevice.Exit();
                    }

                    Array.Clear(WriteBuffer, 0, Constants.BufSize);

                    System.Threading.Thread.Sleep(50);

                    for (int i = 0; i < Lv2ParentName.Length; i++)
                    {

                        WriteBuffer[i + 1] = (byte)StrTemp[i];
                    }

                    WriteBuffer[0] = (byte)Schedule.SD_CHANGE_DIR_MSG;

                    if (!DeviceWrite(Constants.BufSize, WriteBuffer))
                    {
                        MessageBox.Show("Write Error.");
                        brnLoadPic.Enabled = true;
                        UsbDevice.Exit();
                    }

                    Array.Clear(WriteBuffer, 0, Constants.BufSize);

                    WriteBuffer[0] = (byte)Schedule.SD_Read_1st_MSG;

                    StrTemp = Lv2NodeName.ToCharArray();

                    for (int i = 0; i < Lv2NodeName.Length; i++)
                    {

                        WriteBuffer[i + 1] = (byte)StrTemp[i];
                    }

                    if (!DeviceWrite(Constants.BufSize, WriteBuffer))
                    {
                        MessageBox.Show("Write Error.");
                        brnLoadPic.Enabled = true;
                        UsbDevice.Exit();
                    }

                    ReadPixelData(pixelSize);

                }
            }


            UsbDevice.Exit();


            PictureLoadtoPanel();
            brnLoadPic.Enabled = true;


        }

        private void button4_Click(object sender, EventArgs e)
        {
            List<int> ssd28_set = ssd28_timing_cal(mipi);
            tb_lp_tx.Text = Convert.ToString(ssd28_set[1], 16);
            textBox10.Text = ssd28_set[1].ToString();
            textBox19.Text = ssd28_set[2].ToString();
            textBox23.Text = Convert.ToString((ssd28_set[1] << 8 | ssd28_set[2]), 16);
            textBox24.Text = Convert.ToString(ssd28_set[1], 16);
            textBox25.Text = Convert.ToString(ssd28_set[2], 16);
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void Search_SD_Click(object sender, EventArgs e)
        {
            Search_SD.Enabled = false;

            bool bRet;
            byte[] Writebuffer = new byte[Constants.BufSize];
            byte[] Readbuffer = new byte[Constants.BufSize];

            Array.Clear(Writebuffer, 0, Constants.BufSize);
            Array.Clear(Readbuffer, 0, Constants.BufSize);

            char[] File_Name = new char[16];
            string File_Name1;

            TreeNode root, parent, son, node, tempNode;

            int SDCard_Index = 0;


            FolderCnt = 0;
            treeView1.Nodes.Clear();
            root = treeView1.Nodes.Add("SD Card");
            root.ImageIndex = 2;
            root.SelectedImageIndex = 2;

            parent = root;

            if (DeviceOpen() == false)
            {
                MessageBox.Show("USB Connected Fail");
                return;
            }

            Writebuffer[0] = (byte)Schedule.SD_list_MSG;

            if (!DeviceWrite(Constants.BufSize, Writebuffer))
            {
                MessageBox.Show("Write Error.");
                UsbDevice.Exit();
                return;
            }

            do
            {
                Writebuffer[0] = (byte)Schedule.SD_list_1st_MSG;

                if (DeviceWrite(Constants.BufSize, Writebuffer))
                {
                    if (SDCard_Index == 0)
                        System.Threading.Thread.Sleep(100);

                    bRet = DeviceRead(Constants.BufSize, Readbuffer);

                    if ((Readbuffer[0] != 0x02) && (Readbuffer[0] != 0x03) && (bRet == SI_SUCCESS))
                    {
                        Array.Clear(File_Name, 0, 16);

                        for (int j = 0; j < 12; j++)
                        {
                            File_Name[j] = (char)Readbuffer[j + 1];
                        }

                        File_Name1 = new String(File_Name);

                        if (Readbuffer[0] == 0x00)  // Read=0,表示為資料夾,無檔案大小
                        {


                            for (int j = 0; j < 8; j++)
                            {
                                folder_name[FolderCnt, j] = (char)Readbuffer[j + 1];
                            }

                            son = new TreeNode(File_Name1);
                            son.ImageIndex = 0;
                            son.SelectedImageIndex = 0;
                            parent.Nodes.Add(son);

                            File_index[FolderCnt] = son.Index;
                            SDCard_Index++;
                            FolderCnt++;




                        }
                        else if (Readbuffer[0] == 0x01)   //Read=1,表示為檔案,有檔案大小
                        {

                            if (File_Name1.IndexOf("BMP") != -1)
                            {
                                for (int j = 13; j < 17; j++)
                                {
                                    SDCARD_ROOTDATA[SDCard_Index, j] = Readbuffer[j];
                                }

                                son = new TreeNode(File_Name1);
                                son.ImageIndex = 1;
                                son.SelectedImageIndex = 1;
                                parent.Nodes.Add(son);
                                SDCard_Index++;
                            }

                        }
                    }


                }
                else
                {
                    MessageBox.Show("Write Error.");
                    break;
                }


            } while ((Readbuffer[0] != 0x02) && (Readbuffer[0] != 0x03));


            for (int FolderIndex = FolderCnt - 1; FolderIndex >= 0; FolderIndex--)
            {
                SDCard_Index = 0;
                node = treeView1.Nodes[0].Nodes[File_index[FolderIndex]];
                if (node != null)
                {
                    if (node.ImageIndex == (int)SD_Para.cFolder)
                    {
                        Writebuffer[0] = (byte)Schedule.SD_list_MSG;
                        for (int j = 0; j < 8; j++)
                            Writebuffer[j + 1] = (byte)folder_name[FolderIndex, j];

                        if (!DeviceWrite(Constants.BufSize, Writebuffer))
                        {
                            MessageBox.Show("Write Error.");
                            UsbDevice.Exit();
                            return;
                        }

                        do
                        {
                            Writebuffer[0] = (byte)Schedule.SD_list_1st_MSG;
                            if (DeviceWrite(Constants.BufSize, Writebuffer))
                            {
                                bRet = DeviceRead(Constants.BufSize, Readbuffer);
                                if ((Readbuffer[0] != 0x02) && (Readbuffer[0] != 0x03) && (bRet == SI_SUCCESS))
                                {
                                    for (int j = 0; j < 12; j++)
                                    {
                                        File_Name[j] = (char)Readbuffer[j + 1];
                                    }
                                    File_Name1 = new String(File_Name);

                                    if (Readbuffer[0] == 0x00)  // Read=0,表示為資料夾,無檔案大小
                                    {
                                        if (File_Name1.IndexOf(".") == -1)
                                        {

                                            tempNode = new TreeNode(File_Name1);
                                            tempNode.ImageIndex = 0;
                                            tempNode.SelectedImageIndex = 0;
                                            node.Nodes.Add(tempNode);
                                            SDCard_Index++;
                                        }
                                    }
                                    else if (Readbuffer[0] == 0x01)   //Read=1,表示為檔案,有檔案大小
                                    {
                                        if (File_Name1.IndexOf("BMP") != -1)
                                        {
                                            for (int j = 0; j < 4; j++)
                                            {
                                                SDCARD_DATA[FolderIndex, SDCard_Index, j] = Readbuffer[j + 13];
                                            }

                                            tempNode = new TreeNode(File_Name1);
                                            tempNode.ImageIndex = 1;
                                            tempNode.SelectedImageIndex = 1;
                                            node.Nodes.Add(tempNode);
                                            SDCard_Index++;

                                        }

                                    }

                                }
                            }
                            else
                            {
                                MessageBox.Show("Write Error.");
                                UsbDevice.Exit();
                                return;
                            }

                        } while ((Readbuffer[0] != 0x02) && (Readbuffer[0] != 0x03));
                    }

                }

            }


            Search_SD.Enabled = true;
            root.Expand();

            UsbDevice.Exit();
        }

        /// <summary>
        /// return LPD ,HZD , HPD , CZD , CPD , CPED , CPTD , CTD , HTD ,WUD , TGO , TGET
        /// </summary>
        /// <param name="mipi"></param>
        /// <returns></returns>
        private List<int> ssd28_timing_cal(MIPI_DPHY mipi)
        {            
            List<int> output = new List<int>();
            float nibble_clk = ((1 / mipi.bitrate) * 1000) * 4;            
            int LPD = (int)(((mipi.bitrate / 2) / 8) / 18) -1;
            float lp_clk = mipi.bitrate / 2 / (LPD+1) / 8;
            int HZD = (int)Math.Round((mipi.hs_zero / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int HPD = (int)Math.Round((mipi.hs_prepare / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CZD = (int)Math.Round((mipi.clk_zero / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CPD = (int)Math.Round((mipi.clk_prepare / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CPED = (int)Math.Round((mipi.clk_pre / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CPTD = (int)Math.Round((mipi.clk_post / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CTD = (int)Math.Round((mipi.clk_trail / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int HTD = (int)Math.Round((mipi.hs_trail / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int WUD = (int)Math.Round((mipi.t_wakeup / ((1/lp_clk)*1000)), 0, MidpointRounding.AwayFromZero);
            output.Add(LPD);
            output.Add(HZD);
            output.Add(HPD);
            output.Add(CZD);
            output.Add(CPD);
            output.Add(CPED);
            output.Add(CPTD);
            output.Add(CTD);
            output.Add(HTD);
            output.Add(WUD);
            return output;

        }


        /// <summary>
        /// 對SSD2828 Class , LibUSB相依
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        private int Read_Bridge(int Address)
        {
            int Data = 0;
            byte[] readBuffer = new byte[Constants.BufSize];
            //data = ReadBridge(Address);
            //textBox20.Text = (Convert.ToString(data, 16));
            if (DeviceWrite(Constants.BufSize, bridge_setting.Read_Bridge(Address)))
            {
                System.Threading.Thread.Sleep(50);
                if (DeviceRead(Constants.BufSize, readBuffer))
                {
                    Data = (readBuffer[1] * 0x100 + readBuffer[0]);
                    

                }
                else
                {
                    MessageBox.Show("Write Error.");
                    UsbDevice.Exit();
                    return 0;
                }
            }
            return Data;
        }

        private byte[] Read_Driver(int Address , int Packet_Size , bool hs_mode) 
        {            
            byte[] readBuffer = new byte[Constants.BufSize];
            if (DeviceWrite(Constants.BufSize, bridge_setting.Read_Driver(Address , Packet_Size , hs_mode)))
            {
                System.Threading.Thread.Sleep(50);
                if (DeviceRead(Constants.BufSize, readBuffer))
                {                    
                    

                }
                else
                {
                    MessageBox.Show("Write Error.");
                    UsbDevice.Exit();                    
                }
            }
            return readBuffer;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int Address = 0xb1;
            byte[] readBuffer = new byte[Constants.BufSize];
            //data = ReadBridge(Address);
            //textBox20.Text = (Convert.ToString(data, 16));
            textBox20.Text = (Convert.ToString(Read_Bridge(Address), 16));

            bridge_setting.VideoMode_Pixel_Format(24);
            bridge_setting.VideoMode_Type_Sel(2);
            Write_Data_To_Bridge(bridge_setting.VideoMode_Setting());

        }

        private void Write_Data_To_Bridge(int Address , int Data)
        {
            if (!DeviceWrite(Constants.BufSize, bridge_setting.Write_Bridge(Address, Data)))
            {

                MessageBox.Show("Write Error.");
                UsbDevice.Exit();

            }

            UsbDevice.Exit();
        }

        private void Write_Data_To_Bridge(int[] input)
        {
            if (!DeviceWrite(Constants.BufSize, bridge_setting.Write_Bridge(input)))
            {

                MessageBox.Show("Write Error.");
                UsbDevice.Exit();

            }

            UsbDevice.Exit();
        }

        private void button6_Click(object sender, EventArgs e)
        {

            Send_Actiming_To_Bridge(mipi, bridge_setting);
            List<int[]> timing = new List<int[]>();
            timing = bridge_setting.Cal_Timing_Setting();
            bridge_setting.Write_Bridge(timing[0]);
            if (!DeviceWrite(Constants.BufSize, bridge_setting.Write_Bridge(timing[0])))
            {

                MessageBox.Show("Write Error.");
                UsbDevice.Exit();

            }

            UsbDevice.Exit();
            //textBox10.Text = (timing[0][0]).ToString();
            textBox10.Text = Convert.ToString(timing[0][0], 16);
            textBox19.Text = Convert.ToString(timing[0][1], 16);

        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            textbox_bitrate.Text = hScrollBar1.Value.ToString();
        }

        private void tabPage4_Click(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
        }

        private void btn_0xb6_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.GetItemChecked(0))
            {
                bridge_setting.Vysnc_Pulse_High(true);
            }
            else
            {
                bridge_setting.Vysnc_Pulse_High(false);
            }

            if(checkedListBox1.GetItemChecked(1))
            {
                bridge_setting.Hsync_Pulse_High(true);
            }
            else
            {
                bridge_setting.Hsync_Pulse_High(false);
            }

            if (checkedListBox1.GetItemChecked(2))
            {
                bridge_setting.Data_launch_RisingEdge(true);
            }
            else
            {
                bridge_setting.Data_launch_RisingEdge(false);
            }

            if (checkedListBox1.GetItemChecked(3))
            {
                bridge_setting.Compress_Burst_Mode(true);
            }
            else
            {
                bridge_setting.Compress_Burst_Mode(false);
            }

            if (checkedListBox1.GetItemChecked(4))
            {
                bridge_setting.Data_Insert_In_Vertical(true);
            }
            else
            {
                bridge_setting.Data_Insert_In_Vertical(false);
            }

            if (checkedListBox1.GetItemChecked(5))
            {
                bridge_setting.LP_Data_insert(true);
            }
            else
            {
                bridge_setting.LP_Data_insert(false);
            }

            if (checkedListBox1.GetItemChecked(6))
            {
                bridge_setting.VideoMode_CLK_Always_Hs(true);
            }
            else
            {
                bridge_setting.VideoMode_CLK_Always_Hs(false);
            }

            if (checkedListBox1.GetItemChecked(7))
            {
                bridge_setting.Blanking_Pkt_In_BLLP(false);
            }
            else
            {
                bridge_setting.Blanking_Pkt_In_BLLP(true);
            }

            switch (comboBox2.SelectedIndex)
            {
                case 0:
                    bridge_setting.VideoMode_Pixel_Format(24);
                    break;
                case 1:
                    bridge_setting.VideoMode_Pixel_Format(18);
                    break;
                case 2:
                    bridge_setting.VideoMode_Pixel_Format(16);
                    break;
            }

            switch(comboBox1.SelectedIndex)
            {
                case 0:
                    bridge_setting.VideoMode_Type_Sel(0);
                    break;
                case 1:
                    bridge_setting.VideoMode_Type_Sel(1);
                    break;
                case 2:
                    bridge_setting.VideoMode_Type_Sel(2);
                    break;
            }


            Write_Data_To_Bridge(bridge_setting.VideoMode_Setting());

        }

        private void checkedListBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            Write_Data_To_Bridge(bridge_setting.Lane_Select(comboBox3.SelectedIndex + 1));
        }

        private void btn_test_Click(object sender, EventArgs e)
        {
            List<int[]> testini = new List<int[]>();
            testini.Add(new int[] { 0xb7, 0x0250 });
            testini.Add(new int[] { 0xb8, 0x0000 });
            testini.Add(new int[] { 0xb9, 0x0000 });
            testini.Add(new int[] { 0xba, 0xc11e });
            testini.Add(new int[] { 0xbb, 0x0006 });
            testini.Add(new int[] { 0xb9, 0x0001 });
            testini.Add(new int[] { 0xc9, 0x1708 });
            testini.Add(new int[] { 0xca, 0x3905 });
            testini.Add(new int[] { 0xcb, 0x021e });
            testini.Add(new int[] { 0xcc, 0x0c0e });
            testini.Add(new int[] { 0xb9, 0x0001 });
            testini.Add(new int[] { 0xbd, 0x0000 });
            testini.Add(new int[] { 0xbc, 0x0000 });
            testini.Add(new int[] { 0xde, 0x0303 });
            Send_Actiming_To_Bridge(mipi, bridge_setting);
            SSD2828TextBox.Text = Read_Bridge(0xb0).ToString();
            SSD2828TextBox.Text = bridge_setting.bitrate.ToString();
            //foreach (int[] inicode in bridge_setting.Bridge_initial())
            foreach (int[] inicode in testini)
            {
                Write_Data_To_Bridge(inicode);
                
                SSD2828TextBox.Text += Convert.ToString(inicode[0], 16) + ' ' + Convert.ToString(inicode[1], 16) + " Read --> " + Convert.ToString(inicode[0], 16) + " = " + Convert.ToString(Read_Bridge(inicode[0]), 16) + "\n";                
                //Thread.Sleep(500);
            }
            //Write_Data_To_Bridge(bridge_setting.Bridge_initial)
            //int data[] = new int[];
            byte[] data = new byte[5];
            data = Read_Driver(0x0a, 1, false);
            for(int i = 0; i < 5; i++)
            {
                SSD2828TextBox.Text += Convert.ToString(data[i], 16) + "\n";
            }
            

        }

        /// <summary>
        /// Bridge Read Function 
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>


        public void USBconnect()
        {
            ErrorCode ec = ErrorCode.None;
            byte[] writeBuffer = new byte[Constants.BufSize];
            byte[] readBuffer = new byte[Constants.BufSize];


            try
            {
                // Find and open the usb device.
                MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);


                // If the device is open and ready
                if (MyUsbDevice == null)
                    throw new Exception("Device Not Found.");


                IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.

                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);

                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);
                }

                // open read endpoint 1.
                UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

                // open write endpoint 1.
                UsbEndpointWriter writer = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);

                int bytesWritten;
                writeBuffer[0] = (byte)Schedule.GETFWINFO_MSG;
                ec = writer.Write(writeBuffer, 2000, out bytesWritten);

                if (ec != ErrorCode.None)
                    throw new Exception("USB Connected Fail");

                //System.Threading.Thread.Sleep(50);

                int bytesRead;
                // If the device hasn't sent data in the last 1000 milliseconds,
                // a timeout error (ec = IoTimedOut) will occur. 
                ec = reader.Read(readBuffer, 1000, out bytesRead);
                ec = reader.Read(readBuffer, 1000, out bytesRead);

                if (readBuffer[0] == Constants.FW_VERSION)
                    Form1.ActiveForm.Text = Constants.SW_VERSION + "    F/W = Ver " + Constants.FW_VERSION;
                else
                    Form1.ActiveForm.Text = Constants.SW_VERSION + "    Status：F/W = Ver " + readBuffer[0] + "，  Please update F/W = Ver " + Constants.FW_VERSION;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (MyUsbDevice != null)
                {
                    if (MyUsbDevice.IsOpen)
                    {
                        // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                        // it exposes an IUsbDevice interface. If not (WinUSB) the 
                        // 'wholeUsbDevice' variable will be null indicating this is 
                        // an interface of a device; it does not require or support 
                        // configuration and interface selection.
                        IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                        if (!ReferenceEquals(wholeUsbDevice, null))
                        {
                            // Release interface #0.
                            wholeUsbDevice.ReleaseInterface(0);
                        }

                        MyUsbDevice.Close();
                    }
                    MyUsbDevice = null;

                    // Free usb resources
                    UsbDevice.Exit();

                }
            }
        }

        public void Send_Actiming_To_Bridge(MIPI_DPHY mipi , SSD2828 bridge)
        {
            bridge.hs_prepare = mipi.hs_prepare;
            bridge.hs_trail = mipi.hs_prepare;
            bridge.hs_zero = mipi.hs_zero;
            bridge.lptx = mipi.lptx;
            bridge.clk_post = mipi.clk_post;
            bridge.clk_pre = mipi.clk_pre;
            bridge.clk_prepare = mipi.clk_prepare;
            bridge.clk_trail = mipi.clk_prepare;
            bridge.clk_zero = mipi.clk_zero;
            bridge.t_wakeup = mipi.t_wakeup;
            bridge.bitrate = mipi.bitrate;
            bridge.vsa = mipi.vsa;
            bridge.vfp = mipi.vfp;
            bridge.vsa = mipi.vsa;
            bridge.vact = mipi.vact;
            bridge.hact = mipi.hact;
            bridge.hbp = mipi.hbp;
            bridge.hfp = mipi.hfp;
            bridge.hsa = mipi.hsa;
            bridge.lane_cnt = mipi.lane_cnt;
            bridge.pixel_format = mipi.pixel_format;
        }



    }


    
}
