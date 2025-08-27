using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Deployment.Application;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    public partial class FormVersion : Form
    {
        public FormVersion()
        {
            InitializeComponent();
        }

        private void Form4_Load(object sender, EventArgs e)
        {
            System.Reflection.Assembly assembly = Assembly.GetExecutingAssembly();
            System.Reflection.AssemblyName asmName = assembly.GetName();
            System.Version version = asmName.Version;

            labelVersionNo.Text = "Version " + version.ToString();

            //labelVersionNo.Text = "Version " + GetVersion();

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(this.linkLabel1.Text);
        }

        private string GetVersion()
        {
            if (!ApplicationDeployment.IsNetworkDeployed) return String.Empty;

            var version = ApplicationDeployment.CurrentDeployment.CurrentVersion;
            return (
            version.Major.ToString() + "." +
            version.Minor.ToString() + "." +
            version.Build.ToString() + "." +
            version.Revision.ToString()
);
        }
    }
}
