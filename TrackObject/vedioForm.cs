using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using PlayVideoTest;

namespace TrackObject
{
    public partial class vedioForm : Form
    { 
        public vedioForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.AutoSize = true;
            
            MI = new MediaInfo();
            fileName = "D:\\Soccer.mp4";//define the file name to play
            MI.Open(fileName);
            VideoWidth = Convert.ToInt32(MI.Get(StreamKind.Video, 0, "Width"));
            VideoHeight = Convert.ToInt32(MI.Get(StreamKind.Video, 0, "Height"));
            duration = Convert.ToInt32(MI.Get(StreamKind.Video, 0, "Duration"));
            MI.Close();
            pictureBox1.Width = (int)((double)VideoWidth*0.8);
            pictureBox1.Height = (int)((double)VideoHeight*0.8);
            picX = pictureBox1.Location.X;
            picY = pictureBox1.Location.Y;
            picHeight = pictureBox1.Height;
            picWidth = pictureBox1.Width;

            cbSpeed.SelectedIndex = 4;
        }

        private void vedioForm_Load(object sender, EventArgs e)
        {
           
        }

        //Define Record to store the coordinate and time
        struct Record
        {
            private Point coordinate;
            public Point Coordinate
            {
                get { return coordinate; }
                set { coordinate = value; }
            }

            private String time;
            public String Time
            {
                get { return time; }
                set { time = value; }
            }

            public Record(Point _coordinate, String _time)
            {
                this.coordinate = _coordinate;
                this.time = _time;
            }

        }

        List<Record> recordList = new List<Record>();

        DateTime startTime = new DateTime();//time for starting
        DateTime stopTime = new DateTime();//time for ending
        System.Timers.Timer Timers_Timer = new System.Timers.Timer();//Definition of Timer   
        private MediaInfo MI;
        private String fileName;
        int picX;
        int picY;
        int picHeight;
        int picWidth;
        private int duration;
        private int VideoWidth;
        private int VideoHeight;
        void writeInFile(List<Record> recordList)
        {
            string fullPath = "D:\\"+tbDes.Text+"-Record.txt";
            StreamWriter sw;
            if (!File.Exists(fullPath))
            {
                //if file does not exist, then create a new one and wirte the content 
                sw = File.CreateText(fullPath);
                sw.Write("<" + tbDes.Text + ">#");
                for (int i = 0; i < recordList.Count; i++)
                {
                    sw.Write("<" + recordList[i].Time + "," + Convert.ToInt64((Convert.ToDecimal(recordList[i].Coordinate.X) / Convert.ToDecimal(picWidth))*10000) + "," + Convert.ToInt64((Convert.ToDecimal(recordList[i].Coordinate.Y)/Convert.ToDecimal(picHeight))*10000) + ">#");
                }
            }
            else
            {
                File.Delete(fullPath);
                //if file already exists, append content 
                sw = File.CreateText(fullPath);
                sw.Write("<" + tbDes.Text + ">#");
                for (int i = 0; i < recordList.Count; i++)
                {
                    sw.Write("<" + recordList[i].Time + "," + Convert.ToInt64((Convert.ToDecimal(recordList[i].Coordinate.X) / Convert.ToDecimal(picWidth)) * 10000) + "," + Convert.ToInt64((Convert.ToDecimal(recordList[i].Coordinate.Y) / Convert.ToDecimal(picHeight)) * 10000) + ">#");
                }
            }
            sw.Close();
        }

        void Timers_Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            label2.Text = Convert.ToString(DateTime.Now - startTime);//update current time to lable  
            TimeSpan spanStart = DateTime.Now.Subtract(startTime);
            double milliSecondsStartTime = spanStart.TotalSeconds;
            TimeSpan spanNow = DateTime.Now.Subtract(DateTime.Now);
            double milliSecondsNowTime = spanNow.TotalSeconds;
            label3.Text = Convert.ToString((milliSecondsStartTime - milliSecondsNowTime) * Convert.ToDouble(cbSpeed.Text));
            if ((milliSecondsStartTime - milliSecondsNowTime) * 1000 * Convert.ToDouble(cbSpeed.Text) > duration)
            {
                Timers_Timer.Enabled = false;
                writeInFile(recordList);//write record to file
            }
            if (isRecording)
            {
                Point ps = MousePosition;//point according to screen
                Point pw = this.PointToClient(ps);//point according to windows
                Point pr = new Point(pw.X - picX, pw.Y - picY);//the real coordinate according to pictureBox1
                if (pr.X < 0 || pr.X > picWidth || pr.Y < 0 || pr.Y > picHeight)
                {
                    coordinateText.Text = "Out of Bound!";
                }
                else
                {
                    coordinateText.Text = "(" + pr.X + "," + pr.Y + ")";
                    Record record = new Record(pr, Convert.ToString(Convert.ToInt64((milliSecondsStartTime - milliSecondsNowTime) * 1000 * Convert.ToDouble(cbSpeed.Text))));
                    recordList.Add(record);
                }
            }
            Graphics g = Graphics.FromImage(pictureBox1.Image);
            Pen p = new Pen(Color.Red);
            g.DrawEllipse(p, 0, 0, 100, 100);
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            PictureBox PlayScreen = pictureBox1;
            string mciCommand;
            mciCommand = "open " + " "+fileName+" " + " alias MyAVI ";
            mciCommand = mciCommand + " parent " + PlayScreen.Handle.ToInt32() + " style child ";
            LibWrap.mciSendString(mciCommand, null, 0, 0);
            Rectangle r = PlayScreen.ClientRectangle;
            mciCommand = " put MyAVI window at 0 0 " + r.Width + " " + r.Height;
            LibWrap.mciSendString(mciCommand, null, 0, 0);
            LibWrap.mciSendString(" set MyAVI speed "+1000*Convert.ToDouble(cbSpeed.Text), null, 0, 0);
            LibWrap.mciSendString(" play MyAVI ", null, 0, 0);
            //Set Timer and start playing video   
            Timers_Timer.Interval = (int)(100.0/Convert.ToDouble(cbSpeed.Text));
            Timers_Timer.Enabled = true;
            Timers_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Timers_Timer_Elapsed);
            startTime = DateTime.Now;//这边得稍微改一下。因为不小心又按到了start键的话，计时就重新开始了
        }


        private void stopButton_Click(object sender, EventArgs e)
        {
            //stop playing video   
            Timers_Timer.Enabled = false;
            stopTime = DateTime.Now;
            //write record to file
            writeInFile(recordList);
            LibWrap.mciSendString(" stop MyAVI ", null, 0, 0);
        }

        Boolean isRecording = false;//initialize the flag of isRecording
        //press space key and start or end record coordinate
        private void vedioForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R)
            {
                
                isRecording = !isRecording;
                recording.Checked = isRecording;
            }
        }

        //track mouse
        private Point delta;
        private Boolean deltaInitial = false;
        private void vedioForm_MouseMove(object sender, MouseEventArgs e)
        {
            
            if (deltaInitial)
                return;

            Point ps = MousePosition;//point according to screen
            Point pw = this.PointToClient(ps);//point according to windows
            delta.X = pw.X - e.X;
            delta.Y = pw.Y - e.Y;
            deltaInitial = true;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void vedioForm_Load_1(object sender, EventArgs e)
        {

        }

    }
    public class LibWrap
    {
        [DllImport(("winmm.dll "), EntryPoint = "mciSendString", CharSet = CharSet.Auto)]
        public static extern int mciSendString(string lpszCommand, string lpszReturnString,
                    uint cchReturn, int hwndCallback);
    }


}
