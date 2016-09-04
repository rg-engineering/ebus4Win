using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Heizung_Interface_Vaillant_V_1
{
//http://ebus.webhop.org/twiki/bin/view.pl/EBus/EBusBefehle


    /* Datentypen
     De  Ko  Typ  Byte Beschreibung
     X   X  str  x    string
     X   X  bcd  1    bcd
     X   X  d1b  1    data1b
     X   X  d1c  1    data1c
     X   X  d2b  2    data2b
     X   X  d2c  2    data2c
     X      bda  3    date (bcd Format)
     X   X  hda  3    date (hex Format)
     X      bti  3    time (bcd Format)
     X   X  hti  3    time (hex Format)
     X      bdy  1    day  (bcd Format)
     X   X  hdy  1    day  (hex Format)
     X      hex  x    2-nibble hex Format z.B: 2C
     X      uch  1    unsigned char
     X      sch  1    signed char
     X      uin  2    unsigned int
     X      sin  2    signed int
     X      ulg  4    unsigned long
     X      slg  4    signed long
     X      flt  4    float
    */
    public partial class Form_Main : Form
    {
        public delegate void InvokeDelegate();

        private string sRawDataReceived;
        private int nState;
        private string sSource;
        private string sDestination;
        private string sCommand;
        private string sSubCommand;
        private string sDataLength;
        private string sData;
        private string sCRC;
        private string sCalcCRC;
        private int nDataLength;
        private int nDataLengthCount;
        private string sCommandName;

        private string sStatus;
        private string sAnswerDataLength;
        private string sAnswerData;
        private string sAnswerCRC;
        private string sAnswerCalcCRC;

        private double fTemperatureOutside;
        private double fVorlauFtempVF1; 
        private double fVorlauFtempVF2;
        private double fQuellTemp;

        private int nVorlauFtempVF1Status;
        private int nVorlauFtempVF2Status;
        private int nQuellTempStatus;
        private int nMomentanLeistung;
        private int nStatus;
        private uint nCurrentError;
        private uint nCurrentWarning;
        private string sTempError;


        private int nSecond;
        private int nMinute;
        private int nHour;
        private int nDay;
        private int nMonth;
        private int nDayOfWeek;
        private int nYear;
        private DateTime oLastUpdate_0700;
        private DateTime oLastUpdate_B509;


        private bool bLoadFile;

        public Form_Main()
        {
            InitializeComponent();

            try
            {
                serialPort1.Open();
                if (serialPort1.IsOpen)
                {
                    btnOpenPort.Text = "Close Port";
                }
                else
                {
                    btnOpenPort.Text = "Open Port";
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message.ToString());
            }
            toolStripStatusLabel1.Text = "serial port opened";
            nState = 0;

            nDayOfWeek = -1;
            nYear = 1900;
            nMonth = 1;
            nDay = 1;
            nHour = 0;
            nMinute = 0;
            nSecond = 0;
            fTemperatureOutside = 0;
            fVorlauFtempVF1 = 0;
            fVorlauFtempVF2 = 0;
            fQuellTemp = 0;
            nCurrentError=0;
            nCurrentWarning=0;
            sTempError = "";

            nVorlauFtempVF1Status = -1;
            nVorlauFtempVF2Status = -1;
            nQuellTempStatus = -1;
            nMomentanLeistung = -1;
            nStatus = -1;
            bLoadFile = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void serialPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form_SerialPortOptions myForm = new Form_SerialPortOptions(serialPort1);
            myForm.Show();
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialPort sp = (System.IO.Ports.SerialPort)sender;

            int nCount = sp.BytesToRead;
            string indata = "";
            for (int i = 0; i < nCount; i++)
            {
                int inbyte = sp.ReadByte();
                indata += inbyte.ToString("X2");
                indata += " ";
            }

            EvaluateData(indata);

        }

        private void EvaluateData(string indata)
        {
           // EvaluateDataRawData(indata);
            EvaluateDataTelegram(indata);
        }

        private void EvaluateDataRawData(string indata)
        {
            sRawDataReceived = indata;
            tbReceivedDataRaw.BeginInvoke(new InvokeDelegate(InvokeMethodRawData));
        }

        private void EvaluateDataTelegram(string indata)
        {
            for (int i = 0; i < indata.Length; i+=3)
            {
                string sChar = indata.Substring(i, 2);


                //wirklich so?
//                if (sChar == "AA") //Sync received
//                {
//                    nState = 0;
//                }

                switch (nState)
                {
                    case 0:
                        if (sChar != "AA") //Sync
                        {
                            sDestination = "";
                            sCommand = "";
                            sSubCommand = "";
                            sCommandName = "";
                            sDataLength = "";
                            sData = "";
                            sCRC = "";
                            sCalcCRC = "";

                            sAnswerDataLength = "";
                            sAnswerData = "";
                            sAnswerCRC = "";
                            sAnswerCalcCRC = "";

                            nState++;
                            sSource = "0x";
                            sSource += sChar;
                            sStatus = "got source";
                        }
                        break;
                    case 1: // Destination
                        nState++;
                        sDestination = "0x";
                        sDestination += sChar;
                        sStatus = "got destination";
                        break;
                    case 2: // Command
                        nState++;
                        sCommand = "0x";
                        sCommand += sChar;
                        sStatus = "got command";
                        break;
                    case 3: // SubCommand
                        nState++;
                        sSubCommand = "0x";
                        sSubCommand += sChar;
                        GetCommandName();
                        sStatus = "got sub command";
                        break;
                    case 4: // Datalength
                        sDataLength = "0x";
                        sDataLength += sChar;
                        sDataLength += " (";
                        sDataLength += HexToDec(sChar).ToString();
                        sDataLength += ")";
                        nDataLength = HexToDec(sChar);
                        nDataLengthCount = 0;
                        sData = "";
                        sStatus = "got data length";
                        if (nDataLength > 0)
                        {
                            nState++;
                        }
                        else
                        {
                            nState += 2;
                        }
                        break;
                    case 5: // Data
                        sData += sChar;
                        sData += " ";
                        nDataLengthCount++;
                        if (nDataLengthCount == nDataLength)
                        {
                            nState++;
                            sStatus = "got data";
                        }
                        break;
                    case 6: // CRC
                        nState++;
                        sCRC = sChar;
                        sCRC += " (";
                        sCRC += HexToDec(sChar).ToString();
                        sCRC += ")";
                        sCalcCRC = CalcCRC(sSource+sDestination+sCommand+sSubCommand+sDataLength+sData);
                        break;
                    case 7: // Ack

                        if (IsBroadCast())
                        {
                            nState = 0;
                            toolStripStatusLabel1.Text = "broadcast";
                            sStatus = "broadcast";
                            GetDataFromTelegram();
                            if (bLoadFile)
                            {
                                InvokeMethodTelegrams();
                            }
                            else
                            {
                                gridTelegrams.BeginInvoke(new InvokeDelegate(InvokeMethodTelegrams));
                            }
                        }
                        else if (sChar == "00")
                        {
                            nState++;
                        }
                        else if (sChar == "AA")
                        {
                            nState = 0;
                            toolStripStatusLabel1.Text = "seems broadcast";
                            sStatus = "seems broadcast";
                            if (bLoadFile)
                            {
                                InvokeMethodTelegrams();
                            }
                            else
                            {
                                gridTelegrams.BeginInvoke(new InvokeDelegate(InvokeMethodTelegrams));
                            }
                        }
                        else
                        {
                            nState = 0;
                            toolStripStatusLabel1.Text = "ack missing... got " + sChar;
                            sStatus = "ack missing... got " + sChar;
                            if (bLoadFile)
                            {
                                InvokeMethodTelegrams();
                            }
                            else
                            {
                                gridTelegrams.BeginInvoke(new InvokeDelegate(InvokeMethodTelegrams));
                            }
                        }
                        break;
                    case 8: //answer datalength

                        if (sChar == "AA")
                        {
                            nState = 0;
                            toolStripStatusLabel1.Text = "seems broadcast (2)";
                            sStatus = "seems broadcast (2)";
                            if (bLoadFile)
                            {
                                InvokeMethodTelegrams();
                            }
                            else
                            {
                                gridTelegrams.BeginInvoke(new InvokeDelegate(InvokeMethodTelegrams));
                            }
                        }
                        else
                        {

                            sAnswerDataLength = "0x";
                            sAnswerDataLength += sChar;
                            sAnswerDataLength += " (";
                            sAnswerDataLength += HexToDec(sChar).ToString();
                            sAnswerDataLength += ")";
                            nDataLength = HexToDec(sChar);
                            nDataLengthCount = 0;
                            sAnswerData = "";
                            if (nDataLength > 0)
                            {
                                nState++;
                            }
                            else
                            {
                                nState += 2;
                            }
                        }
                        break;
                    case 9:
                        sAnswerData += sChar;
                        sAnswerData += " ";
                        nDataLengthCount++;
                        if (nDataLengthCount == nDataLength)
                        {
                            nState++;
                        }
                        break;
                    case 10: //CRC
                        nState++;
                        sAnswerCRC = sChar;
                        sAnswerCRC += " (";
                        sAnswerCRC += HexToDec(sChar).ToString();
                        sAnswerCRC += ")";
                        sAnswerCalcCRC = CalcCRC(sAnswerDataLength + sAnswerData);
                        break;
                    case 11:
                        if (sChar == "00")
                        {
                            sStatus = "complete";
                            GetDataFromTelegram();
                        }
                        else
                        {
                            sStatus = "answer ack missing";
                            
                        }
                        nState = 0;
                        if (bLoadFile)
                        {
                            InvokeMethodTelegrams();
                        }
                        else
                        {
                            gridTelegrams.BeginInvoke(new InvokeDelegate(InvokeMethodTelegrams));
                        }
                        break;

                    default:
                        nState = 0;
                        toolStripStatusLabel1.Text = "telegram not complete...";
                        break;
                }
            }
        }


        public void InvokeMethodRawData()
        {
            tbReceivedDataRaw.Text += sRawDataReceived;
            sRawDataReceived = "";
        }

        public void InvokeMethodTelegrams()
        {
            DataGridViewRow row = new DataGridViewRow();
            row.CreateCells(gridTelegrams);
            row.Cells[0].Value = gridTelegrams.Rows.Count + 1;
            row.Cells[1].Value = sSource;
            row.Cells[2].Value = sDestination;
            row.Cells[3].Value = sCommand;
            row.Cells[4].Value = sSubCommand;
            row.Cells[5].Value = sCommandName;
            row.Cells[6].Value = sDataLength;
            row.Cells[7].Value = sData;
            row.Cells[8].Value = sCRC;
            row.Cells[9].Value = sCalcCRC;

            row.Cells[10].Value = sAnswerDataLength;
            row.Cells[11].Value = sAnswerData;
            row.Cells[12].Value = sAnswerCRC;
            row.Cells[13].Value = sAnswerCalcCRC;

            row.Cells[14].Value = sStatus;

            int nIsKnown = IsKnownTelegram();
            if (nIsKnown == 1)
            {
                row.DefaultCellStyle.BackColor = Color.Yellow;
            }
            else if (nIsKnown == 2)
            {
                row.DefaultCellStyle.BackColor = Color.Green;
            }
            else if (nIsKnown == 3)
            {
                row.DefaultCellStyle.BackColor = Color.Khaki;
            }


            DateTime oDate = new DateTime(nYear, nMonth, nDay, nHour, nMinute, nSecond);
            switch (nDayOfWeek)
            {
                case 0:
                    tbDateTime.Text = "Monday ";
                    break;
                case 1:
                    tbDateTime.Text = "Tuesday ";
                    break;
                case 2:
                    tbDateTime.Text = "Wednesday ";
                    break;
                case 3:
                    tbDateTime.Text = "Thursday ";
                    break;
                case 4:
                    tbDateTime.Text = "Friday ";
                    break;
                case 5:
                    tbDateTime.Text = "Saturday ";
                    break;
                case 6:
                    tbDateTime.Text = "Sunday ";
                    break;
                default:
                    tbDateTime.Text = " ";
                    break;
            }
            tbDateTime.Text += oDate.ToString();

            tbTempOutside.Text = fTemperatureOutside.ToString("0.0");
            tbLastUpdate.Text = oLastUpdate_0700.ToString();

            tbVorlaufTempVF1.Text = fVorlauFtempVF1.ToString("0.0");
            tbVorlaufTempVF2.Text = fVorlauFtempVF2.ToString("0.0");
            tbQuellTemp.Text = fQuellTemp.ToString("0.0");

            tbVorlaufTempVF1Status.Text = nVorlauFtempVF1Status.ToString();
            tbVorlaufTempVF2Status.Text = nVorlauFtempVF2Status.ToString();
            tbQuellTempStatus.Text = nQuellTempStatus.ToString();

            tbMomantanLeistung.Text = nMomentanLeistung.ToString();

            string sTempStatus="";
            switch (nStatus)
            {
                case -1:
                    break;
                case 0:
                    sTempStatus = "Bereitschaft";
                    break;
                case 42:
                    sTempStatus = "Warmwasser";
                    break;
                case 50:
                    sTempStatus = "Heizen";
                    break;
                case 64:
                    sTempStatus = "Heizen";
                    break;
                default:
                    sTempStatus = nStatus.ToString();
                    break;
            }

            tbStatus.Text=sTempStatus;

            chart1.Series["SerieTemperatureOutside"].Points.Add(fTemperatureOutside);

            chart1.Series["SerieVorlauFtempVF2"].Points.Add(fVorlauFtempVF2);
            chart1.Series["SerieVorlauFtempVF2Status"].Points.Add(nVorlauFtempVF2Status);

            chart1.Series["SerieQuellTemp"].Points.Add(fQuellTemp);
            chart1.Series["SerieMomentanLeistung"].Points.Add(nMomentanLeistung);
            chart1.Series["SerieStatus"].Points.Add(nStatus);


            //add some known data
            string sInfo="";

            //sInfo = sTempStatus + " " + fVorlauFtempVF2.ToString("0.0") + " " + fQuellTemp.ToString("0.0");

            sInfo=sTempError;
            row.Cells[15].Value = sInfo ;
            sTempError = "";

            gridTelegrams.Rows.Add(row);

        }

        private bool IsBroadCast()
        {
            bool bRet = false;

            if (sDestination == "0xFE")
            {
                bRet = true;
            }
            else if (sCommand == "0x07" && sSubCommand == "0x00")
            {
                bRet = true;
            }
            return bRet;
        }

        private int IsKnownTelegram()
        {
            int nRet = -1;

            string sCmd = sCommand;
            string sSubCmd = sSubCommand;
            string sLength = sDataLength;
            string sDta = sData;

            if (sCmd == "0xFE" && sSubCmd == "0x01")
            {
            }
            else if (sCmd == "0xB5" && sSubCmd == "0x03")
            {
                nRet = 3;
            }
            else if (sCmd == "0xB5" && sSubCmd == "0x04")
            {
            }
            else if (sCmd == "0xB5" && sSubCmd == "0x05" )
            {
                if (sLength == "0x04 (4)" && sDta.StartsWith("27")) //VorlaufTemp
               {
                   nRet = 1;
               }
            }
            else if (sCmd == "0xB5" && sSubCmd == "0x16" && sLength == "0x08 (8)") //DateTime
            {
                nRet = 1;
            }
            else if (sCmd == "0xB5" && sSubCmd == "0x16" && sLength == "0x03 (3)") //OutTemp
            {
                nRet = 1;
            }
            else if (sCmd == "0xB5" && sSubCmd == "0x09") 
            {
                if (sLength == "0x04 (4)") //
                {
                }
                else if (sLength == "0x03 (3)" && sDta == "29 01 00 ") //Vorlauftemperatur VF1
                {
                    nRet = 2;
                }
                else if (sLength == "0x03 (3)" && sDta == "29 03 00 ") //Vorlauftemperatur VF2
                {
                    nRet = 2;
                }
                else if (sLength == "0x03 (3)" && sDta == "29 07 00 ") //Rücklauftemperatur RF1
                {
                    nRet = 1;
                }
                else if (sLength == "0x03 (3)" && sDta == "29 09 00 ") //Rücklauftemperatur RF2 ???
                {
                    nRet = 1;
                }
                else if (sLength == "0x03 (3)" && sDta == "29 0F 00 ") //Quellentemperatur
                {
                    nRet = 2;
                }
                else if (sLength == "0x03 (3)" && sDta == "29 B8 01 ") //Hocheffizenzpumpenstatus
                {
                    nRet = 1;
                }
                else if (sLength == "0x03 (3)" && sDta == "29 B9 01 ") //Status Heizkreispumpe
                {
                    nRet = 1;
                }
                else if (sLength == "0x03 (3)" && sDta == "29 D3 00 ") //???
                {
                    nRet = 1;
                }
                else if (sLength == "0x03 (3)" && sDta == "29 BA 00 ") //Status?
                {
                    nRet = 2;
                }
                else if (sLength == "0x03 (3)" && sDta == "29 BB 00 ") //Status?
                {
                    nRet = 2;
                }
            }
            else if (sCmd == "0x07" && sSubCmd == "0x00")
            {
                nRet = 2;
            }
            return nRet;


            //EnergyYieldDayTransfer ff15 b509 03 0d 86 00 //0D8600
            //BufferNtcTo ffedb509030d0700
            //BufferNtcFrom ffedb509030d0800
            //FlowTempSensor ff15b509030d0200
            //SolarNtcTo ffedb509030d0600
            //SolarNtcFrom ffedb509030d0500
            //YieldSum ffedb509030d5600
            //YieldDay ffedb509030d3b00
            //Ntc3 ff0ab509030d0200
            //Ntc2 ff0ab509030d0100
            //Ntc1 ff0ab509030d0000
            //BrinePress ff08b509030d1600
            //mc2FlowTempSensor ff53b509030d0300
            //mc1FlowTempSensor ff52b509030d0100
            //EnergyYieldSum ff15b509030d8700
            //

 

 


        }


        private void GetCommandName()
        {
            if (sCommand == "0xB5" && sSubCommand == "0x03")
            {
                sCommandName = "Get Error / Warning";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x04")
            {
                sCommandName = "Get Data Block";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x05")
            {
                sCommandName = "Burner Operational Data";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x06")
            {
                sCommandName = "Unknown Broadcast 2";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x09")
            {
                sCommandName = "Get Solar Data ";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x10")
            {
                sCommandName = "Operational Data from Room Controller to Burner Control Unit";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x11")
            {
                sCommandName = "Operational Data of Burner Control Unit to Room Control Unit";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x12")
            {
                sCommandName = "Unknown Command";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x16" && sDataLength=="0x08 (8)")
            {
                sCommandName = "DateTime Broadcast Service";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x16" && sDataLength == "0x03 (3)")
            {
                sCommandName = "OutTemp Broadcast Service";
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x16")
            {
                sCommandName = "Broadcast Service";
            }
            else if (sCommand == "0x07" && sSubCommand == "0x00")
            {
                sCommandName = "Date / Time";
            }
        }

        private void GetDataFromTelegram()
        {
            if (sCommand == "0xB5" && sSubCommand == "0x03") //Fehler
            {
                Extract_B503();
            }
            else if (sCommand == "0xB5" && sSubCommand == "0x05")
            {
                Extract_B505();
            }
            if (sCommand == "0xB5" && sSubCommand == "0x09") 
            {
                Extract_B509();
            }

            else if (sCommand == "0x07" && sSubCommand == "0x00")
            {
                Extract_0700();
            }
        }

        private void Extract_B505()
        {
            if (sDataLength=="0x04 (4)")
            {
                double fTemp = GetFromData1C(sData, 3); //ist noch nciht implementiert
            }
        }

        private void Extract_B503()
        {
            string sVariable;

            if (sData.Length == 6)
            {
                sVariable = sData.Substring(0, 5);

                switch (sVariable)
                {
                    case "00 01": //aktueller Fehler
                        nCurrentError = GetFromUIN(sAnswerData, 1);
                        sTempError = sAnswerData;
                        break;
                    case "00 02": //aktulle Warnung
                        break;
                    case "01 01": //Fehlerhistorie
                        break;
                    case "01 02": //Warnungshistorie
                        break;
                }
            }
            else if (sData.Length == 8)
            {
                sVariable = sData.Substring(0, 8);

                switch (sVariable)
                {
                    case "00 01 00": //???
                        break;
                }
            }
        }

        private void Extract_B509()
        {

            string sVariable = sData.Substring(0, 8);

            switch (sVariable)
            {
                case "29 01 00": //Vorlauftemperatur VF1
                    fVorlauFtempVF1 = GetFromData2C(sAnswerData, 3);
                    nVorlauFtempVF1Status=GetFromBCD(sAnswerData,5);
                    break;
                case "29 03 00": //Vorlauftemperatur VF2
                    fVorlauFtempVF2 = GetFromData2C(sAnswerData, 3);
                    nVorlauFtempVF2Status=GetFromBCD(sAnswerData,5);
                    break;
                case "29 07 00": //Rücklauftemperatur RF1
                    break;
                case "29 0F 00": //Quellentemperatur
                    fQuellTemp = GetFromData2C(sAnswerData, 3);
                    nQuellTempStatus = GetFromBCD(sAnswerData, 5);
                    break;
                case "29 B8 01": //Hocheffizenzpumpenstatus
                    break;
                case "29 B9 01": //Status Heizkreispumpe
                    break;
                case "29 BA 00":
                    nMomentanLeistung = GetFromBCD(sAnswerData, 3);
                    break;
                case "29 BB 00":
                    nStatus = GetFromBCD(sAnswerData, 3);
                    break;
                case "0d 86 00": //EnergyYieldDayTransfer
                    break;
                case "0D 07 00": //BufferNtcTo
                    break;
                case "0D 08 00": //BufferNtcFrom
                    break;
                 case "0D 02 00": //FlowTempSensor
                    break;
                 case "0D 06 00": //SolarNtcTo
                    break;
                 case "0D 05 00": //SolarNtcFrom
                    break;
                 case "0D 56 00": //YieldSum
                    break;
                 case "0D 3B 00": //YieldDay
                    break;
                 case "0D 00 00": //Ntc1
                    break;
                 case "0D 01 00": //Ntc2
                    break;
//                 case "0D 02 00": //Ntc3
//                    break;
                 case "0D 16 00": //BrinePress
                    break;
                 case "0D 03 00": //mc1FlowTempSensor
                    break;
//                 case "0D 01 00": //mc2FlowTempSensor
//                    break;
                 case "0D 87 00": //EnergyYieldSum
                    break;

            }
        }

        private void Extract_0700()
        {
            //TempOutside and DateTime broadcast
            fTemperatureOutside = GetFromData2B(sData, 1);
            nSecond = GetFromByte(sData, 3);
            nMinute = GetFromByte(sData, 4);
            nHour = GetFromByte(sData, 5);
            nDay = GetFromByte(sData, 6);
            nMonth = GetFromByte(sData, 7);
            nDayOfWeek = GetFromByte(sData, 8);
            nYear = GetFromByte(sData, 9);
            if (nYear < 2000) nYear += 2000;
            oLastUpdate_0700 = System.DateTime.Now;
            /*
            for (int i = 0; i < 27; i += 3)
            {
                string sTemp = sData.Substring(i, 2);
                int nTemp = 0;
                switch (i)
                {
                    case 0:
                        // negative Werte noch nicht berücksichtigt!!!
                        nTemp = HexToDec(sTemp);
                        fTemperatureOutside = 0;
                        fTemperatureOutside = (double)nTemp / 256;
                        // siehe http://ebus.webhop.org/twiki/pub/EBus/EBusDoku/Spec_Prot_7_V1_6_3_D.pdf
                        break;
                    case 3:
                        nTemp = HexToDec(sTemp);
                        fTemperatureOutside += nTemp;
                        break;
                    case 6:
                        nSecond = StrToDec(sTemp);
                        break;
                    case 9:
                        nMinute = StrToDec(sTemp);
                        break;
                    case 12:
                        nHour = StrToDec(sTemp);
                        break;
                    case 15:
                        nDay = StrToDec(sTemp);
                        break;
                    case 18:
                        nMonth = StrToDec(sTemp);
                        break;
                    case 21:
                        nDayOfWeek = StrToDec(sTemp);
                        break;
                    case 24:
                        nYear = StrToDec(sTemp);
                        if (nYear < 2000)
                        {
                            nYear += 2000;
                        }
                        break;
                    default:
                        break;
                }
            }
             * */
        }



        private string CalcCRC(string indata)
        {
            string sRet = "";

            return sRet;

        }

        private void btnOpenPort_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                btnOpenPort.Text = "Open Port";
            }
            else
            {
                serialPort1.Open();
                btnOpenPort.Text = "Close Port";
            }
        }

        private void rawDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            btnOpenPort.Text = "Open Port";

            SaveFileDialog oDlg = new SaveFileDialog();

            oDlg.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            oDlg.FilterIndex = 1;
            oDlg.AddExtension = true;
            oDlg.DefaultExt = "txt";
            oDlg.RestoreDirectory = true;

            if (oDlg.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(@oDlg.FileName, tbReceivedDataRaw.Text);
            }
            toolStripStatusLabel1.Text = "raw data saved";
        }

        private void telegramsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            btnOpenPort.Text = "Open Port";

            SaveFileDialog oDlg = new SaveFileDialog();

            oDlg.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            oDlg.FilterIndex = 1;
            oDlg.AddExtension = true;
            oDlg.DefaultExt = "csv";
            oDlg.RestoreDirectory = true;

            if (oDlg.ShowDialog() == DialogResult.OK)
            {
                string sLine = "";
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@oDlg.FileName))
                {
                    //Headline
                    for (int n = 0; n < gridTelegrams.ColumnCount; n++)
                    {
                        sLine += gridTelegrams.Columns[n].HeaderText;
                        sLine += ";";
                    }
                    file.WriteLine(sLine);

                    //Data
                    for (int i = 0; i < gridTelegrams.Rows.Count; i++)
                    {
                        DataGridViewRow row = gridTelegrams.Rows[i];
                        sLine = "";

                        for (int n = 0; n < row.Cells.Count; n++)
                        {
                            if (row.Cells[n].Value != null)
                            {
                                sLine += row.Cells[n].Value.ToString();
                            }
                            else
                            {
                                sLine += " ";
                            }
                            sLine += ";";
                        }
                        file.WriteLine(sLine);
                    }
                }
                toolStripStatusLabel1.Text = "telegram data saved";
            }
        }


        //Umrechnung siehe Spec_Prot_7_V1_6_3_D.pdf
        //http://www.mycsharp.de/wbb2/thread.php?threadid=48671

        private uint GetFromUIN(string indata, int startpos)
        {
            uint nRet = 0;

            string sByte = indata.Substring((startpos - 1) * 3, 2);
            byte nLowByte = HexToByte(sByte);
            sByte = indata.Substring((startpos - 1) * 3+3, 2);
            byte nHighByte = HexToByte(sByte);

            return nRet;
        }

        private char GetFromBCD(string indata, int startpos)
        {
            //0..99
            char nRet = (char) 0;
            string sByte = indata.Substring((startpos - 1) * 3, 2);
            byte nByte = HexToByte(sByte);
            byte nHighNibble = 0xF0;
            byte nLowNibble = 0x0F;

            byte nByteHighNibble = (byte)((nByte & nHighNibble) >> 4);
            byte nByteLowNibble = (byte)(nByte & nLowNibble);

            nRet = (char)(nByteHighNibble * 10 + nByteLowNibble);

            return nRet;
        }
        private char GetFromData1B(string indata, int startpos)
        {
            //-127 ... 127
            char nRet = (char) 0;

            Console.WriteLine("GetFromData1B fehlt noch");

            return nRet;
        }
        
        private char GetFromData1C(string indata, int startpos)
        {
            //0 ..100
            char nRet = (char) 0;
            string sLowByte = indata.Substring((startpos - 1) * 3, 2);

            Console.WriteLine("GetFromData1C fehlt noch");

            return nRet;
        }
        
        private double GetFromData2C(string indata, int startpos)
        {
            //-127.99 ... 127.99
            double fRet = 0.0;

            try
            {
                string sLowByte = indata.Substring((startpos - 1) * 3, 2);
                string sHighByte = indata.Substring(((startpos - 1) * 3) + 3, 2);

                byte nLowByte = HexToByte(sLowByte);
                byte nHighByte = HexToByte(sHighByte);

                Console.WriteLine("GetFromD2C: {0:X},{1:X}", nHighByte, nLowByte);

                byte nCheck = 0x80;

                byte nCheckResult = (byte)(nHighByte & nCheck);
                byte nHighNibble = 0xF0;
                byte nLowNibble = 0x0F;
                
                if (nCheckResult != 0) //negativ
                {
                    nHighByte = (byte)~nHighByte;
                    nLowByte = (byte)~nLowByte;

                    byte nLowByteHighNibble = (byte)((nLowByte & nHighNibble) >> 4);
                    byte nLowByteLowNibble = (byte)(nLowByte & nLowNibble);

                    Console.WriteLine("GetFromD2C: {0:X},{1:X}", nLowByteHighNibble, nLowByteLowNibble);

                    fRet = -1.0* (double)nHighByte * 16.0 - (double)nLowByteHighNibble - (double)(nLowByteLowNibble +1.0) / 16.0;
                }
                else
                {
                    byte nLowByteHighNibble = (byte)((nLowByte & nHighNibble) >> 4);
                    byte nLowByteLowNibble = (byte)(nLowByte & nLowNibble);

                    Console.WriteLine("GetFromD2C: {0:X},{1:X}", nLowByteHighNibble, nLowByteLowNibble);

                    fRet = (double)nHighByte * 16.0 + (double)nLowByteHighNibble + (double) nLowByteLowNibble / 16.0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("exception in GetFromData2C() :" + ex.Message.ToString());
            }

            return fRet;
        }

        private double GetFromData2B(string indata, int startpos)
        {
            //-2047,99 .. 2047,99
            double fRet = 0.0;

            try
            {
                string sLowByte = indata.Substring((startpos - 1) * 3, 2);
                string sHighByte = indata.Substring(((startpos - 1) * 3) + 3, 2);

                byte nLowByte = HexToByte(sLowByte);
                byte nHighByte = HexToByte(sHighByte);

                //Console.WriteLine("GetFromD2B: {0:X},{1:X}", nHighByte, nLowByte);

                byte nCheck = 0x80;

                byte nCheckResult = (byte)(nHighByte & nCheck);

                if (nCheckResult != 0) //negativ
                {
                    nHighByte = (byte)~nHighByte;
                    nLowByte = (byte)~nLowByte;
                    //Console.WriteLine("GetFromD2B: inverted {0:X},{1:X}", nHighByte, nLowByte);
                    fRet = -1.0 * nHighByte - (double)(nLowByte + 1.0) / 256.0;
                }
                else
                {
                    fRet = (double)nHighByte + (double)nLowByte / 256.0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("exception in GetFromData2B() :" + ex.Message.ToString());
            }
            return fRet;
        }

        private int GetFromByte(string indata, int startpos)
        {
            int nRet = 0;

            nRet = StrToDec(indata.Substring((startpos - 1) * 3, 2));

            return nRet;
        }

        private int HexToDec(string hexValue)
        {
            return Int32.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
        }

        private byte HexToByte(string hexValue)
        {
            return byte.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
        }

        private int StrToDec(string strValue)
        {
            return Int32.Parse(strValue);
        }

        private byte StrToByte(string strValue)
        {
            return byte.Parse(strValue);
        }

        private string DecToHex(int decValue)
        {
            return string.Format("{0:x}", decValue);
        }


        private void btnTest_Click(object sender, EventArgs e)
        {
            GetFromData2C("FF FF", 1);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog oDlg = new OpenFileDialog();

            oDlg.Filter = "txt files (*.txt)|*.txt|htm files (*.htm)|*.htm|All files (*.*)|*.*";
            oDlg.FilterIndex = 1;
            oDlg.AddExtension = true;
            oDlg.DefaultExt = "txt";
            oDlg.RestoreDirectory = true;

            if (oDlg.ShowDialog() == DialogResult.OK)
            {
                toolStripStatusLabel1.Text = "importing data";
                using (System.IO.StreamReader file = new System.IO.StreamReader(@oDlg.FileName))
                {
                    try
                    {
                        String sLine;
                        while ((sLine = file.ReadLine()) != null)
                        {

                            String[] sData = sLine.Split('<'); // used for html

                            int count = sData.Length;


                            //count = 2;
                            int i = 0;
                            for (i = 0; i < count; i++)
                            {
                                Console.WriteLine("read data set: {0} of {1}", i, count);

                                if (sData[i].StartsWith("br>")) //used for html
                                {
                                    sData[i] = sData[i].Remove(0, 30);
                                }
                                else if (sData[i].StartsWith("html")) //used for html
                                {
                                    sData[i] = sData[i].Remove(0, 5);
                                }
                                else if (sData[i].StartsWith("body")) //used for html
                                {
                                    sData[i] = sData[i].Remove(0, 18);
                                }
                                else if (sData[i].StartsWith("p>ebus")) //used for html
                                {
                                    sData[i] = sData[i].Remove(0, 29);
                                }
                                else if (sData[i].StartsWith("Sat")) //used for txt
                                {
                                    sData[i] = sData[i].Remove(0, 25);
                                }
                                else if (sData[i].StartsWith("Tue")) //used for txt
                                {
                                    sData[i] = sData[i].Remove(0, 25);
                                }

                                sData[i] = sData[i].ToUpper();

                                string sTemp = "";
                                for (int n = 0; n < sData[i].Length; n = n + 2)
                                {
                                    if ((n + 1) < sData[i].Length)
                                    {
                                        sTemp += sData[i][n];
                                        sTemp += sData[i][n + 1];
                                        sTemp += " ";
                                    }
                                }

                                toolStripStatusLabel1.Text = "data loaded; count " + i.ToString() + " of " + count.ToString();
                                bLoadFile = true;
                                EvaluateData(sTemp);
                                bLoadFile = false;
                                sTemp = "";
                                sData[i] = ""; // löschen; speicher frei machen
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            toolStripStatusLabel1.Text = "data loaded";           
        }
    }

   

}
