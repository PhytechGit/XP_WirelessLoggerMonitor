using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace XP_Monitor
{
    public partial class WellcomePage : Form
    {
        public WellcomePage()
        {
            InitializeComponent();
        }

        private void StartBtn_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }
        public string GetFactory()
        {
            return sFactoryName.Text;
        }
        public string GetWorker()
        {
            return sWorkerName.Text;
        }
    }
}
