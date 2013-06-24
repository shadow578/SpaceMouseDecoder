using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        //SerialPort sp = new SerialPort("COM1", 57600, Parity.None, 8, StopBits.One);
        delegate void Invoker(string parameter);
        delegate void Invoker2(int param1, int param2);

        public Form1()
        {
            InitializeComponent();
            ButtonCloseSerial.Enabled = false;
            statusLabel.Text = ""; 
            
        }

        public void openSerial()
        {

            if (!(sp.IsOpen))
            {
                try
                {
                    statusLabel.Text = "Opening port...";
                    sp.ReadTimeout = 3000;
                    sp.WriteTimeout = 5000;
                    sp.Open();
                    statusLabel.Text = "Port open.";
                    ButtonCloseSerial.Enabled = sp.IsOpen;
                    ButtonOpenSerial.Enabled = !sp.IsOpen;
                }
                catch (UnauthorizedAccessException ex)
                {
                    statusLabel.Text = "Port open failed.";
                    MessageBox.Show(ex.Message);
                }              
            }
        }

        public void closeSerial()
        {
            if (!(sp == null))
            {
                // The COM port exists.
                if (sp.IsOpen)
                {
                    statusLabel.Text = "Closing port...";
                    // Wait for the transmit buffer to empty.
                    while (sp.BytesToWrite > 0)
                    {
                    }
                    // The COM port is open; close it and dispose of its resources.
                    sp.Dispose();
                    statusLabel.Text = "Port closed.";
                    ButtonCloseSerial.Enabled = sp.IsOpen;
                    ButtonOpenSerial.Enabled = !sp.IsOpen;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openSerial();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            closeSerial();        
        }

        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            string data = sp.ReadExisting();
            if (this.InvokeRequired)
            {
                // Execute the same method, but this time on the GUI thread
                this.BeginInvoke(new Invoker(setText), data);

                // we return immidiately
                return;
            }

            // From here on it is safe to access methods and properties on the GUI
            // For example:
            /*foreach (char c in data)
            {

            }*/
            setText(data);
        }
        int iUpdate = 0;
        string text = "", text1 = "", text2 = "", text3 = "", text4 = "", text5 = "", text6 = "";
        int x=0, y=0, z=0, u=0, v=0, w=0;
        private void setText(string sIn)
        {
            
            
            

            if (sIn[0].Equals('k'))
            {
                this.textBox1.Text = "Button Pressed: " + ToBinary((DecodeSpacemouse(sIn[1]) + DecodeSpacemouse(sIn[2]) * 16 + DecodeSpacemouse(sIn[3]) * 256));
                //this.textBox1.Text = sIn;
            }
            if (sIn[0].Equals('d'))
            {


                iUpdate++;
                


                int n = 1;
                int temp = ((DecodeSpacemouse(sIn[n + 1]) * 256 + DecodeSpacemouse(sIn[n + 2]) * 16 + DecodeSpacemouse(sIn[n + 3]) * 1));
                if (sIn[n].Equals('G'))
                {                    
                    x = -(4096 -temp);              
                }
                else if (sIn[n].Equals('H'))
                {
                    x = temp;
                }
                text1 = "X: " + x;

                n = 5;
                temp = (4096 - (DecodeSpacemouse(sIn[n + 1]) * 256 + DecodeSpacemouse(sIn[n + 2]) * 16 + DecodeSpacemouse(sIn[n + 3]) * 1));
                if (sIn[n].Equals('G'))
                {
                    z = -temp;
                }
                else if (sIn[n].Equals('H'))
                {
                    z = (4096 - temp);
                }
                text2 = "Z: " + z;

                n = 9;
                temp = (4096 - (DecodeSpacemouse(sIn[n + 1]) * 256 + DecodeSpacemouse(sIn[n + 2]) * 16 + DecodeSpacemouse(sIn[n + 3]) * 1));
                if (sIn[n].Equals('G'))
                {
                    y = temp;
                }
                else if (sIn[n].Equals('H'))
                {
                    y = -(4096 - temp);
                }
                text3 = "Y: " + y;
             
            }                
            if (sIn.Length == 12)
            {
                iUpdate++;
                int temp = 0;
                int n = 0;
                temp = (DecodeSpacemouse(sIn[n]) * 256 + DecodeSpacemouse(sIn[n + 1]) * 16 + DecodeSpacemouse(sIn[n + 2]));
                if (temp > 1000)
                {
                    u = -(4096 - temp);
                }
                else u = temp;
                text4 = "u: " + u;
                //}

                n = 3;

                temp = (4096 - (DecodeSpacemouse(sIn[n + 1]) * 256 + DecodeSpacemouse(sIn[n + 2]) * 16 + DecodeSpacemouse(sIn[n + 3]) * 1));
                if (sIn[n].Equals('G'))
                {
                    w = -temp;
                }
                else if (sIn[n].Equals('H'))
                {
                    w = (4096 - temp);
                }
                text5 = "w: " + w;


                n = 7;
                temp = (4096 - (DecodeSpacemouse(sIn[n + 1]) * 256 + DecodeSpacemouse(sIn[n + 2]) * 16 + DecodeSpacemouse(sIn[n + 3]) * 1));
                if (sIn[n].Equals('G'))
                {
                    v = -temp;
                }
                else if (sIn[n].Equals('H'))
                {
                    v = (4096 - temp);
                }
                text6 = "v: " + v;
            }
            if (iUpdate == 2)
            {
                this.textBox1.Text = text1 + "\r\n" + text3 + "\r\n" + text2 + "\r\n\r\n" + text4 + "\r\n" + text6 + "\r\n" + text5;
                iUpdate = 0;
                if (this.InvokeRequired)
                {
                    // Execute the same method, but this time on the GUI thread
                    this.BeginInvoke(new Invoker2(setPositionDot), x, y);
                }
                setPositionDot(x, y);
            }

            
                
        }

        public void setPositionDot(int x, int y)
        {
            Point p = new Point((int)(100 + x*0.2)-7,(int)(100 -y*0.2)-7);
            radioButton1.Location = p;
            System.Drawing.Pen myPen;
            myPen = new System.Drawing.Pen(System.Drawing.Color.Red);
            System.Drawing.Graphics formGraphics = this.CreateGraphics();
            formGraphics.Clear(Color.WhiteSmoke);
            formGraphics.DrawLine(myPen, panel1.Location.X + radioButton1.Location.X - 7, panel1.Location.Y + radioButton1.Location.Y - 7, panel1.Location.X + radioButton1.Location.X - 7 - (float)((Math.Cos((w / 2) * Math.PI / 180)) * 50), panel1.Location.Y + radioButton1.Location.Y - 7 - (float)(Math.Sin((w / 2) * Math.PI / 180)) * 50);
            myPen.Dispose();
            formGraphics.Dispose();
            
        }

        public int DecodeSpacemouse(char c)
        {
            int n;

            switch (c)
            {
                case '0': n = 0; break;
                case 'A': n = 1; break;
                case 'B': n = 2; break;
                case '3': n = 3; break;
                case 'D': n = 4; break;
                case '5': n = 5; break;
                case '6': n = 6; break;
                case 'G': n = 7; break;
                case 'H': n = 8; break;
                case '9': n = 9; break;
                case ':': n = 10; break;
                case 'K': n = 11; break;
                case '<': n = 12; break;
                case 'M': n = 13; break;
                case 'N': n = 14; break;
                case '?': n = 15; break;
                default: n = 0; break;
            }

            return (n);
        }

        private string ToBinary(int Decimal)
        {

           // Declare a few variables we're going to need
           int BinaryHolder;
           char[] BinaryArray;
           string BinaryResult = "";

           while (Decimal > 0)
           {
              BinaryHolder = Decimal % 2;
              BinaryResult += BinaryHolder;
              Decimal = Decimal / 2;
           }
           // The algoritm gives us the binary number in reverse order (mirrored)
           // We store it in an array so that we can reverse it back to normal

           BinaryArray = BinaryResult.ToCharArray();
           Array.Reverse(BinaryArray);
           BinaryResult = new string(BinaryArray);
           return BinaryResult;

        }
    }
}
