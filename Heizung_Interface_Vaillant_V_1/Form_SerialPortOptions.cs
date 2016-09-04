using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;


namespace Heizung_Interface_Vaillant_V_1
{
    public partial class Form_SerialPortOptions : Form
    {
        SerialPort oPort1;

        public Form_SerialPortOptions(SerialPort port1)
        {
            InitializeComponent();

            oPort1 = port1;

            foreach (string s in SerialPort.GetPortNames())
            {
                comboBox_Port1.Items.Add(s);
            }
            comboBox_Port1.SelectedItem = oPort1.PortName;


            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                comboBox_Parity.Items.Add(s);
            }
            comboBox_Parity.SelectedItem = oPort1.Parity.ToString();

            comboBox_DataBits.Items.Add("8");
            comboBox_DataBits.Items.Add("7");
            comboBox_DataBits.SelectedItem = oPort1.DataBits.ToString();

            comboBox_BaudRate.Items.Add("300");
            comboBox_BaudRate.Items.Add("1200");
            comboBox_BaudRate.Items.Add("2400");
            comboBox_BaudRate.Items.Add("4800");
            comboBox_BaudRate.Items.Add("9600");
            comboBox_BaudRate.Items.Add("19200");
            comboBox_BaudRate.Items.Add("28200");
            comboBox_BaudRate.Items.Add("57600");
            comboBox_BaudRate.Items.Add("115200");
            comboBox_BaudRate.Items.Add("230400");
            comboBox_BaudRate.SelectedItem = oPort1.BaudRate.ToString();

            comboBox_stopbits.Items.Add(StopBits.None);
            comboBox_stopbits.Items.Add(StopBits.One);
            comboBox_stopbits.Items.Add(StopBits.OnePointFive);
            comboBox_stopbits.Items.Add(StopBits.Two);
            comboBox_stopbits.SelectedItem = oPort1.StopBits;
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            bool bWasOpen1 = false;
            if (oPort1.IsOpen)
            {
                bWasOpen1 = true;
                oPort1.Close();
            }

            try
            {
                oPort1.PortName = comboBox_Port1.SelectedItem.ToString();
                oPort1.Parity = (Parity)Enum.Parse(typeof(Parity), comboBox_Parity.SelectedItem.ToString(), true);
                oPort1.DataBits = System.Convert.ToInt32(comboBox_DataBits.SelectedItem);
                oPort1.BaudRate = System.Convert.ToInt32(comboBox_BaudRate.SelectedItem);
                oPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), comboBox_stopbits.SelectedItem.ToString(), true);

                if (bWasOpen1)
                {
                    oPort1.Open();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show( exc.Message.ToString());
            }
            
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
