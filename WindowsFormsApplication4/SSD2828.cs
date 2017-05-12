using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication4
{
    public class SSD2828
    {
        public float hs_trail, hs_zero, hs_prepare, clk_prepare, lptx, clk_zero, clk_pre, clk_trail, clk_post, t_wakeup, bitrate;
        public int vfp, vbp, vsa, vact, hbp, hfp, hsa, hact, lane_cnt, pixel_format;
        public int VS_P, HS_P, PCLK_P, CBM, NVB, NVD, BLLP, VCS, VM, VPF;//0xB6
        public int TXD, LPE, EOT, ECD, REN, DCS, CSS, HCLK, VEN, SLP, CKE, HS; //0XB7
        public int SYSD, SYS_DIS, PEN;//0XB9
        public int FR, MS, NS;//0XBA CHANGE BEFORE SET "PEN"
        public int LPD;//0XBB
        public int LS;//0XDE
        private const int TX_CLK = 20; //Mhz
        private int Picture_mode_sel = 0;
        private int Bridge_sel = 0;
        private const int BufSize = 4096;
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

        public SSD2828()
        {
            bitrate = 600;
            lane_cnt = 4;
            Picture_mode_sel = 0;
            Bridge_sel = 0;
            //0xB7
            TXD = 0;
            LPE = 0;
            EOT = 1;
            ECD = 0;
            REN = 0;
            DCS = 1;
            CSS = 0;
            HCLK = 1;            
            VEN = 0;
            SLP = 0;                                    
            CKE = 0;
            HS = 0;

            MS = 2;


        }
        /// <summary>
        /// 設定為Command mode
        /// </summary>
        public void Set_Command_mode()
        {
            Picture_mode_sel = 0;
        }
        /// <summary>
        /// 設定為video mdoe
        /// </summary>
        public void Set_Video_mode()
        {
            Picture_mode_sel = 1;
        }
        /// <summary>
        /// 設定bridge 1輸出
        /// </summary>
        public void Select_Bridge_1()
        {
            Bridge_sel = 0;
        }
        /// <summary>
        /// 設定bridge 2 輸出
        /// </summary>
        public void Select_Bridge_2()
        {
            Bridge_sel = 1;
        }
        /// <summary>
        /// 0:bridge 1 + command mode , 1:bridge 2 + command mode , 2:bridge 1 + video mode , 3:bridge 2 + Video mode , 
        /// </summary>
        /// <returns></returns>
        public int Get_Bridge_setting()
        {
            return (Picture_mode_sel * 2) + (Bridge_sel);
        }

        public byte[] Write_Bridge(int Address, int Data)
        {
            byte[] writeBuffer = new byte[4096];


            Array.Clear(writeBuffer, 0, 4096);




            writeBuffer[0] = 0x09;
            writeBuffer[1] = 0x02;
            writeBuffer[2] = 0x00;
            writeBuffer[3] = 0x00;
            writeBuffer[4] = 0x00;
            writeBuffer[5] = (byte)Address;
            writeBuffer[10] = (byte)(Data & 0xFF);
            writeBuffer[11] = (byte)((Data >> 8) & 0xFF);

            switch (Get_Bridge_setting())
            {
                case 0:
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT1;
                    break;
                case 1:
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT2;
                    break;
                case 2:
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W1;
                    break;
                case 3:
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W2;
                    break;
            }

            return writeBuffer;

            //if (!DeviceWrite(Constants.BufSize, writeBuffer))
            //{

            //    MessageBox.Show("Write Error.");
            //    UsbDevice.Exit();

            //}

            //UsbDevice.Exit();
        }

        public byte[] Write_Bridge(int[] input)
        {
            byte[] writeBuffer = new byte[4096];


            Array.Clear(writeBuffer, 0, 4096);

            int Address = input[0];
            int Data = input[1];

            writeBuffer[0] = 0x09;
            writeBuffer[1] = 0x02;
            writeBuffer[2] = 0x00;
            writeBuffer[3] = 0x00;
            writeBuffer[4] = 0x00;
            writeBuffer[5] = (byte)Address;
            writeBuffer[10] = (byte)(Data & 0xFF);
            writeBuffer[11] = (byte)((Data >> 8) & 0xFF);

            switch (Get_Bridge_setting())
            {
                case 0:
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT1;
                    break;
                case 1:
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT2;
                    break;
                case 2:
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W1;
                    break;
                case 3:
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W2;
                    break;
            }

            return writeBuffer;

            //if (!DeviceWrite(Constants.BufSize, writeBuffer))
            //{

            //    MessageBox.Show("Write Error.");
            //    UsbDevice.Exit();

            //}

            //UsbDevice.Exit();
        }

        public byte[] Read_Driver(int Address , int Packet_length  , bool hs_mode)
        {
            byte[] writeBuffer = new byte[BufSize];
            byte[] readBuffer = new byte[BufSize];

            Array.Clear(writeBuffer, 0, BufSize);
            Array.Clear(writeBuffer, 0, BufSize);




            writeBuffer[0] = (byte)Schedule.DEVICE_READ_MSG;
            writeBuffer[1] = ((byte)(Packet_length & 0xff));
            writeBuffer[2] = ((byte)((Packet_length >> 8) & 0xff));
            writeBuffer[3] = ((byte)((Packet_length >> 16) & 0xff));
            writeBuffer[4] = ((byte)((Packet_length >> 24) & 0xff));
            writeBuffer[5] = (byte)(Address & 0xff);
            //writeBuffer[6] = Address >= 0xb0 ? (byte)1 : (byte)0;
            writeBuffer[6] = (byte)0x01;
            //writeBuffer[8] = hs_mode ? (byte)1 : (byte)0;
            writeBuffer[8] = (byte)0x01;

            switch (Get_Bridge_setting())
            {
                case 0:
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT1;
                    break;
                case 1:
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT2;
                    break;
                case 2:
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W1;
                    break;
                case 3:
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W2;
                    break;
            }

            return writeBuffer;

        }

        public byte[] Read_Bridge(int Address)
        {
            byte[] writeBuffer = new byte[BufSize];
            byte[] readBuffer = new byte[BufSize];

            Array.Clear(writeBuffer, 0, BufSize);
            Array.Clear(writeBuffer, 0, BufSize);




            writeBuffer[0] = (byte)Schedule.READREG_MSG;
            writeBuffer[1] = 0x02;
            writeBuffer[2] = 0x00;
            writeBuffer[3] = 0x00;
            writeBuffer[4] = 0x00;
            writeBuffer[5] = (byte)Address;


            switch (Get_Bridge_setting())
            {
                case 0:
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT1;
                    break;
                case 1:
                    writeBuffer[7] = (byte)Interface.IF_SSDMCU24BIT2;
                    break;
                case 2:
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W1;
                    break;
                case 3:
                    writeBuffer[7] = (byte)Interface.IF_SSDSPI3W2;
                    break;
            }

            return writeBuffer;

        }

        public byte[] Reset_Bridge()
        {
            byte[] writeBuffer = new byte[BufSize];
            Array.Clear(writeBuffer, 0, BufSize);
            writeBuffer[0] = (byte)Schedule.BRG_RESET_MSG;
            return writeBuffer;
        }
        public List<int[]> Set_Bridge_Freq()
        {
            /*
             * f-pre = fin / MS
             * fout = f-pre * NS
             * 
             * fout = 輸出bitrate = fin / MS * NS
             */
            int Target_bitrate = (int)((bitrate / 5) * 5);
            int BA = 0;
            List<int[]> output = new List<int[]>();

            if (bitrate > 62.5 && bitrate < 125)
                FR = 0;
            else if (bitrate >= 126 && bitrate < 250)
                FR = 1;
            else if (bitrate >= 251 && bitrate < 500)
                FR = 2;
            else if (bitrate >= 501 && bitrate < 1000)
                FR = 3;
           
            NS = (Target_bitrate / (TX_CLK / MS));
            BA = ((FR & 3) << 14 | (MS & 0x1F) << 8 | (NS & 0xFF)) & 0xFFFF;
            output.Add(new int[] { 0xba, BA });            
            return output;
        }
        public List<int[]> Cal_Timing_Setting()
        {
            //List<List<int>> output = new List<List<int>>();
            List<int[]> output = new List<int[]>();
            float nibble_clk = ((1 / bitrate) * 1000) * 4;
            int LPD = (int)(((bitrate / 2) / 8) / 18) - 1;
            float lp_clk = bitrate / 2 / (LPD + 1) / 8;
            int HZD = (int)Math.Round((hs_zero / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int HPD = (int)Math.Round((hs_prepare / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CZD = (int)Math.Round((clk_zero / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CPD = (int)Math.Round((clk_prepare / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CPED = (int)Math.Round((clk_pre / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CPTD = (int)Math.Round((clk_post / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int CTD = (int)Math.Round((clk_trail / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int HTD = (int)Math.Round((hs_trail / nibble_clk), 0, MidpointRounding.AwayFromZero);
            int WUD = (int)Math.Round((t_wakeup / ((1 / lp_clk) * 1000)), 0, MidpointRounding.AwayFromZero);
            int TGO = 8; //4 * lp_clk (TAGO = 4倍LPTX) =  n * lp_clk/2 
            int TGET = 10; //5 * lp_clk (TAGET = 5倍LPTX) =  n * lp_clk/2 
            output.Add(new int[] { 0xc9, (HZD << 8 | HPD) & 0xffff });
            output.Add(new int[] { 0xca, (CZD << 8 | CPD) & 0xffff });
            output.Add(new int[] { 0xcb, (CPED << 8 | CPTD) & 0xffff });
            output.Add(new int[] { 0xcc, (CTD << 8 | HTD) & 0xffff });
            //output.Add(new int[] { 0xcd, (WUD) & 0xffff });
            //output.Add(new int[] { 0xce, (TGO << 8 | TGET) & 0xffff });
            output.Add(new int[] { 0xbb, (LPD) & 0x003f });

            return output;

        }

        public List<int[]> Bridge_initial()
        {
            List<int[]> output = new List<int[]>();
            //int B6 = (VS_P << 15 | HS_P << 14 | PCLK_P << 13 | CBM << 8 | NVB << 7 | NVD << 6 | BLLP << 5 | VCS << 4 | VM << 2 | VPF) & 0xffff;
            int B7 = (TXD & 1) << 11 | (LPE & 1) << 10 | (EOT & 1) << 9 | (ECD & 1) << 8 | (REN & 1) << 7 | (DCS & 1) << 6 | (CSS & 1) << 5 | (HCLK & 1) << 4
                | (VEN & 1) << 3 | (SLP & 1) << 2 | (CKE & 1) << 1 | (HS & 1);
            int B9 = (SYSD << 14 | SYS_DIS << 13 | PEN) & 0xffff;
            output.Add(new int[] { 0xb9, 0x00 });
            output.Add(new int[] { 0xb7, B7 });
            output.Add(new int[] { 0xb8, 0x00 });


            foreach (int[] data in Set_Bridge_Freq())
            {
                output.Add(data);
            }
            foreach (int[] data in Cal_Timing_Setting())
            {
                output.Add(data);
            }
            output.Add(new int[] { 0xb9, 0x01 });

            output.Add(Lane_Select(lane_cnt));
            return output;
        }

        public List<int[]> Cal_Porch_Setting()
        {
            List<int[]> output = new List<int[]>();
            output.Add(new int[] { 0xb1, (vsa << 8 | hsa) & 0xff });
            output.Add(new int[] { 0xb2, (vbp << 8 | hbp) & 0xff });
            output.Add(new int[] { 0xb3, (vfp << 8 | hfp) & 0xff });
            output.Add(new int[] { 0xb4, (hact) & 0xff });
            output.Add(new int[] { 0xb5, (vact) & 0xff });
            output.Add(new int[] { 0xb6, (vsa << 8 | hsa) & 0xff });

            return output;
        }
        /// <summary>
        /// Count 0XB6
        /// </summary>
        /// <returns></returns>
        public int[] VideoMode_Setting()
        {
            
            int VICR6 = (VS_P << 15 | HS_P << 14 | PCLK_P << 13 | CBM << 8 | NVB << 7 | NVD << 6 | BLLP << 5 | VCS << 4 | VM << 2 | VPF) & 0xffff;
            int[] output = new int[] { 0xb6, VICR6 };
            return output;

        }

        public int[] Configuration_register()
        {
            int B7 = (TXD & 1) << 11 | (LPE & 1) << 10 | (EOT & 1) << 9 | (ECD & 1) << 8 | (REN & 1) << 7 | (DCS & 1) << 6 | (CSS & 1) << 5 | (HCLK & 1) << 4
                | (VEN & 1) << 3 | (SLP & 1) << 2 | (CKE & 1) << 1 | (HS & 1);
            int[] output = new int[] { 0xb7, B7 };
            return output;
        }

        public void Vysnc_Pulse_High(bool high)
        {
            VS_P = high ? 1 : 0;
        }

        public void Hsync_Pulse_High(bool high)
        {
            HS_P = high ? 1 : 0;
        }

        public void Data_launch_RisingEdge(bool Rising)
        {
            PCLK_P = Rising ? 1 : 0;
        }

        public void Compress_Burst_Mode(bool Burst)
        {
            CBM = Burst ? 1 : 0;
        }

        public void Data_Insert_In_Vertical(bool vertical)
        {
            NVB = vertical ? 1 : 0;
        }

        public void LP_Data_insert(bool LP)
        {
            NVD = LP ? 1 : 0;
        }

        public void VideoMode_CLK_Always_Hs(bool HS)
        {
            VCS = HS ? 0 : 1;
        }

        public void Blanking_Pkt_In_BLLP(bool PKT)
        {
            BLLP = PKT ? 0 : 1;
        }

        public void VideoMode_Type_Sel(int type)
        {
            VM = (type & 3);
        }

        public void VideoMode_Pixel_Format(int pixel_format)
        {
            switch (pixel_format)
            {
                case 24:
                    VPF = 3;
                    this.pixel_format = pixel_format;
                    break;
                case 18:
                    VPF = 1;
                    this.pixel_format = pixel_format;
                    break;
                case 16:
                    VPF = 1;
                    this.pixel_format = pixel_format;
                    break;
                default:
                    VPF = 3;
                    this.pixel_format = 24;
                    break;

            }
            

        }
         
        public int[] Lane_Select(int lane)
        {
            if(lane > 0 && lane < 5)
            {
                lane_cnt = lane;
                int[] output = new int[] { 0xde, (lane-1) & 0x3 };
                return output;
                
            }
            else
            {
                lane_cnt = 4;
                int[] output = new int[] { 0xde, 0x03 };
                return output;
            }


        }

       
    }

}