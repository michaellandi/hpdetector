/*
 * HPDetector
 * Copyright © 2009 Michael Landi
 * 
 * http://www.sourcesecure.net
 */

/*
 * This file is part of HPDetector.
 *
 * HPDetector is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * HPDetector is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with HPDetector.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HPDetector
{
    internal enum Protocol
    {
        TCP,
        UDP
    }

    internal sealed partial class frmMain : Form
    {
        private const int       DEFAULTSTART    = 1;
        private const int       DEFAULTSTOP     = 65535;

        private int             _intStart;
        private int             _intStop;
        private bool            _bRunning;
        private List<int>       _lstTCPBound;
        private List<int>       _lstTCPNetstat;
        private List<int>       _lstUDPBound;
        private List<int>       _lstUDPNetstat;

        public frmMain()
        {
            _intStart = DEFAULTSTART;
            _intStop = DEFAULTSTOP;
            _bRunning = false;
            _lstTCPBound = new List<int>();
            _lstUDPBound = new List<int>();
            _lstTCPNetstat = new List<int>();
            _lstUDPNetstat = new List<int>();

            InitializeComponent();
        }

        private void Start()
        {
            _bRunning = true;

            this.UseWaitCursor = true;
            Invoke(new OnDisableControl(DisableControl), cmdStart);
            Invoke(new OnDisableControl(DisableControl), cmdCancel);
            Invoke(new OnDisableToolStripItem(DisableToolStripItem), fullPortScanToolStripMenuItem);
            Invoke(new OnDisableToolStripItem(DisableToolStripItem), basePortScan11023ToolStripMenuItem);

            if (_bRunning)
            {
                Invoke(new OnUpdateStatus(UpdateStatus), "(1/5)  Parsing TCP information...");
                Invoke(new OnUpdateProgress(UpdateProgress), 1, 2);
                GetNetstat(_lstTCPNetstat, Protocol.TCP);
                Invoke(new OnUpdateProgress(UpdateProgress), 2, 2);
                Thread.Sleep(500);
            }

            if (_bRunning)
            {
                Invoke(new OnUpdateStatus(UpdateStatus), "(2/5)  Probing application bound TCP ports...");
                GetTCPBound();
                Thread.Sleep(500);
                Invoke(new OnClearPortLabel(ClearPortLabel));
            }

            if (_bRunning)
            {
                Invoke(new OnUpdateStatus(UpdateStatus), "(3/5)  Parsing UDP information...");
                Invoke(new OnUpdateProgress(UpdateProgress), 1, 2);
                GetNetstat(_lstUDPNetstat, Protocol.UDP);
                Invoke(new OnUpdateProgress(UpdateProgress), 2, 2);
                Thread.Sleep(500);
            }

            if (_bRunning)
            {
                Invoke(new OnUpdateStatus(UpdateStatus), "(4/5)  Probing application bound UDP ports...");
                GetUDPBound();
                Thread.Sleep(500);
                Invoke(new OnClearPortLabel(ClearPortLabel));
            }

            if (_bRunning)
            {
                int intItems = _lstTCPBound.Count + _lstUDPBound.Count;
                Invoke(new OnUpdateStatus(UpdateStatus), "(5/5)  Comparing and checking " + intItems.ToString() + " suspicious ports...");
                Compare();
                Thread.Sleep(500);
            }

            Invoke(new OnClearPortLabel(ClearPortLabel));
            Invoke(new OnEnableControl(EnableControl), cmdStart);
            Invoke(new OnEnableControl(EnableControl), cmdCancel);
            Invoke(new OnEnableToolStripItem(EnableToolStripItem), fullPortScanToolStripMenuItem);
            Invoke(new OnEnableToolStripItem(EnableToolStripItem), basePortScan11023ToolStripMenuItem);
            this.UseWaitCursor = false;

            if (_bRunning)
                Invoke(new OnUpdateStatus(UpdateStatus), "Finished");
            else
            {
                Invoke(new OnUpdateStatus(UpdateStatus), "Finished");
                Invoke(new OnUpdateText(UpdateText), "User cancelled operation.");
                Invoke(new OnUpdateProgress(UpdateProgress), 0, 1);
            }

            _bRunning = false;
        }

        #region Detection_Functions

        private void GetNetstat(List<int> list, Protocol protocol)
        {
            Process p;
            int intPortBuffer;
            string strOutput;
            string[] strItem;
            string[] strEntry;
            string[] strPart;
            
            p = new Process();
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "netstat";

            if (protocol == Protocol.TCP)
                p.StartInfo.Arguments = "-na -p tcp";
            else
                p.StartInfo.Arguments = "-na -p udp";

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            strOutput = p.StandardOutput.ReadToEnd().Replace("\t", "").Trim();
            p.WaitForExit();

            while (strOutput.Contains("  "))
                strOutput = strOutput.Replace("  ", " ");

            strEntry = strOutput.Split('\n');

            foreach (string strBuffer in strEntry)
                if (strBuffer.Contains(":"))
                {
                    strItem = strBuffer.Trim().Split(' ');
                    strPart = strItem[1].Split(':');

                    intPortBuffer = Int32.Parse(strPart[1]);
                    
                    if (intPortBuffer <= _intStop && intPortBuffer >= _intStart)
                        list.Add(intPortBuffer);
                }
        }

        private void GetUDPBound()
        {
            for (int i = _intStart; i <= _intStop; i++)
            {
                if (IsBound(i, Protocol.UDP))
                    _lstUDPBound.Add(i);

                Invoke(new OnUpdateProgress(UpdateProgress), i, _intStop);
                Invoke(new OnUpdatePortLabel(UpdatePortLabel), i, Protocol.UDP);

                if (!_bRunning)
                    break;
            }
        }

        private void GetTCPBound()
        {
            for (int i = _intStart; i <= _intStop; i++)
            {
                if (IsBound(i, Protocol.TCP))
                    _lstTCPBound.Add(i);

                Invoke(new OnUpdateProgress(UpdateProgress), i, _intStop);
                Invoke(new OnUpdatePortLabel(UpdatePortLabel), i, Protocol.TCP);

                if (!_bRunning)
                    break;
            }
        }

        private bool IsBound(int port, Protocol protocol)
        {
            if (protocol == Protocol.TCP)
            {
                try
                {
                    TcpListener tListener = new TcpListener(IPAddress.Any, port);
                    tListener.Start();
                    tListener.Stop();
                    tListener = null;

                    return false;
                }
                catch
                {
                    return true;
                }
            }
            else if (protocol == Protocol.UDP)
            {
                try
                {
                    IPEndPoint iepEndPoint = new IPEndPoint(IPAddress.Any, port);
                    Socket sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sSocket.Bind(iepEndPoint);
                    sSocket.Close();
                    sSocket = null;

                    return false;
                }
                catch
                {
                    return true;
                }
            }
            else
                throw(new Exception("Unknown protocol type: " + protocol.ToString()));
        }

        private bool DoesExist(List<int> list, int item) 
        {
            foreach (int iBuffer in list)
                if (iBuffer == item)
                    return true;

            return false;
        }

        private void Compare()
        {
            int iHidden = 0;
            List<int> lTempList = new List<int>();

            for (int i = 0; i < _lstTCPBound.Count; i++)
            {
                if (!DoesExist(_lstTCPNetstat, _lstTCPBound[i]))
                {
                    bool inUse = IsBound(_lstTCPBound[i], Protocol.TCP);

                    GetNetstat(lTempList, Protocol.TCP);

                    if (DoesExist(lTempList, _lstTCPBound[i]) != inUse)
                    {
                        Invoke(new OnUpdatePorts(UpdatePorts), _lstTCPBound[i], Protocol.TCP);
                        iHidden++;
                    }
                }

                Invoke(new OnUpdatePortLabel(UpdatePortLabel), _lstTCPBound[i], Protocol.TCP);
                Invoke(new OnUpdateProgress(UpdateProgress), i + 1, _lstTCPBound.Count + _lstUDPBound.Count);

                Thread.Sleep(500);
            }

            for (int i = 0; i < _lstUDPBound.Count; i++)
            {
                if (!DoesExist(_lstUDPNetstat, _lstUDPBound[i]))
                {
                    bool inUse = IsBound(_lstUDPBound[i], Protocol.UDP);

                    GetNetstat(lTempList, Protocol.UDP);

                    if (DoesExist(lTempList, _lstUDPBound[i]) != inUse)
                    {
                        Invoke(new OnUpdatePorts(UpdatePorts), _lstUDPBound[i], Protocol.UDP);
                        iHidden++;
                    }

                    Thread.Sleep(500);
                }

                Invoke(new OnUpdatePortLabel(UpdatePortLabel), _lstUDPBound[i], Protocol.UDP);
                Invoke(new OnUpdateProgress(UpdateProgress), i + 1 + _lstTCPBound.Count, _lstTCPBound.Count + _lstUDPBound.Count);
            }


            if (iHidden == 0)
                Invoke(new OnUpdateText(UpdateText), "No hidden ports detected.");
            else
                Invoke(new OnUpdateText(UpdateText), iHidden.ToString() + " hidden ports detected.");
        }

        #endregion

        #region Event_Handlers

        private void cmdStart_Click(object sender, EventArgs e)
        {
            txtPorts.Text = "";
            _lstTCPBound.Clear();
            _lstUDPBound.Clear();
            _lstTCPNetstat.Clear();
            _lstUDPNetstat.Clear();

            new Thread(Start).Start();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_bRunning)
            {
                e.Cancel = true;
                DialogResult dResult = MessageBox.Show(this, "The application is collecting information, would you like to terminate?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (dResult == DialogResult.Yes)
                {
                    _bRunning = false;
                    Invoke(new OnUpdateStatus(UpdateStatus), "Cancelling...");
                }
            }
            else
                Environment.Exit(0);
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void fullPortScanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_bRunning)
                return;

            fullPortScanToolStripMenuItem.Checked = true;
            basePortScan11023ToolStripMenuItem.Checked = false;

            _intStart = DEFAULTSTART;
            _intStop = DEFAULTSTOP;
        }

        private void basePortScan11023ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_bRunning)
                return;

            fullPortScanToolStripMenuItem.Checked = false;
            basePortScan11023ToolStripMenuItem.Checked = true;

            _intStart = DEFAULTSTART;
            _intStop = 1023;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void scanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_bRunning)
                return;

            cmdStart_Click(this, EventArgs.Empty);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new frmAbout().ShowDialog(this);
        }

        #endregion

        #region Event_Delegates

        private delegate void OnUpdateProgress(int value, int count);

        private void UpdateProgress(int value, int count)
        {
            pbMain.Maximum = count;
            pbMain.Value = value;
        }

        private delegate void OnUpdateStatus(string status);

        private void UpdateStatus(string status)
        {
            lblStatus.Text = status;
        }

        private delegate void OnEnableControl(Control ctl);

        private void EnableControl(Control ctl)
        {
            ctl.Enabled = true;
        }

        private delegate void OnDisableControl(Control ctl);

        private void DisableControl(Control ctl)
        {
            ctl.Enabled = false;
        }

        private delegate void OnDisableToolStripItem(ToolStripItem ctl);

        private void DisableToolStripItem(ToolStripItem ctl)
        {
            ctl.Enabled = false;
        }

        private delegate void OnEnableToolStripItem(ToolStripItem ctl);

        private void EnableToolStripItem(ToolStripItem ctl)
        {
            ctl.Enabled = true;
        }

        private delegate void OnUpdatePorts(int port, Protocol protocol);

        private void UpdatePorts(int port, Protocol protocol)
        {
            if (txtPorts.Text == "")
                txtPorts.Text = protocol.ToString().ToUpper() + "/" + port.ToString() + ":\tMay be a hidden port.";
            else
                txtPorts.Text += Environment.NewLine + protocol.ToString().ToUpper() + "/" + port.ToString() + ":\tMay be a hidden port.";
        }

        private delegate void OnUpdateText(string text);

        private void UpdateText(string text)
        {
            if (txtPorts.Text == "")
                txtPorts.Text = text;
            else
                txtPorts.Text += Environment.NewLine + text;
        }

        private delegate void OnUpdatePortLabel(int port, Protocol protocol);

        private void UpdatePortLabel(int port, Protocol protocol)
        {
            lblPort.Text = "Port: " + protocol.ToString().ToUpper() + "/" + port.ToString();
        }

        private delegate void OnClearPortLabel();

        private void ClearPortLabel()
        {
            lblPort.Text = "";
        }

        #endregion
    }
}