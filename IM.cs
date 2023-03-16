//#define Simulation
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks; 
using System.Windows.Forms; 

namespace PCControl_IM_230228
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        } 

        //Variables
        static int spd = 1, Mode, cnttrayin, cnttrayout, settrayin, settrayout,
            setalign1, setalign2, setalign3, setalign4,
            cntalign1, cntalign2, cntalign3, cntalign4,
            stepinit, stepld, stepud1, stepud2,
            stepinitbk, stepldbk, stepud1bk, stepud2bk;
        //Mode: 1 - Auto, 2 - Manual
        bool bstart, bcycle, binit, bpause;
        //bstart: true - Auto Run, false Non Auto Run
        //bcycle: true - Cycle Stop, false Non Cycle Stop
        //bpause: true - Paused, false Non Paused
        //binit: true - Initialized, false Not Initialized Yet 

        device
            c1 = new device(1, 0x180, 0x280),
            c2 = new device(1, 0x1a0, 0x290),
            c3 = new device(1, 0x1c0, 0x2b1),
            c4 = new device(1, 0x1e0, 0x2a0),
            z1 = new device(0, 0x2c1, 0x288),
            s1 = new device(0, 0x2c0, 0x286),
            x1 = new device(0, 0x2c2, 0x28c),
            x2 = new device(0, 0x2c3, 0x298),
            y2 = new device(0, 0x2c4, 0x29a),
            z4 = new device(0, 0x2d1, 0x2a8),
            s4 = new device(0, 0x2d0, 0x2a6),
            x4 = new device(0, 0x2d2, 0x2ac),
            x3 = new device(0, 0x2d3, 0x2b6),
            y3 = new device(0, 0x2d4, 0x2bc),
            btin = new device(0, 0x26b, 0x283),
            cpin = new device(0, 0x26c, 0x20b),
            btout = new device(0, 0x261, 0x2a3),
            cpout = new device(0, 0x262, 0x20c),
            lb = new device(0, 0x26e),
            lg = new device(0, 0x27a),
            lr = new device(0, 0x27e),
            ly = new device(0, 0x27f);


        //CC Link
        [DllImport("MdFunc32.dll")]
        static extern short mdOpen(short chan, short mode, out int path);
        [DllImport("MdFunc32.dll")]
        static extern short mdClose(int path);
        [DllImport("MdFunc32.dll")]
        static extern int mdReceiveEx(int path, int net, int st, int dev, int devno, ref int size, ushort[] dataX);
        [DllImport("MdFunc32.dll")]
        static extern int mdSendEx(int path, int net, int st, int dev, int devno, ref int size, ushort[] dataY); 

        int path, net=4, st=1, size=92, ret;
        static ushort[] dataX = new ushort[46], dataY = new ushort[46];
        bool isopen; 

        void connect()
        {
#if Simulation
            //Tao ket noi gia
            isopen = true;
            LoaderSimulation.LoaderUnit Loader = new LoaderSimulation.LoaderUnit();
            Loader.Show();
#else
            ret = mdOpen(81, -1, out path);
            isopen = (ret == 0 || ret == 66);
#endif
            if (isopen) ret = 0;
        } 

        void disconnect()
        {
            ret = mdClose(path);
            isopen = false;
        } 

        void recvX()
        {
            ret = mdReceiveEx(path, net, st, 1, 0, ref size, dataX);
        } 

        void sendY()
        {
            ret = mdSendEx(path, net, st, 2, 0, ref size, dataY);
        } 

        static bool get(int add)
        {
            int wrd = add / 16;
            int bit = add % 16; 

            return (dataX[wrd] & (ushort)(1 << bit)) != 0;
        } 

        static void set(int add, bool val)
        {
            int wrd = add / 16;
            int bit = add % 16; 

            if (val) dataY[wrd] = (ushort)(dataY[wrd] | (1 << bit));
            else dataY[wrd] = (ushort)(dataY[wrd] & (~(1 << bit)));
        } 

        //Class Device
        class device
        {
            int x1, x2, y1, y2, y3, y4, y5, type;
            public bool run;
            //type 0 - cylinder/lamp/button, 1 - conveyor
            public device(int _type, int _y1, int _x1 = 0)
            {
                type = _type;
                x1 = _x1;
                y1 = _y1;
                if (type == 0)
                    x2 = x1 + 1; 

                if (type == 1)
                {
                    x2 = x1 + 2;
                    y2 = y1 + 1;
                    y3 = y2 + 1;
                    y4 = y3 + 1;
                    y5 = y4 + 1;
                }
            } 

            public void go(bool dir)
            {
                if (type == 0)
                {
                    set(y1, dir);
                    run = dir;
                }
                else
                {
                    set(y1, dir);
                    set(y2, !dir);
                    run = true;
                    set(y3, spd == 2);
                    set(y4, spd == 1);
                    set(y5, spd == 0);
                }
            } 

            public void go() { go(!run); } 

            public void fw() { go(true); }
            public void bw() { go(false); } 

            public void stop()
            {
                set(y1, false);
                set(y2, false);
                run = false;
            } 

            public bool infw() { return get(x1); }
            public bool inbw() { return get(x2); } 

            public bool exist()
            {
                return get(x1) || get(x2);
            }
            public bool recv() { return get(x2); } 

            public bool press() { return get(x1); }
        } 

        class TIMER
        {
            public bool sts;
            public int start;
            public bool delay(bool In, int time = 0)
            {
                if (In)
                {
                    if (!sts)
                    {
                        sts = true;
                        start = Environment.TickCount;
                    }
                    return Environment.TickCount - start >= time;
                }
                else
                {
                    sts = false;
                    return false;
                }
            }
        } 

        public int LR = 1, LY = 2, LG = 4, LB = 8;
        public void lampset(int lamp)
        {
            lb.go((lamp & LB) != 0);
            lg.go((lamp & LG) != 0);
            ly.go((lamp & LY) != 0);
            lr.go((lamp & LR) != 0);
        } 

        private void timer1_Tick(object sender, EventArgs e)
        {
            //Dong bo data giua chuong trinh va PLC
            if (isopen)
            {
#if Simulation
                dataX = LoaderSimulation.LoaderUnit.DataX;
                LoaderSimulation.LoaderUnit.DataY = dataY;
#else
                recvX();
                sendY();
#endif
            }
            if (ret != 0) isopen = false; 

            this.Invoke( new Action (() =>
            {
                    //Cap nhat trang thai: mau, text
                    statusconnect.Text = isopen ? "Ket noi" : "Ngat ket noi";
                    statusconnect.BackColor = isopen ? Color.LightGreen : Color.Red;
                    //if (isopen)
                    //    statusconnect.BackColor = Color.LightGreen;
                    //else
                    //    statusconnect.BackColor = Color.Red; 

                    btb.BackColor = lb.run ? Color.LightGreen : Color.Transparent;
                    btg.BackColor = lg.run ? Color.LightGreen : Color.Transparent;
                    bty.BackColor = ly.run ? Color.Yellow : Color.Transparent;
                    btr.BackColor = lr.run ? Color.Red : Color.Transparent; 

                    btAuto.BackColor = Mode == 1 ? Color.LightGreen : Color.Transparent;
                    btManual.BackColor = Mode == 2 ? Color.LightGreen : Color.Transparent;
                    btStart.BackColor = bstart ? Color.LightGreen : Color.Transparent;
                    btCycle.BackColor = bcycle ? Color.LightGreen : Color.Transparent;
                    btPause.BackColor = bpause ? Color.LightGreen : Color.Transparent;
                    btInitial.BackColor = binit ? Color.LightGreen : Color.Transparent; 

                    btPort1.BackColor = c1.exist() ? Color.LightGreen : Color.Transparent;
                    btPort2.BackColor = c2.exist() ? Color.LightGreen : Color.Transparent;
                    btPort3.BackColor = c3.exist() ? Color.LightGreen : Color.Transparent;
                    btPort4.BackColor = c4.exist() ? Color.LightGreen : Color.Transparent; 

                    lbtrayin.Text = cnttrayin.ToString();
                    lbtrayout.Text = cnttrayout.ToString();
                }
                )); 

            //Nhay den Xanh khi o Manual mode
            if (Mode == 2)
            {
                if (tmanualblink.delay(true, 500))
                {
                    tmanualblink.delay(false);
                    lg.go();
                }
            } 

            //Cycle Stop
            if (bcycle && stepld == 0 && stepud1 == 0 && stepud2 == 0)
            {
                bstart = bcycle = false;
                lampset(LY);
            }
            if (bstart)
            {
                //120 khong cap tray Conveyor 1,3 -> Stop
                if (!c1.exist() && !c3.exist())
                {
                    if (t120s.delay(true, 120000))
                    {
                        t120s.delay(false);
                        bcycle = true;
                    }
                } 

                //Chay du so luot cap tray in -> Stop
                if (cnttrayin >= settrayin)
                {
                    cnttrayin = 0; //Reset de chay lai
                    bcycle = true;
                } 

                //Chay du so luot lay tray out -> Stop
                if (cnttrayout >= settrayout)
                {
                    cnttrayout = 0; //Reset de chay lai
                    bcycle = true;
                } 

            } 

            //Sequences
            seqinit();
            seqld();
            sequd1();
            sequd2(); 

        }
        
        TIMER t120s = new TIMER(),
            tmanualblink = new TIMER(),
            tinit = new TIMER(), tinitblink = new TIMER();
        void seqinit()
        {


            switch(stepinit)
            {
                case 0:
                    break; 

                case 10:
                    c1.fw();
                    c2.fw();
                    c3.fw();
                    c4.fw();
                    z1.bw();
                    s1.fw();
                    x1.bw();
                    x2.bw();
                    y2.bw();
                    z4.bw();
                    s4.fw();
                    x4.bw();
                    x3.bw();
                    y3.bw(); 

                    if (tinitblink.delay(true, 500))
                    {
                        tinitblink.delay(false);
                        ly.go();
                        lg.go();
                    }
                    
                    if (tinit.delay(true, 3000))
                    {
                        tinit.delay(false);
                        stepinit++;
                    } 

                    break;
                    
                case 11:
                    c1.bw(); c2.bw(); c3.bw(); c4.bw(); 

                    if (tinitblink.delay(true, 500))
                    {
                        tinitblink.delay(false);
                        ly.go();
                        lg.go();
                    } 

                    if (tinit.delay(true, 3000))
                    {
                        tinit.delay(false);
                        stepinit++;
                    } 

                    break;
                    
                case 12:
                    c1.stop(); c2.stop(); c3.stop(); c4.stop(); 

                    if (tinitblink.delay(true, 500))
                    {
                        tinitblink.delay(false);
                        ly.go();
                        lg.go();
                    } 

                    if (!c1.exist() && !c2.exist()
                          && !c3.exist() && !c4.exist())
                    {
                        lampset(LY);
                        stepinit = 0;
                        binit = true;
                    } 

                    break;
            }
        } 

        TIMER tbtin = new TIMER(), tbtinblink = new TIMER();
        void seqld()
        {
            switch(stepld)
            {
                case 0:
                    //Action 

                    //Check dieu kien de Nhay Step
                    if (bstart && !bpause && !bcycle && !c1.exist()
                        && tbtin.delay(btin.press(), 2000))
                    {
                        stepld = 10;
                        tbtin.delay(false);
                    }
                    break; 

                case 10:
                    if (tbtinblink.delay(true, 500))
                    {
                        tbtinblink.delay(false);
                        btin.go();
                    } 

                    if (c1.exist() && cpin.press())
                        stepld++;
                    break; 

                case 11:
                    z1.fw();
                    s1.fw();
                    btin.go(false); 

                    if (z1.infw())
                        stepld++;
                    break; 

                case 12:
                    x1.fw(); 

                    if (x1.infw())
                        stepld++;
                    break; 

                case 13:
                    x1.bw(); 

                    if (x1.inbw())
                    {
                        cntalign1++;
                        if (cntalign1 >= setalign1)
                            stepld++;
                        else
                            stepld = 12;
                    }
                    break; 

                case 14:
                    z1.bw();
                    s1.bw();
                    cntalign1 = 0; 

                    if (z1.inbw() && s1.inbw() && !c2.exist())
                        stepld++;
                    break; 

                case 15: 
                    c1.fw();
                    c2.fw(); 

                    if (c2.recv())
                        stepld++;
                    break; 

                case 16:
                    c1.stop();
                    c2.stop();
                    x2.fw();
                    y2.fw(); 

                    if (x2.infw() && y2.infw())
                        stepld++; 

                    break;
                case 17:
                    x2.bw();
                    y2.bw();
                    if(x2.inbw() && y2.inbw())
                    {
                        cntalign2++;
                        if (cntalign2 >= setalign2)
                            stepld++;
                        else
                            stepld = 16;
                    }
                    break;
                case 18:
                    cntalign2 = 0;
                    cnttrayin++;
                    stepld = 0;
                    break;
            } 

        } 

        TIMER tc3 = new TIMER();
        void sequd1()
        {
            switch(stepud1)
            {
                   
                case 0:
                    if (bstart && !bpause && !bcycle && tc3.delay(c3.exist(), 5000))
                    {
                        stepud1 = 10;
                        tc3.delay(false);
                    }
                    break; 

                case 10:
                    x3.fw();
                    y3.fw(); 

                    if (x3.infw() && y3.infw())
                        stepud1++;
                   break; 

                case 11:
                   x3.bw();
                   y3.bw(); 

                   if (x3.inbw() && y3.inbw())
                   {
                       cntalign3++;
                       if (cntalign3 >= setalign3)
                           stepud1++;
                       else stepud1 = 10;
                   }                       
                   break; 

                case 12:
                   cntalign3 = 0;
                   if(!c4.exist() && stepud2==0)
                       stepud1++;
                   break; 

                case 13:
                   c3.fw();
                   c4.fw(); 

                   if (c4.recv())
                       stepud1++;
                   break; 

                case 14:
                   c3.stop();
                   c4.stop();
                   stepud2 = 10;
                   stepud1=0;
                   break;                    
            }
        } 

        TIMER tbtout = new TIMER(), tc4blink = new TIMER();
        void sequd2()
        { 

            switch (stepud2)
            { 

                case 0: 

                    if (stepud1 == 13 && c4.recv())
                        stepud2 = 10;
                    break; 

                case 10: 

                    if (tbtout.delay(btout.press(), 2000))
                    {
                        stepud2++;
                        tbtout.delay(false);
                    }
                    break; 

                case 11:
                    z4.fw(); 

                    if (tc4blink.delay(true, 500))
                    {
                        btout.go();
                        tc4blink.delay(false);
                    } 

                    if (z4.infw())
                        stepud2++;
                    break; 

                case 12:
                    x4.fw(); 

                    if (tc4blink.delay(true, 500))
                    {
                        btout.go();
                        tc4blink.delay(false);
                    } 

                    if (x4.infw())
                        stepud2++;
                    break; 

                case 13:
                    x4.bw(); 

                    if (tc4blink.delay(true, 500))
                    {
                        btout.go();
                        tc4blink.delay(false);
                    } 

                    if (x4.inbw())
                    {
                        cntalign4++;
                        if(cntalign4 >= setalign4)                            
                            stepud2++;
                        else
                            stepud2 = 12;
                    }
                    break; 

                case 14:
                    cntalign4 = 0;
                    z4.bw();
                    s4.bw();
                    
                    if (tc4blink.delay(true, 500))
                    {
                        btout.go();
                        tc4blink.delay(false);
                    } 

                    if (z4.inbw() && s4.inbw() && !c4.exist() && cpout.press())
                        stepud2++;
                    break; 

                case 15:
                    s4.fw();
                    btout.go(false); 

                    if (s4.infw())
                    {
                        cnttrayout++;
                        stepud2 = 0;
                    }
                    break;
            }
        } 

        private void btAuto_Click(object sender, EventArgs e)
        {
            if (!isopen)
            {
                MessageBox.Show("PLC ngat ket noi");
                return;
            }
            if (!binit)
            {
                MessageBox.Show("EQ chua khoi tao");
                return;
            }
            if (c1.run || c2.run || c3.run || c4.run)
            {
                MessageBox.Show("Conveyor dang run");
                return;
            } 

            Mode = 1;
            tabControl1.SelectedIndex = 0;
            lampset(LY);
        } 

        private void btManual_Click(object sender, EventArgs e)
        {
            if (!isopen)
            {
                MessageBox.Show("PLC ngat ket noi");
                return;
            }
            if (!binit)
            {
                MessageBox.Show("EQ chua khoi tao");
                return;
            }
            if (bstart)
            {
                MessageBox.Show("EQ Auto Run");
                return;
            } 

            Mode = 2;
            tabControl1.SelectedIndex = 1;
            lampset(LG); //Chua co nhay 

            binit = false;
        } 

        private void btStart_Click(object sender, EventArgs e)
        {
            if (!isopen)
            {
                MessageBox.Show("PLC ngat ket noi");
                return;
            }
            if (Mode != 1)
            {
                MessageBox.Show("EQ khong Auto Mode");
                return;
            }
            if (setalign1 == 0 || setalign2 == 0 || setalign3 == 0 || setalign4 == 0)
            {
                MessageBox.Show("Hay set Align");
                return;
            }
            if (settrayin == 0 || settrayout == 0)
            {
                MessageBox.Show("Hay set so tray in, tray out");
                return;
            } 

            bstart = true;
            lampset(LG);
        } 

        private void btCycle_Click(object sender, EventArgs e)
        {
            if (!bstart)
            {
                MessageBox.Show("EQ chua Auto Run");
                return;
            } 

            bcycle = true;
        } 

        private void btPause_Click(object sender, EventArgs e)
        {
            if (!bstart)
            {
                MessageBox.Show("EQ chua Auto Run");
                return;
            } 

            bpause = true;
            c1.stop();
            c2.stop();
            c3.stop();
            c4.stop();
            stepinitbk = stepinit;
            stepldbk = stepld;
            stepud1bk = stepud1;
            stepud2bk = stepud2;  
            stepinit = 0;
            stepld   = 0;
            stepud1  = 0;
            stepud2 = 0;
        } 

        private void btResume_Click(object sender, EventArgs e)
        {
            if (!bpause)
            {
                MessageBox.Show("EQ chua Paused");
                return;
            } 

            bpause = false;
            stepinit = stepinitbk;
            stepld = stepldbk;
            stepud1 = stepud1bk;
            stepud2 = stepud2bk;
            stepinitbk = 0;
            stepldbk = 0;
            stepud1bk = 0;
            stepud2bk = 0;
        } 

        private void btInitial_Click(object sender, EventArgs e)
        {
            if (!isopen)
            {
                MessageBox.Show("PLC ngat ket noi");
                return;
            }
            if (bstart)
            {
                MessageBox.Show("EQ Auto Run");
                return;
            } 

            binit = false;
            stepinit = 10;
            lampset(LY + LG); //chua co nhay
            Mode = 0;
        } 

        private void btConnect_Click(object sender, EventArgs e)
        {
            connect();
        } 

        private void btDisconnect_Click(object sender, EventArgs e)
        {
            if (bstart)
            {
                MessageBox.Show("EQ dang Auto Run");
                return;
            } 

            disconnect();
        } 

        bool checkmode()
        {
            if (!isopen)
            {
                MessageBox.Show("PLC is Disconnected");
                return false;
            }
            if (Mode != 2)
            {
                MessageBox.Show("EQ is not Manual Mode");
                return false;
            }
            return true;
        } 

        private void button1_Click(object sender, EventArgs e)
        {
            if (checkmode()) c1.fw();
        } 

        private void button7_Click(object sender, EventArgs e)
        {
            if (checkmode()) c2.fw();
        } 

        private void button13_Click(object sender, EventArgs e)
        {
            if (checkmode()) c3.fw();
        } 

        private void button19_Click(object sender, EventArgs e)
        {
            if (checkmode()) c4.fw();
        } 

        private void button2_Click(object sender, EventArgs e)
        {
            if (checkmode()) c1.bw();
        } 

        private void button8_Click(object sender, EventArgs e)
        {
            if (checkmode()) c2.bw();
        } 

        private void button14_Click(object sender, EventArgs e)
        {
            if (checkmode()) c3.bw();
        } 

        private void button20_Click(object sender, EventArgs e)
        {
            if (checkmode()) c4.bw();
        } 

        private void button3_Click(object sender, EventArgs e)
        {
            if (checkmode()) c1.stop();
        } 

        private void button9_Click(object sender, EventArgs e)
        {
            if (checkmode()) c2.stop();
        } 

        private void button15_Click(object sender, EventArgs e)
        {
            if (checkmode()) c3.stop();
        } 

        private void button21_Click(object sender, EventArgs e)
        {
            if (checkmode()) c4.stop();
        } 

        private void button25_Click(object sender, EventArgs e)
        {
            spd = 2;
        } 

        private void button26_Click(object sender, EventArgs e)
        {
            spd = 1;
        } 

        private void button27_Click(object sender, EventArgs e)
        {
            spd = 0;
        } 

        private void button4_Click(object sender, EventArgs e)
        {
            if (checkmode()) x1.go();
        } 

        private void button10_Click(object sender, EventArgs e)
        {
            if (checkmode()) x2.go();
        } 

        private void button16_Click(object sender, EventArgs e)
        {
            if (checkmode()) x3.go();
        } 

        private void button22_Click(object sender, EventArgs e)
        {
            if (checkmode()) x4.go();
        } 

        private void button11_Click(object sender, EventArgs e)
        {
            if (checkmode()) y2.go();
        } 

        private void button17_Click(object sender, EventArgs e)
        {
            if (checkmode()) y3.go();
        } 

        private void button5_Click(object sender, EventArgs e)
        {
            if (checkmode()) z1.go();
        } 

        private void button23_Click(object sender, EventArgs e)
        {
            if (checkmode()) z4.go();
        } 

        private void button6_Click(object sender, EventArgs e)
        {
            if (checkmode()) s1.go();
        } 

        private void button24_Click(object sender, EventArgs e)
        {
            if (checkmode()) s4.go();
        } 

        private void button31_Click(object sender, EventArgs e)
        {
            if (checkmode()) lb.go();
        } 

        private void button32_Click(object sender, EventArgs e)
        {
            if (checkmode()) lg.go();
        } 

        private void button33_Click(object sender, EventArgs e)
        {
            if (checkmode()) ly.go();
        } 

        private void button34_Click(object sender, EventArgs e)
        {
            if (checkmode()) lr.go();
        } 

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            setalign1 = (int)numericUpDown1.Value;
        } 

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            setalign2 = (int)numericUpDown2.Value;
        } 

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            setalign3 = (int)numericUpDown3.Value;
        } 

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            setalign4 = (int)numericUpDown4.Value;
        } 

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            settrayin = (int)numericUpDown5.Value;
        } 

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            settrayout = (int)numericUpDown6.Value;
        }
    }   
}1
