/*
 * HPDetector
 * Copyright © 2009 Michael Landi
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
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace HPDetector
{
    public partial class frmAbout : Form
    {
        public frmAbout()
        {
            InitializeComponent();

            try
            {
                lblVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch { }
        }

        private void lblURL_Click(object sender, EventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = "http://www.sourcesecure.net";
            p.Start();
        }
    }
}