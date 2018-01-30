using System;
using System.IO;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Net;
//using System.Json;
using System.Web.Script.Serialization;// .Extensions;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Imaging;
using System.Timers;
using Microsoft.Win32;


namespace XP_Monitor
{
    public partial class Form1 : Form
    {
        static string[][] typesArr;
        static int typesNum;
        byte[] m_buffer = new byte[1000];
        byte[] m_SenID = new byte[4] { 0, 0, 0, 0 };
        long[] m_ID = new long[10];
        int length;
        byte m_Param;
        byte m_GetOrSet;
        byte m_numSen;
        static int nEventCntr;
        byte[] m_RomVer = new byte[4] { 0, 0, 0, 0 };
        string m_sFactory;
        string m_sWorker;
        bool m_bConnected2Logger;
        long m_nAllocID;
        //private static System.Timers.Timer waitTimer;
        Color firstColor;

        byte[] beep = new byte[8] { 0x42, 0x45, 0x45, 0x50, 0x42, 0x45, 0x45, 0x50 }; // "BEEPBEEP"

        public Form1()
        {
            InitializeComponent();
            m_numSen = 0;
            nEventCntr = 0;
            m_bConnected2Logger = false;

            firstColor = brnClrBtn.BackColor;
            /*
            // Create a timer with a two second interval.
            waitTimer = new System.Timers.Timer(5000);
            // Hook up the Elapsed event for the timer. 
            waitTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            //ElapsedEventHandler(OnTimedEvent);// timerEndProc_Tick;//OnTimedEvent;
            waitTimer.AutoReset = true;
             * */
        }

        //private void SetTimer()
        //{
        //    waitTimer.Enabled = true;
        //}

        //private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        //{
        //    ClearBtns();
        //    waitTimer.Enabled = false;
        //}

        private void OpenPort_Click(object sender, EventArgs e)
        {
            if (comboPorts.Text == "")
            {
                MessageBox.Show("No selected Port");
                return;
            }
            if (OpenPortBtn.Text.CompareTo("Open Port") == 0)
            {
                serialPort1.PortName = comboPorts.Text;  
                //port.PortName = "COM3";
                //port.BaudRate = 19200;
                //port.Parity = Parity.None;
                //port.DataBits = 8;
                //port.StopBits = StopBits.One;
                try
                {
                    serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived_1);
                    nEventCntr++;
                    serialPort1.Open();
                    pictureBox1.Image = XP_Monitor.Properties.Resources.Lock_Unlock_icon;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Open PORT");
                }
                if (!serialPort1.IsOpen)
                    MessageBox.Show("Cant open PORT");
                else
                    OpenPortBtn.Text = "Close Port";
            }
            else
            {
                try
                {
                    serialPort1.Close();
                    serialPort1.DataReceived -= new SerialDataReceivedEventHandler(serialPort1_DataReceived_1);
                    nEventCntr--;
                    OpenPortBtn.Text = "Open Port";
                    pictureBox1.Image = XP_Monitor.Properties.Resources.Lock_Lock_icon;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Close PORT");
                }
            }
        }

        delegate void SetTextCallback(Control c, string text);

        delegate void SetSelectedItemCallback(ComboBox c, string text);

        private void SetText(Control c, string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            //if (this.textID.InvokeRequired)
            if (c.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { c, text });
            }
            else
            {
                c.Text = text;
            }
        }

        delegate void AddTextCallback(RichTextBox c, string text);

        private void AddText(RichTextBox c, string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            //if (this.textID.InvokeRequired)
            if (text == null)
                return;
            try
            {
                if (c.InvokeRequired)
                {
                    AddTextCallback d = new AddTextCallback(AddText);
                    this.Invoke(d, new object[] { c, text });
                }
                else
                {
                    c.AppendText(text);
                    //c.Text += text;
                    c.SelectionStart = c.Text.Length;
                    c.ScrollToCaret();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void SetSelectedItem(ComboBox c, string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            //if (this.textID.InvokeRequired)
            if (c.InvokeRequired)
            {
                SetSelectedItemCallback f = new SetSelectedItemCallback(SetSelectedItem);
                this.Invoke(f, new object[] { c, text });
            }
            else
            {
                int n = c.FindStringExact(text);
                //c.SelectedItem = n;
                c.Text = c.Items[n].ToString();
            }
        }

        private bool CheckBuf(int l)
        {
            int n = 0;
            int i = 0;
            do
            {
                if (m_buffer[i].Equals(beep[n]))
                    n++;
                else
                    n = 0;
                i++;
            }
            while ((n < 8) && (i < l));
            // new connection recognized:
            if (n == 8)
            {
                SetText(textTime, DateTime.Now.ToString());
                ClearReadBuf();
                try
                {
                    serialPort1.DataReceived -= new SerialDataReceivedEventHandler(serialPort1_DataReceived_1);
                    nEventCntr--;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Check Buffer");
                }
                m_numSen = 0;
                HelloSensor();
                return true;
            }
            return false;
        }

        private void HelloSensor()
        {
            //MessageBox.Show("HelloSensor");
            //if (comboBoxIDs.Items.Count > 0)
            //    comboBoxIDs.Items.Clear();
            //MessageBox.Show("Please wait while system get sensor's IDs");
            if (BuildPrtocol(0, 0))
            {
                //if (BuildPrtocol(59, 0))            // get num of sensors in logger
                //{
                //    if (m_numSen >= 1)
                //    {
                //        if (m_numSen > 9)
                //            m_numSen = 9;
                //        for (byte i = 1; i <= m_numSen; i++)
                //            BuildPrtocol(i, 0);
                //        comboBoxIDs.SelectedIndex = 0;
                //    }
                //}
                //else
                    m_numSen = 0;
                    m_bConnected2Logger = true;
                BuildPrtocol(58, 0);       //get software version
                BuildPrtocol(55, 0);       //get battery
            }
        }

        private void ClearReadBuf()
        {
            for (int i = 0; i < 100; i++)
                m_buffer[i] = 0;
        }

        private void btnSlctAll_Click(object sender, EventArgs e)
        {
            if (btnSlctAll.Text == "Select All")
            {
                foreach (Control c in tabControl1.SelectedTab.Controls)
                {
                    if (c is CheckBox)
                    {
                        //if (c != configCheck)
                        if ((c.Enabled == true) && (c.Visible == true))
                            ((CheckBox)(c)).Checked = true;
                    }
                }
                checkBox2.Checked = false;      // do not put set ID as checked - need to do manually
                btnSlctAll.Text = "Unselect All";
            }
            else
            {
                foreach (Control c in tabControl1.SelectedTab.Controls)
                {
                    if (c is CheckBox)
                    {
                        ((CheckBox)(c)).Checked = false;
                    }
                }
                btnSlctAll.Text = "Select All";
            }
        }

        private void btnGetProp_Click(object sender, EventArgs e)
        {
            SendCommand(0);
        }

        private void btnSetProp_Click(object sender, EventArgs e)
        {
            SendCommand(1);
        }

        private static int CompareTabIndex(CheckBox c1, CheckBox c2)
        {
            return c1.TabIndex.CompareTo(c2.TabIndex);
        }

        private void SendCommand(byte bSet/*, Control c*/)
        {
            if (serialPort1.IsOpen == false)
                return;
            btnSlctAll.Text = "Select All";
            //order all check box by tab order:
            List<CheckBox> ca = new List<CheckBox>();
            foreach (Control ctl in tabControl1.SelectedTab.Controls)
            {
                if (ctl is CheckBox)
                    if (((CheckBox)(ctl)).Checked == true)
                        ca.Add((CheckBox)(ctl));
            }

            Comparison<CheckBox> cc = new Comparison<CheckBox>(CompareTabIndex);
            ca.Sort(cc);

            //foreach (Control c in Controls)
            foreach (CheckBox c in ca)
            {
                switch (c.Name)
                {
                    case "checkBox2":      // set newID
                        BuildPrtocol(0, bSet);
                        break;
                    case "cbMsr":       //measure
                    case "cbIntrvl":       //interval
                    case "cbType":       //type
                        if (comboBoxIDs.Items.Count == 0)
                        {
                            MessageBox.Show("No Sensors in list. Impossible to get information");
                            break;
                        }
                        if (comboBoxIDs.SelectedIndex == -1)
                        {
                            MessageBox.Show("First select sensor from list.");
                            break;
                        }
                        if (bSet == 1) //|| (comboBoxIDs.SelectedIndex == 0))      //set is disable for this parameter. if combo is showing logger ID and user asked for measurement
                            break;
                        if (c.Name == "cbMsr")
                            BuildPrtocol(10, 0);
                        if (c.Name == "cbIntrvl")                        
                            BuildPrtocol(20, 0);
                        if (c.Name == "cbType")
                            BuildPrtocol(30, 0);
                        break;
                        case "checkBox30":       //time
                        BuildPrtocol(40, bSet);
                        break;
                    case "checkBox28":       //battery
                        if (bSet == 1)      //set disable for this parameter
                            break;
                        BuildPrtocol(55, 0);
                        break;
                    case "checkBox29":       //rssi
                        if (bSet == 1)      //set disable for this parameter
                            break;
                        BuildPrtocol(56, 0);
                        break;
                    case "checkBox39":       //start con hour
                        BuildPrtocol(47, bSet);
                        break;
                    case "checkBox40":       //con per day
                        BuildPrtocol(48, bSet);
                        break;
                    case "checkBox38":       //interval conn
                        BuildPrtocol(49, bSet);
                        break;
                    case "checkBox37":       //IP
                        BuildPrtocol(41, bSet);
                        break;
                    case "checkBox36":       //port
                        BuildPrtocol(42, bSet);
                        break;
                    case "checkBox35":       //APN
                        BuildPrtocol(43, bSet);
                        break;
                    case "checkBox34":       //mmc
                        BuildPrtocol(44, bSet);
                        break;
                    case "checkBox33":       //mnc
                        BuildPrtocol(45, bSet);
                        break;
                    case "checkBox32":       //roaming
                        BuildPrtocol(46, bSet);
                        break;
                    //case "checkBox27":       //password
                    //    BuildPrtocol(50, bSet);
                    //    break;
                    //case "checkBox26":       //option1
                    //    BuildPrtocol(51, bSet);
                    //    break;
                    //case "checkBox25":       //option2
                    //    BuildPrtocol(52, bSet);
                    //    break;
                    //case "checkBox19":       //offset
                    //    BuildPrtocol(53, bSet);
                    //    break;
                    //case "checkBox20":       //gain
                    //    BuildPrtocol(54, bSet);
                    //    break;
                    case "cbTmeZone":       //timezone offset
                        BuildPrtocol(57, bSet);
                        break;                        
                    case "checkBox24":       //Rom Version
                        if (bSet == 1)      //set disable for this parameter
                            break;
                        BuildPrtocol(58, bSet);
                        break;
                    case "cbEpoch":       //epoch time
                        if (bSet == 1)
                        {
                            if (MessageBox.Show("Are you sure you want to change epoch time?", "Change Epoch Time", MessageBoxButtons.YesNo) == DialogResult.No)
                                break;
                        }
                        BuildPrtocol(64, bSet);
                        break;

                } //switch
//                if (c.Name != "checkBox1")      // check box of set combination always on 
                 ((CheckBox)(c)).Checked = false;        // all others - false
            }
        }

        private byte GetCheckSum(int len)
        {
            byte check_sum = 0;

            for (int i = 0; i < len; i++)
            {
                check_sum += m_buffer[i];// *buff++;                
            }
            return (check_sum);
            //return 1;
        }

        private bool UnpackBuffer()
        {
            //AddText(richTextBox1, "unpack buffer\n");
            //for (int i = 0; i < length; i++)
            //    m_buffer[i] = Convert.ToByte(m_readBuf[i]);
            // min length is 10
            //if (length < 10)
            //    return false;
            if (m_buffer[0] != 0xFF)
                return false;
            if (m_buffer[1] != 0xFF)
                return false;
            // if length less than what written in length byte
            if (length < m_buffer[2])
                return false;
            // check id - only if not asking for id
            if ((m_Param != 0) && (m_Param != 60))
                for (int i = 0; i < 4; i++)
                    if (m_buffer[i + 3] != m_SenID[i])
                        return false;

            if (m_buffer[7] != m_Param)
                //if request is anything but id request
                if (m_Param >= 10)
                    return false;
            length = m_buffer[2];
            // check sum
            if (GetCheckSum(length) != m_buffer[length])
                return false;

            if (m_GetOrSet == 1)    //if set was sent
            {
                //if set new combination was ok
                if ((m_Param == 60) && (m_buffer[8]) == 1)
                {
                    HelloSensor();
                    return true;
                }
                return Convert.ToBoolean(m_buffer[8]);
            }
            else
            {
                Int16 value;

                switch (m_Param)
                {
                    case 0:    //ID
                    case 1:    //ID
                    case 2:    //ID
                    case 3:    //ID
                    case 4:    //ID
                    case 5:    //ID
                    case 6:    //ID
                    case 7:    //ID
                    case 8:    //ID
                    case 9:    //ID
                        if (m_Param == 0)
                            Buffer.BlockCopy(m_buffer, 3, m_SenID, 0, 4);
                        m_ID[m_Param] = BitConverter.ToInt32(m_buffer, 8);
                        if (m_Param == 0)
                            SetText(textBoxLgrID, m_ID[0].ToString());
                        else
                            comboBoxIDs.Items.Add(m_ID[m_Param].ToString());
                        break;
                    //case 10:    //Measure
                    //case 11:
                    //case 12:
                    //case 13:
                    //case 14:
                    //case 15:
                    //case 16:
                    //case 17:
                    //case 18:
                    //case 19:
                    //    value = BitConverter.ToInt16(m_buffer, 8);
                    //    //textLastMsr.Text = value.ToString();
                    //    SetText(textLastMsr, value.ToString());
                    //    break;
                    case 20:    //interval
                    case 21:
                    case 22:
                    case 23:
                    case 24:
                    case 25:
                    case 26:
                    case 27:
                    case 28:
                    case 29:
                        value = (Int16)m_buffer[8];
                        value *= 10;
                        SetSelectedItem(comboBoxIntervals, value.ToString());
                        //comboBoxIntervals.SelectedItem = value;
                        break;
                    case 30:    //type
                    case 31:
                    case 32:
                    case 33:
                    case 34:
                    case 35:
                    case 36:
                    case 37:
                    case 38:
                    case 39:
                        textType.Text = Convert.ToString(m_buffer[8]);
                        if (m_buffer[8] == 0)
                            textType.Text = "None";
                        else
                        {
                            for (int i = 0; i < typesNum; i++)
                            {
                                byte b = Convert.ToByte(typesArr[i][0]);
                                if (m_buffer[8] == b)
                                {
                                    textType.Text = typesArr[i][1];
                                    break;
                                }
                            }
                        }
                        //SetText(textType, Bytes2Str(10));
                        break;
                    case 40:    //time
                        try
                        {
                            DateTime dt = new DateTime(2000 + (int)m_buffer[8], (int)m_buffer[9], (int)m_buffer[10], (int)m_buffer[11], (int)m_buffer[12], 0);
                            SetText(textTime, dt.ToString());
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                        //textTime.Text = dt.ToString();                        
                        break;
                    case 41:    //IP
                        //textIP.Text = Bytes2Str(32);
                        SetText(textIP, Bytes2Str(32));
                        break;
                    case 42:    //port  
                        SetText(textPort, Bytes2Str(4));
                        //textPort.Text = Bytes2Str(4);
                        break;
                    case 43:    //apn
                        SetText(textAPN, Bytes2Str(32));
                        //textAPN.Text = Bytes2Str(32);
                        break;
                    case 44:    // mcc
                        SetText(textMCC, Bytes2Str(4));
                        //textMCC.Text = Bytes2Str(4);
                        break;
                    case 45:    //mnc
                        SetText(textMNC, Bytes2Str(4));
                        break;
                    case 46:    //roaming
                        if (m_buffer[8] == 1)       // if answer is 1 = auto connect = roaming = no use country code
                            SetSelectedItem(comboBoxRoaming, "No");
                        //comboBoxRoaming.SelectedItem = "Yes";
                        else
                            SetSelectedItem(comboBoxRoaming, "Yes");    // if answer is 0 =  no auto connect = no roaming = use country code
                        //comboBoxRoaming.SelectedItem = "No";
                        break;
                    case 47:    // start con
                        SetText(textStartH, Convert.ToString(m_buffer[8]));
                        break;
                    case 48:    //con per day
                        SetText(textPerDay, Convert.ToString(m_buffer[8]));
                        break;
                    case 49:    //con interval
                        SetText(textIntCon, Convert.ToString(m_buffer[8]));
                        break;
                    case 55:    //battery
                        value = BitConverter.ToInt16(m_buffer, 8);
                        //textBat.Text = value.ToString();
                        SetText(textBat, value.ToString());
                        break;
                    case 56:    //RSSI
                        value = Convert.ToInt16(m_buffer[8]);
                        //textRssi.Text = value.ToString();
                        SetText(textRssi, value.ToString());
                        break;
                    case 57:    //TimeZone offset
                        value = BitConverter.ToInt16(m_buffer, 8);
                    //    value -= 100;
                        SetText(txtTimeZone, value.ToString());
                        break;
                    case 58:    //Rom Version
                        ParseVersion();
                        break;
                    case 59:    //number of sensors
                        m_numSen = m_buffer[8];
                        break;
                   case 64:     //epoch
                        DateTime dt1 = new DateTime(2000+m_buffer[13], m_buffer[12], m_buffer[11], m_buffer[10], m_buffer[9], m_buffer[8]);
                        dateTimeEpoch.Value = dt1;   
                        break;
                }//switch

                return true;
            }  //if get command

        }  // func

        private void ParseVersion()
        {
            // save rom version
            for (int i = 0; i < 4; i++)
                m_RomVer[i] = m_buffer[8 + i];
            string sVer = new string((char)m_RomVer[0], 1);
            sVer += '.';
            sVer += Convert.ToString(m_RomVer[1]);
            sVer += '.';
            sVer += Convert.ToString(m_RomVer[2]);
            sVer += '.';
            sVer += Convert.ToString(m_RomVer[3]);
            SetText(textVersion, sVer);
            SetText(stSWVer, sVer);    // also put it in order info
        }

        private bool BuildPrtocol(byte prmIndex, byte getOrSet)
        {
            byte size = 0;
            int n;
            m_Param = prmIndex;
            m_GetOrSet = getOrSet;  //get = 0, set = 1

            m_buffer[0] = 0xFF;
            m_buffer[1] = 0xFF;
            for (int i = 0; i < 4; i++)
                m_buffer[3 + i] = m_SenID[i];
            m_buffer[7] = getOrSet;
            m_buffer[8] = prmIndex;
            // if ask for parameter depend on sensor id - add the sensor index
            //if ((getOrSet == 0) && (prmIndex < 40) && (prmIndex >= 10))
            //{
            //    if ((comboBoxIDs.SelectedIndex <= m_numSen) && (comboBoxIDs.SelectedIndex > -1))           // check that sensor index is reasonable
            //    {
            //        m_buffer[8] += (byte)comboBoxIDs.SelectedIndex;
            //        m_buffer[8]++;
            //    }
            //}
            if (getOrSet == 1)  //if set command
            {
                switch (prmIndex)
                {
                    case 0:
                        Buffer.BlockCopy(BitConverter.GetBytes(Convert.ToInt32(textID.Text)), 0, m_buffer, 9, 4);
                        size = 4;
                        break;
                    case 20:    //interval
                        //m_buffer[9] = Convert.ToByte((Convert.ToInt16(comboBoxIntervals.SelectedItem) / 10));
                        //size = 1;
                        break;
                    case 40:    //time
                        DateTime dt = DateTime.Now;
                        m_buffer[9] = (byte)(dt.Year - 2000);
                        m_buffer[10] = (byte)dt.Month;
                        m_buffer[11] = (byte)dt.Day;
                        m_buffer[12] = (byte)dt.Hour;
                        m_buffer[13] = (byte)dt.Minute;
                        size = 5;
                        break;
                    case 41:    //IP
                        Buffer.BlockCopy(StrtoBytes(textIP.Text, 32), 0, m_buffer, 9, 32);
                        size = 32;
                        break;
                    case 42:    //port  
                        Buffer.BlockCopy(StrtoBytes(textPort.Text, 4), 0, m_buffer, 9, 4);
                        size = 4;
                        break;
                    case 43:    //apn
                        Buffer.BlockCopy(StrtoBytes(textAPN.Text, 32), 0, m_buffer, 9, 32);
                        size = 32;
                        break;
                    case 44:    // mcc
                        Buffer.BlockCopy(StrtoBytes(textMCC.Text, 4), 0, m_buffer, 9, 4);
                        size = 4;
                        break;
                    case 45:    //mnc
                        Buffer.BlockCopy(StrtoBytes(textMNC.Text, 4), 0, m_buffer, 9, 4);
                        size = 4;
                        break;
                    case 46:    //roaming
                        if (comboBoxRoaming.SelectedItem.ToString() == "Yes")// if answer is 1 = send 0, sensor switch it to 1
                            m_buffer[9] = 0;
                        else
                            m_buffer[9] = 1;
                        size = 1;
                        break;
                    case 47:    // start con
                        m_buffer[9] = Convert.ToByte(textStartH.Text);
                        size = 1;
                        break;
                    case 48:    //con per day
                        m_buffer[9] = Convert.ToByte(textPerDay.Text);
                        size = 1;
                        break;
                    case 49:    //con interval
                        m_buffer[9] = Convert.ToByte(textIntCon.Text);
                        size = 1;
                        break;
                    case 57:    //TimeZone offset
                        n = Convert.ToInt16(txtTimeZone.Text);
                    //    n += 100;
                        m_buffer[9] = (byte)n;
                        m_buffer[10] = (byte)(n >> 8);
                        size = 2;
                        break;
                    //case 63:        //do measuring to all sensors
                    //    size = 0;
                    //    break;
                    case 64:
                        int year = dateTimeEpoch.Value.Year;
                        m_buffer[9] = (byte)dateTimeEpoch.Value.Second;     // second
                        m_buffer[10] = (byte)dateTimeEpoch.Value.Minute;    // minutes
                        m_buffer[11] = (byte)dateTimeEpoch.Value.Hour;      // hours
                        m_buffer[12] = (byte)dateTimeEpoch.Value.Day;       // day
                        m_buffer[13] = (byte)dateTimeEpoch.Value.Month;     // month
                        if (year > 2000)
                            year -= 2000;
                        m_buffer[14] = (byte)year;      // year
                        size = 6;
                        break;                    
                }
            }
                                  
            n = size + 9;
            m_buffer[2] = (byte)n;
            m_buffer[n] = GetCheckSum(n);
            AddText(richTextBox1, Buff2Log(false, n + 1));
            try
            {
                //serialPort1.DiscardInBuffer();
                serialPort1.Write(m_buffer, 0, n + 1);
                //serialPort1.DiscardOutBuffer();
                //clear buffer
                ClearReadBuf();
                Thread.Sleep(500);
                if (m_Param == 61)  //if disconnect from sensor
                    return true;
                length = serialPort1.BytesToRead;
                //AddText(richTextBox1, length.ToString());
                //if (length > 0)
                //    m_nTimeOut = 0;
                if (length > 45)
                    return false;
                serialPort1.Read(m_buffer, 0, length);
            }
            catch (Exception x)
            {
                MessageBox.Show("Exception data sending");
                MessageBox.Show(x.Message);
            }
            AddText(richTextBox1, Buff2Log(true, length));
            bool b = UnpackBuffer();
            if (b)
            {
                //if update sensor id:
                if ((prmIndex == 0) && (getOrSet == 1))
                {
                    Buffer.BlockCopy(m_buffer, 3, m_SenID, 0, 4);
                    m_ID[0] = BitConverter.ToInt32(m_SenID, 0);
                    textBoxLgrID.Text = m_ID[0].ToString();                        
                }
                AddText(richTextBox1, "Operation OK\n");
            }
            else
                AddText(richTextBox1, "Operation Failed\n");
            ClearReadBuf();
//            this.Refresh();
            return b;
        }

        private byte[] StrtoBytes(string str, int len)
        {
            byte[] myBytes = new byte[len];
            int i, n = str.Length;
            if (len < n)
                n = len;
            for (i = 0; i < n; i++)
                myBytes[i] = Convert.ToByte(str[i]);
            if (n < len)
            {
                myBytes[n] = (byte)'#';
                for (i = n + 1; i < len; i++)
                    myBytes[i] = 0;
            }
            return myBytes;
        }

        private string Buff2Log(bool Rx, int len)
        {
            string s;
            //char[] tmp = new char[45];
            if (Rx)
            {
                if (configCheck.Checked == false)
                    s = new string('>', 2);
                else
                    s = new string('-', 1);
            }
            else
                s = new string('<', 2);

            for (int i = 0; i < len; i++)
            {
                if ((configCheck.Checked) || (m_bConnected2Logger == false))
                {
                        s += Convert.ToChar(m_buffer[i]);
                }
                else
                {
                    s += Convert.ToString(m_buffer[i]);
                    s += ',';
                }
            }
            if (configCheck.Checked == false)
                s += '\n';

            return s;
        }

        private string Bytes2Str(int len)
        {
            char[] tmp = new char[32];

            for (int i = 0; i < len; i++)
                if (m_buffer[i + 8] != (byte)'#')
                    tmp[i] = Convert.ToChar(m_buffer[i + 8]);
                else
                    break;
            string s = new string(tmp);
            return s;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tabControl1.TabPages.Remove(tabPage3);
            tabControl1.TabPages.Remove(tabPage4);
            comboBoxIntervals.SelectedIndex = 0;
            comboBoxRoaming.SelectedIndex = 1;
            GetSensorTypes();
            textTime.Text = DateTime.Now.ToString();
            string[] ports = SerialPort.GetPortNames();
            if (ports.GetLength(0) > 0)
            {
                for (int i = 0; i < ports.GetLength(0); i++)
                    comboPorts.Items.Insert(i, ports[i]);
                comboPorts.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("Can't find any PORT to connect with");
            }
            //GetHWVers();
            // set default modem 
            MdmTypeCmb.SelectedIndex = 0;
            HardwareCmb.SelectedIndex = 0;
            
            WellcomePage dlg = new WellcomePage();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                m_sFactory = dlg.GetFactory();
                m_sWorker = dlg.GetWorker();
            }
        }

        private void ClrLogBtn_Click(object sender, EventArgs e)
        {
            richTextBox1.ResetText();
        }

        private void serialPort1_DataReceived_1(object sender, SerialDataReceivedEventArgs e)
        {
            bool bMonitor;
            Thread.Sleep(500);
            try
            {
                bMonitor = configCheck.Checked;
                if (!bMonitor)
                {
                    length = serialPort1.BytesToRead;
                    serialPort1.Read(m_buffer, 0, length);
                    AddText(richTextBox1, Buff2Log(true, length));
                    if (length > 45)
                        return;
                    //serialPort1.Read(m_buffer, 0, length);
                    //AddText(richTextBox1, Buff2Log(true, length));
                    if (CheckBuf(length) == true)
                        return;
                    //AddText(richTextBox1, Buff2Log(true, length));
                    bool b = UnpackBuffer();
                    ClearReadBuf();
                }
                else
                {
                    length = serialPort1.BytesToRead;
                    serialPort1.Read(m_buffer, 0, length);
                    AddText(richTextBox1, Buff2Log(true, length));
                    ClearReadBuf();
                }
            }
            catch (Exception x)
            {
                //MessageBox.Show("Exception data recieved");
                MessageBox.Show(x.Message, "Exception data recieved");
            }
            //m_bGotAnswer = true;

        }

        private void ResetAllControls()
        {
            foreach (TabPage tp in tabControl1.TabPages)
                foreach (Control c in tp.Controls)
                {
                    if (c is CheckBox)
                    {
                        ((CheckBox)(c)).Checked = false;
                        ((CheckBox)(c)).Enabled = true;
                    }
                    if (c is TextBox)
                        ((TextBox)(c)).ResetText();
                    if (c is ComboBox)
                        if (((ComboBox)(c)).Items.Count > 0)
                            ((ComboBox)(c)).SelectedIndex = 0;
                }
            btnSlctAll.Text = "Select All";
            comboBoxIDs.Text = "";
            comboBoxIDs.Items.Clear();
            comboBoxRoaming.SelectedIndex = 1;
            int i;
            for (i = 0; i < 4; i++)
            {
                m_SenID[i] = 0;
                m_RomVer[i] = 0;
            }
            for (i = 0; i < 10; i++)
                m_ID[i] = 0;

            // set default modem
            MdmTypeCmb.SelectedIndex = 0;
        }

        private void ResetBtn_Click(object sender, EventArgs e)
        {
            BuildPrtocol(61, 1);    // send to sensor diconnecting order
            m_bConnected2Logger = false;
            m_numSen = 0;
            try
            {
                //serialPort1.DataReceived -= new SerialDataReceivedEventHandler(serialPort1_DataReceived_1);
                if (nEventCntr == 0)
                {
                    serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived_1);
                    nEventCntr++;
                }                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            ResetAllControls();
            textBoxLgrID.ResetText();
        }

        private void configCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (configCheck.Checked == false)
            {
                //richTextBox1.Font = new Font("Arial", 12);
                //foreach (Control c in Controls)
                //{
                //    c.Enabled = true;
                //}
                tabControl1.Enabled = true;
                btnSlctAll.Enabled = true;
                btnGetProp.Enabled = true;
                btnSetProp.Enabled = true;
                ResetBtn.Enabled = true;
                //richTextBox1.Height = 173;
                //richTextBox1.Top = 330;
                //richTextBox1.Left = 28;

            }
            else
            {
                tabControl1.Enabled = false;
                btnSlctAll.Enabled = false;
                btnGetProp.Enabled = false;
                btnSetProp.Enabled = false;
                ResetBtn.Enabled = false;
                ResetBtn_Click(this, null);
                //foreach (Control c in Controls)
                //{
                //    if ((c != configCheck) && (c != richTextBox1) && (c !=  ClrLogBtn) && (c != SaveLogBtn))
                //        c.Enabled = false;
                //}
            }
        }

        private void SaveLogBtn_Click(object sender, EventArgs e)
        {
            SaveFileDialog sFile = new SaveFileDialog();
            sFile.DefaultExt = "*.rtf";
            sFile.Filter = "RTF Files|*.rtf|Text Files|*.txt";

            if (sFile.ShowDialog() == System.Windows.Forms.DialogResult.OK && sFile.FileName.Length > 0)
            {
                richTextBox1.SaveFile(sFile.FileName);
            }
        }
        
        private byte GetSelType(/*string fileName, */ComboBox c)
        {
            //int counter = 0;
            string[] strArr;
            string line;

            if (c.SelectedIndex == 0)
                return 0;

            strArr = c.Text.Split('\t');
            line = strArr[0];

            for (int i = 0; i < typesNum; i++)
            {
                if (line == typesArr[i][1])
                    return Convert.ToByte(typesArr[i][0]);
            }
            return 0;
        }

        private void FillCombo(/*string fileName, */ComboBox c)
        {
            //int counter = 0;
            string line;

            c.Items.Insert(0, "None");
            for (int i = 0; i < typesNum; i++)
            {
                line = typesArr[i][1] + "\t (" + typesArr[i][2] + ")";
                c.Items.Insert(i + 1, line);
            }
            c.SelectedIndex = 0;

            /*
            // Read the file and display it line by line.
            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader(fileName);
                while ((line = file.ReadLine()) != null)
                {
                    c.Items.Insert(counter, line);                     
                    counter++;
                }
                if (c.Items.Count > 0)
                    c.SelectedIndex = 0;

                file.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
             */
        }

        private void SetCombo(byte senType, ComboBox c)
        {
            //string[] strArr;
            //string line;

            if (senType == 0)
            {
                c.SelectedIndex = 0;
                return;
            }

            for (int i = 0; i < typesNum; i++)
            {
                byte b = Convert.ToByte(typesArr[i][0]);
                if (senType == b)
                {
                    c.SelectedIndex = i + 1;
                    return;
                }
            }
        }

        private void GetHWVers()
        {
            string line;
            string uriString = "http://46.101.79.233:3001/hardwares.json?user_id=1507&api_token=H8MwSswzsrzy7SybEf-V";
            try
            {
                WebClient webClient = new WebClient();
                Stream stream = webClient.OpenRead(uriString);
                StreamReader reader = new StreamReader(stream);
                String json = reader.ReadToEnd();
                JavaScriptSerializer jS = new JavaScriptSerializer();

                // Parse JSON into dynamic object, convenient!
                IList<HardwareObj> result = jS.Deserialize<IList<HardwareObj>>(json);
                for (int i = 0; i < result.Count; i++)
                {
                    line = result[i].version;
                    HardwareCmb.Items.Insert(i, line);
                }
                HardwareCmb.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void GetSensorTypes()
        {
            string line;
            int j;
            typesNum = 0;
            string uriString = @"http://plantbeat.phytech.com/sensor_types.json?user_id=1091&api_token=FrAnazu5rt67";
            //string uriString = @"http://plantbeat.phytech.com/activeadmin/sensor_types.json?per_page=1000&user_id=1504&api_token=UY-4R2zK-58wyuprwMus";
            try
            {
                WebClient webClient = new WebClient();
                Stream stream = webClient.OpenRead(uriString);
                StreamReader reader = new StreamReader(stream);
                String json = reader.ReadToEnd();
                JavaScriptSerializer jS = new JavaScriptSerializer();

                // Parse JSON into dynamic object, convenient!
                IList<SensorTypeObject> result = jS.Deserialize<IList<SensorTypeObject>>(json);
                typesArr = new string[result.Count][];
                j = 0;
                for (int i = 0; i < result.Count; i++)
                {
                    if ((result[i].@virtual == false) && (result[i].deprecated == false))
                    {
                        typesArr[j] = new string[2];
                        line = result[i].codename + " (" + result[i].title + ")";
                        typesArr[j][0] = result[i].id.ToString();
                        typesArr[j][1] = line;
                        j++;
                    }
                }
                typesNum = j;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private byte GetSensorTypeID(string sType)
        {
            for (int i = 0; i < typesNum; i++)
            {
                if (sType == typesArr[i][1])
                    return Convert.ToByte(typesArr[i][0]);
            }
            return 0;
        }  

        private void SndOrderBtn_Click(object sender, EventArgs e)
        {
            SendLoggerInfo();
            
            //string strAddress = "https://crm.zoho.com/crm/private/xml/Products/insertRecords?";
            //String strLine = "authtoken=4b8873e7a46d5cf77a027bda4feb3fe4&scope=crmapi&wfTrigger=true&xmlData=<Products><row no=\"1\">";
            //strLine += String.Format("<FL val=\"Product Name\">{0}</FL>", m_ID[0]);
            //strLine += String.Format("<FL val=\"Product Code\">{0}</FL>", m_ID[0]);
            //strLine += String.Format("<FL val=\"Status\">Manufactured</FL>");
            //strLine += String.Format("<FL val=\"production_date\">{0}</FL>", DateTime.Now); //yyyy-mm-dd hh:MM:ss
            //strLine += String.Format("<FL val=\"Product Active\">false</FL>");   //false 
            //strLine += String.Format("<FL val=\"Product Batch\">{0}</FL>", textBatch.Text);
            //strLine += String.Format("<FL val=\"wireless\">{0}</FL>", true);	//true or false
            //strLine += String.Format("<FL val=\"software_version\">{0}</FL>", textVersion.Text);
            //strLine += String.Format("<FL val=\"sensor types\">{0}</FL>", ""/*GetSensorsList()*/); //"1234,1235,1236")
            //strLine += String.Format("<FL val=\"hardware_version\">{0}</FL>", HardwareCmb.Text);
            //strLine += String.Format("<FL val=\"factory\">{0}</FL>", m_sFactory);
            //strLine += String.Format("<FL val=\"modem\">{0}</FL>", MdmTypeCmb.Text);
            //strLine += String.Format("<FL val=\"worker identifier\">{0}</FL>", m_sWorker);
            //strLine += String.Format("<FL val=\"sim number\">{0}</FL>", txtSIM.Text);
            //strLine += String.Format("<FL val=\"comments\">{0}</FL>", txtComment.Text);
            //strLine += "</row></Products>";
                        
            //AddText(richTextBox1, strLine);
            //string s = SendProductInfoToZoho(strAddress, strLine);
            //AddText(richTextBox1, s);
            //if (s.Contains("Record(s) added successfully") == true)
            //    MessageBox.Show("Product Information was sent successfully to CRM");
            //else
            //    MessageBox.Show(s, "Failed to send Product Information");            
        }                

 /*        public static string SendProductInfoToZoho(string url, string postcontent)
        {
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(postcontent);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            return responseFromServer;
        }*/

         public void SendLoggerInfo()
         {
             /*
             * "factory":"reuven"
             * "worker_identifier":"shluki"
             * "software_version":"r234"
             * "hardware_version":"shalom"
             * "sensor_type":23
             * "measuring_value":3.0
             * "battery_value":3.0
             * "rssi_value":3.0
             * "hardware_type":"Sensor"
             * */
             string strAddress = String.Format(@"http://plantbeat.phytech.com/activeadmin/sensors_allocations/{0}.json?user_id=1091&api_token=FrAnazu5rt67", m_nAllocID);
             String strLine;// = "authtoken=4b8873e7a46d5cf77a027bda4feb3fe4&scope=crmapi&wfTrigger=true&xmlData=<Products><row no=\"1\">";
             strLine = String.Format("&factory={0}&worker_identifier={1}&software_version={2}&hardware_version={3}", m_sFactory, m_sWorker, textVersion.Text, HardwareCmb.Text);
             strLine += String.Format("&battery_value={0}&hardware_type=RemoteLogger", textBat.Text);

             AddText(richTextBox1, "Sending Logger parameter to server...\r\n");
             try
             {
                 WebRequest request = WebRequest.Create(strAddress);
                 request.Method = "PATCH";
                 byte[] byteArray = Encoding.UTF8.GetBytes(strLine);
                 request.ContentType = "application/x-www-form-urlencoded";
                 request.ContentLength = byteArray.Length;
                 Stream dataStream = request.GetRequestStream();
                 dataStream.Write(byteArray, 0, byteArray.Length);
                 dataStream.Close();
                 WebResponse response = request.GetResponse();
                 dataStream = response.GetResponseStream();
                 StreamReader reader = new StreamReader(dataStream);
                 string responseFromServer = reader.ReadToEnd();
                 reader.Close();
                 dataStream.Close();
                 response.Close();
             
                 //return responseFromServer;
                 if (responseFromServer.Contains("true") == true)
                     AddText(richTextBox1, "Logger Information was sent successfully\r\n");
                 else
                     AddText(richTextBox1, "Failed to send Logger Information\r\n");
             }
             catch (Exception ex)
             {
                 MessageBox.Show(ex.Message);
             }
         }
         /*private void PrintBtn_Click(object sender, EventArgs e)
         {
            string barcodeString;

            Bitmap barcode = new Bitmap(1, 1);
            Font threeOfNine = new Font("Free 3 of 9", 50,
                System.Drawing.FontStyle.Regular,
                System.Drawing.GraphicsUnit.Point);
            barcodeString = '*' +textBoxLgrID.Text +'*';
            Graphics graphics = Graphics.FromImage(barcode);
            SizeF dataSize = graphics.MeasureString(barcodeString, threeOfNine);
            barcode = new Bitmap(barcode, dataSize.ToSize());
            graphics = Graphics.FromImage(barcode);
            graphics.Clear(Color.White);
            graphics.DrawString(barcodeString, threeOfNine, new SolidBrush(Color.Black), 0, 0);
            graphics.Flush();
            threeOfNine.Dispose();
            graphics.Dispose();
            new Print("Logger " + textBoxLgrID.Text + "  (" + MdmTypeCmb.Text.Substring(0, 3) + ")\n", barcode, Monitor.Properties.Resources.Logo);// qrCode.GetGraphic(1, Color.Black, Color.White, null, 0, 0),
         }
    */

         private void PrintBtn_Click_1(object sender, EventArgs e)
         {
             //string barcodeString;
             //font: "IDAutomationHC39M"  "Code 128 Regular" 

             //Bitmap barcode = new Bitmap(1, 1);
             //Font threeOfNine = new Font("Free 3 of 9", 28,
             //    System.Drawing.FontStyle.Regular,
             //    System.Drawing.GraphicsUnit.Point);
             //barcodeString = '*' + textBoxLgrID.Text + '*';
             //Graphics graphics = Graphics.FromImage(barcode);
             //SizeF dataSize = graphics.MeasureString(barcodeString, threeOfNine);
             //barcode = new Bitmap(barcode, dataSize.ToSize());
             //graphics = Graphics.FromImage(barcode);
             //graphics.Clear(Color.White);
             //graphics.DrawString(barcodeString, threeOfNine, new SolidBrush(Color.Black), 0, 0);
             //graphics.Flush();
             //threeOfNine.Dispose();
             //graphics.Dispose();
             /*
             QRCodeGenerator.ECCLevel eccLevel = (QRCodeGenerator.ECCLevel)3;// (level == "L" ? 0 : level == "M" ? 1 : level == "Q" ? 2 : 3);
             using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
             {
                 using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(textBoxLgrID.Text, eccLevel))
                 {
                     using (QRCode qrCode = new QRCode(qrCodeData))
                     {

                        // pictureBoxQRCode.BackgroundImage = qrCode.GetGraphic(20, Color.Black, Color.White,
                         //    GetIconBitmap(), (int)iconSize.Value);
              //new Print(textBoxLgrID.Text, barcode, qrCode.GetGraphic(2, Color.Black, Color.White, null, 0, 0), Monitor.Properties.Resources.BnWLogo);
                    }
                 }
             }*/
             //new Print("Logger " + textBoxLgrID.Text + "  (" + MdmTypeCmb.Text.Substring(0, 3) + ")\n", barcode,/* qrCode.GetGraphic(1, Color.Black, Color.White, null, 0, 0),*/ Monitor.Properties.Resources.Logo);
             new Print(textBoxLgrID.Text,/* barcode, qrCode.GetGraphic(2, Color.Black, Color.White, null, 0, 0), */XP_Monitor.Properties.Resources.BnWLogo);

         }

         private void IDGnrtrBtn_Click(object sender, EventArgs e)
         {
             
             //            string uriString = "http://46.101.79.233:3001/activeadmin/sensors_allocations.json?user_id=1580&api_token=nz-nLBTpvQL4N-3GTZBz";
             string uriString = @"http://plantbeat.phytech.com/activeadmin/sensors_allocations.json?user_id=1091&api_token=FrAnazu5rt67";
             try
             {
                 WebClient client = new WebClient();
                 // Optionally specify an encoding for uploading and downloading strings.
                 client.Encoding = System.Text.Encoding.UTF8;
                 client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                 //uriString = urifirstPart + textID.Text.ToString() + urilastPart;
                 // Create a new NameValueCollection instance to hold some  parameters to be posted to the URL.
                 NameValueCollection myNameValueCollection = new NameValueCollection();

                 // Add necessary parameter/value pairs to the name/value container.
                 //                myNameValueCollection.Add("utf8", "%E2%9C%93");
                 myNameValueCollection.Add("sensors_allocation[allocations_number]", "1");   //m_numSen.ToString());
                 myNameValueCollection.Add("sensors_allocation[wireless]", "1");
                 myNameValueCollection.Add("commit", "Create Sensors allocation");

                 // 'The Upload(String,NameValueCollection)' implicitly method sets HTTP POST as the request method.             
                 byte[] responseArray = client.UploadValues(uriString, myNameValueCollection);
                 string reply = Encoding.UTF8.GetString(responseArray, 0, responseArray.Length);
                 // Upload the data.
                 //string[] separators = { ",", "[", "]" }; //, "?", ";", ":", " " };
                 //string[] IDs = reply.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                ///////////////////////////////////////////////////
                string[] separators = { ",", "[", "]", ":", "{", "}" }; //, "?", ";", ":", " " };
                string[] IDs = reply.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                int n1, n2;
                if (int.TryParse(IDs[3], out n1) && int.TryParse(IDs[1], out n2))
                {
                    textID.Text = IDs[3];
                    m_nAllocID = n2;
                //textID.Text = IDs[0];
                }
                else
                    return ;
                ///////////////////////////////////////////////////////
                 // send the ID
                 BuildPrtocol(0, 1);                 

                 // Disply the server's response.
                 //MessageBox.Show(responseArray.ToString());
                 //.WriteLine(reply);
             }
             catch (WebException we)
             {
                 MessageBox.Show(we.Message);
             }
            
         }

         private string GeBurnFileName(string sFileType)
         {
             OpenFileDialog ofd = new OpenFileDialog();
             string sFilter;
             sFilter = sFileType + " files (*." + sFileType + ")|*." + sFileType;
             ofd.Filter = sFilter;// "hex files (*.hex)|*.hex";
             DialogResult result = ofd.ShowDialog();
             if (result == System.Windows.Forms.DialogResult.OK)
                 return ofd.FileName;
             return "";
         }
         private void lgrBrsBtn_Click(object sender, EventArgs e)
         {
             loggerFile2Burn.Text = GeBurnFileName("hex");
         }

         private void RecBrsBtn_Click(object sender, EventArgs e)
         {
             RcvrFile2Burn.Text = GeBurnFileName("hex");
         }

         private void lgrEepBrsBtn_Click(object sender, EventArgs e)
         {
             LoggerEep2Burn.Text = GeBurnFileName("eep");
         }

         private bool IsDigit(char c)
         {
             if ((c >= '0') && (c <= '9'))
                return true;
             return false;
         }

         private void BurnAll()
         {
            
            
             
         }

         private void burnBtn_Click(object sender, EventArgs e)
         {
             burnRcvBtn.BackColor = Color.Orange;
             // programs path
             string ezr32commanderpath = "C:\\phytechburn\\SimplicityCommander\\Simplicity Commander\\";
             // writing flash radio transreceiver...
             Process p = new Process();

             p.StartInfo.FileName = ezr32commanderpath + "Commander.exe";//filePath of the application
             p.StartInfo.Arguments = "adapter probe";
             p.StartInfo.UseShellExecute = false;
             p.StartInfo.RedirectStandardOutput = true;
             p.StartInfo.RedirectStandardError = true;
             p.StartInfo.CreateNoWindow = true;
             p.Start();
             AddText(richTextBox1, p.StandardOutput.ReadToEnd());
             p.WaitForExit();
             string s = "";

             if (richTextBox1.Text.Contains("J-Link Serial"))//J-Link Serial   : 440064012
             {
                 int i = richTextBox1.Text.IndexOf("J-Link Serial");
                 if (i >= 0)
                 {
                     i += 13;
                     while (!IsDigit(richTextBox1.Text[i++])) ;
                     i--;
                     do
                     {
                         s += richTextBox1.Text[i];
                         i++;
                     }
                     while (IsDigit(richTextBox1.Text[i]));
                 }
             }
             else
             {
                 MessageBox.Show("Cant find J-Link Serial. \nPlease make sure it connected to your PC");
                 return;
             }
             textEZRno.Text = s;
             p.StartInfo.Arguments = "device masserase -d EZR32HG320F64R68";
             p.Start();
             AddText(richTextBox1, p.StandardOutput.ReadToEnd());
             p.WaitForExit();
             p.StartInfo.Arguments = "flash " + RcvrFile2Burn.Text + " -d EZR32HG320F64R68 -s " + s;// textEZRno.Text;
             p.Start();
             AddText(richTextBox1, p.StandardOutput.ReadToEnd());
             p.WaitForExit();
             if (!richTextBox1.Text.Contains("ERROR"))//if got this message means succeedded
             {
                 burnRcvBtn.BackColor = Color.Green;
                 burnRcvBtn.Text = "PASS";
                 burnRcvBtn.ForeColor = Color.White;
             }
             else
             {
                 burnRcvBtn.BackColor = Color.Red;
                 burnRcvBtn.Text = "FAILED";
                 burnRcvBtn.ForeColor = Color.White;
             }
             //SetTimer();
         }

        //use avrdude.exe
         private void BurnLogger(string args)
         {
             string avrdudepath = "C:\\phytechburn\\avrdude\\";
             string line;
             try
             {
                 // writing flash logger...
                 Process p = new Process();

                 //writing atmega flash & eeprom...
                 p.StartInfo.FileName = avrdudepath + "avrdude.exe";//filePath of the application                 
                 //p.StartInfo.Arguments =  "-p m644p -c avrispmkII -P usb -U hfuse:w:0xDF:m -U lfuse:w:0xDF:m";
                 p.StartInfo.Arguments = args;// "-p m644p -c avrispmkII -P usb -U flash:w:" + loggerFile2Burn.Text + ":i -U eeprom:w:" + LoggerEep2Burn.Text + ":i";

                 p.StartInfo.UseShellExecute = false;
                 p.StartInfo.RedirectStandardOutput = true;
                 p.StartInfo.RedirectStandardError = true;
                 p.StartInfo.CreateNoWindow = true;
                 p.EnableRaisingEvents = true;

                 p.Start();
                 
                 //p.BeginOutputReadLine();

                 while ((!p.StandardOutput.EndOfStream) || (!p.StandardError.EndOfStream))
                 {
                     while ((line = p.StandardOutput.ReadLine()) != null)
                     //line = p.StandardOutput.ReadLine();
                     //if (line != null)
                     {
                         AddText(richTextBox1, line);
                         AddText(richTextBox1, "\r\n");
                     }
                     line = p.StandardError.ReadLine();
                     if (line != null)
                     {
                         AddText(richTextBox1, line);
                         AddText(richTextBox1, "\r\n");
                     }
                 }
                  
                 p.WaitForExit();
             }
             catch (Exception e)
             {
                 AddText(richTextBox1, e.Message);
             }
         }
        //use atprogram.exe
         private int BurnLogger2()
         {
             //string avrdudepath = "C:\\phytechburn\\avrdude\\";
             string line;
             int exitCode = 1;

             // The name of the key must include a valid root.
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = "Software\\Atmel\\AtmelStudio\\7.0_Config";
            const string keyName = userRoot + "\\" + subkey;
        
            string path = (string)Registry.GetValue(keyName, "InstallDir", "Empty");
           
             try
             {
                 // writing flash logger...
                 Process p = new Process();

                 //writing atmega flash & eeprom...
                 p.StartInfo.FileName = path + "\\atbackend\\atprogram.exe";//filePath of the application                 
                 //p.StartInfo.Arguments =  "-p m644p -c avrispmkII -P usb -U hfuse:w:0xDF:m -U lfuse:w:0xDF:m";
                 p.StartInfo.Arguments = string.Format("-f -t avrispmk2 -i isp -d atmega644pa -cl 500khz write -fs --values EFDFFF --verify program -fl -f {0} --format hex --verify -c program -ee -f {1} --format hex --verify", loggerFile2Burn.Text, LoggerEep2Burn.Text);
                  
                 p.StartInfo.UseShellExecute = false;
                 p.StartInfo.RedirectStandardOutput = true;
                 p.StartInfo.RedirectStandardError = true;
                 p.StartInfo.CreateNoWindow = true;
                 p.EnableRaisingEvents = true;

                 p.Start();

                 //p.BeginOutputReadLine();

                 while ((!p.StandardOutput.EndOfStream) || (!p.StandardError.EndOfStream))
                 {
                     while ((line = p.StandardOutput.ReadLine()) != null)
                     //line = p.StandardOutput.ReadLine();
                     //if (line != null)
                     {
                         AddText(richTextBox1, line);
                         AddText(richTextBox1, "\r\n");
                     }
                     line = p.StandardError.ReadLine();
                     if (line != null)
                     {
                         AddText(richTextBox1, line);
                         AddText(richTextBox1, "\r\n");
                     }
                 }

                 p.WaitForExit();
                 exitCode = p.ExitCode;
             }
             catch (Exception e)
             {
                 AddText(richTextBox1, e.Message);
             }
             return exitCode;
         }

        //use batch file
         private int BurnLogger1()
         {
             string avrdudepath = "C:\\phytechburn\\avrdude\\";
             int exitCode = 0;
             //string line;
             try
             {
                 // writing flash logger...
                 Process p = new Process();

                 //writing atmega flash & eeprom...
                 //p.StartInfo.FileName = avrdudepath + "avrdude.exe";//filePath of the application
                 p.StartInfo.FileName = avrdudepath + "burn_logger.bat";

                 p.StartInfo.UseShellExecute = false;
                 //p.StartInfo.RedirectStandardOutput = true;
                 //p.StartInfo.RedirectStandardError = true;
                 //p.StartInfo.CreateNoWindow = true;
                 p.EnableRaisingEvents = true;

                 p.Start();

                 //p.BeginOutputReadLine();
                 /*
                 while ((!p.StandardOutput.EndOfStream) || (!p.StandardError.EndOfStream))
                 {
                     while ((line = p.StandardOutput.ReadLine()) != null)
                     //line = p.StandardOutput.ReadLine();
                     //if (line != null)
                     {
                         AddText(richTextBox1, line);
                         AddText(richTextBox1, "\r\n");
                     }
                     line = p.StandardError.ReadLine();
                     if (line != null)
                     {
                         AddText(richTextBox1, line);
                         AddText(richTextBox1, "\r\n");
                     }
                 }
                 */
                 p.WaitForExit();
                 exitCode = p.ExitCode;
             }
             catch (Exception e)
             {
                 AddText(richTextBox1, e.Message);
             }
             return  exitCode;
         }
         
        private void burnLgrBtn_Click(object sender, EventArgs e)
         {
             int res;
             richTextBox1.Clear();
             burnLgrBtn.Enabled = false;
             burnLgrBtn.BackColor = Color.Orange;
             // programs path
             AddText(richTextBox1, "Burn files\n");
             //BurnLogger("-p m644p -c avrispmkII -P usb -U hfuse:w:0xDF:m -U lfuse:w:0xEF:m");
             res = BurnLogger2();
             //AddText(richTextBox1, "Burn flash & eeprom\nPlease wait until orange led turn green\n");
             //BurnLogger("-p m644p -c avrispmkII -P usb -U flash:w:" + loggerFile2Burn.Text + ":i -U eeprom:w:" + LoggerEep2Burn.Text + ":i");
             //BurnLogger1();
             if (res == 0)//(richTextBox1.Text.Contains("flash verified")) && (richTextBox1.Text.Contains("eeprom verified")))//if got this message means succeedded
             {
                 burnLgrBtn.BackColor = Color.Green;
                 burnLgrBtn.Text = "PASS";
                 burnLgrBtn.ForeColor = Color.White;
             }
             else
             {
                 burnLgrBtn.BackColor = Color.Red;
                 burnLgrBtn.Text = "FAILED";
                 burnLgrBtn.ForeColor = Color.White;
             }
             burnLgrBtn.Enabled = true;
             //flash verified
             //eeprom verified
         }

         private void ClearBtns()
         {  
             burnLgrBtn.BackColor = firstColor; //Color.Red;
             SetText(burnLgrBtn, "BURN");
            //burnLgrBtn.Text = "BURN";
            burnLgrBtn.ForeColor = Color.Black;

            burnRcvBtn.BackColor = firstColor;// Color.Red;
            SetText(burnRcvBtn, "BURN");
            //burnRcvBtn.Text = "BURN";
            burnRcvBtn.ForeColor = Color.Black;

         }

         private void brnClrBtn_Click(object sender, EventArgs e)
         {
             ClearBtns();
         }         
    }
    public class HardwareObj
    {
        public int id { get; set; }
        public string version { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
    }
    public class SensorTypeObject
    {
        public int id { get; set; }
        public string unit { get; set; }
        public string title { get; set; }
        public string codename { get; set; }
        public bool @virtual { get; set; }
        public bool deprecated { get; set; }
    }
}

