using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication4
{
    public class MIPI_DPHY
    {
        public int vfp, vbp, vsa, vact, hbp, hfp, hsa, hact, lane_cnt, pixel_format;
        public bool dualport;
        public float bitrate;
        public float framerate;
        private float  hs_prepare_min, hs_prepare_max, clk_post_min,  hs_prepare_zero_min,  clk_prepare_min, clk_prepare_max, hs_trail_max, clk_trail_max, hs_trail_min, t_wakeup_min, clk_pre_min, clk_trail_min, clk_prepare_zero_min;
        public float hs_trail, hs_zero, hs_prepare, clk_prepare, lptx, clk_zero, clk_pre, clk_trail, clk_post , t_wakeup;
       


        public MIPI_DPHY()
        {
            
        }

        public void Set_Actiming()
        {

        }


        public MIPI_DPHY(int vfp , int vbp , int vsa , int vact , int hfp , int hbp , int hsa , int hact , float framerate , int pixel_format , int lane_cnt)
        {

            this.vfp = vfp;
            this.vbp = vbp;
            this.vsa = vsa;
            this.vact = vact;
            this.hfp = hfp;
            this.hbp = hbp;
            this.hsa = hsa;
            this.hact = hact;
            this.framerate = framerate;
            this.pixel_format = pixel_format;
            this.lane_cnt = lane_cnt;
            cal_bitrate();
            cal_actiming();
        }

        public void cal_actiming()
        {
            cal_bitrate();
            float ui = (1 / bitrate) *1000;
            clk_post_min = 60 + 52 * ui;
            clk_pre_min = 8 * ui;
            clk_prepare_min = 38;
            clk_prepare_max = 95;
            clk_trail_min = 60;
            clk_prepare_zero_min = 300;
            hs_trail_max = 105 + 12 * ui;
            clk_trail_max = 105 + 12 * ui;
            hs_prepare_min = 40 + 4 * ui;            
            hs_prepare_max = 85 + 6 * ui;
            hs_prepare_zero_min = 145 + 10 * ui;
            hs_trail_min = 60 + 4 * ui;
            t_wakeup_min = 1;
            clk_post = clk_post_min + 50;
            clk_pre = 50;
            clk_prepare = 70;
            clk_trail = 100;
            clk_zero = 300;
            hs_trail = 100;
            clk_trail = 100;
            hs_prepare = (hs_prepare_max + hs_prepare_min) / 2;
            hs_zero = hs_prepare_zero_min + 50 - hs_prepare;
            t_wakeup = 1000000;


        }

        

        /// <summary>
        /// BIT RATE計算
        /// </summary>
        public void cal_bitrate()
        {
            bitrate = (vbp + vfp + vsa + vact) * (hbp + hfp + hsa + hact) * 24 / 1000000 * 60 / lane_cnt;
        }


        /// <summary>
        /// 計算Actiming 的參考值
        /// </summary>

    }
}
