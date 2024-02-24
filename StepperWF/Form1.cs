using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoadCellWF
{
    public partial class Form1 : Form
    {
        public string CurrentMacro = "loadcell.tst.txt";
        public LoadCellController loadCellController;
        public string[] CmdLineArgs;
        public bool stopMonitoring = false;
        public string serialNumber;
        public Form1(String[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            CmdLineArgs = args;
            if (args.Length>0 && args[0] != "Slave")
                CurrentMacro = args[0];
            InitializeComponent();
            button2.Text = CurrentMacro;
            loadCellController = new LoadCellController(CurrentMacro, this);
            if (CmdLineArgs.Length > 0)
                if (CmdLineArgs[0] == "Slave")
                {
                    Thread runner = new Thread( () => loadCellController.SocketMode( CmdLineArgs ) );
                    runner.Start();
                }
                else
                {
                    MacroRunner macroRunner = new MacroRunner( loadCellController, null, CurrentMacro );
                    macroRunner.RunMacro();
                }

        }
        // run macro
        private void button1_Click(object sender, EventArgs e)
        {
            Control[] macro = this.Controls.Find("button2", true);
            string CurrentMacro = macro[0].Text;
            MacroRunner macroRunner = new MacroRunner(loadCellController,null, CurrentMacro);
            macroRunner.RunMacro();
        }

        // Select macro
        private void button2_Click(object sender, EventArgs e)
        {
            var picker = new OpenFileDialog();
            picker.FileName = CurrentMacro;
            picker.DefaultExt = "txt";
            picker.InitialDirectory = Environment.CurrentDirectory;
            picker.Filter = "txt files (*.txt)|*.txt";
            if (picker.ShowDialog() == DialogResult.OK)
            {
                CurrentMacro = picker.FileName;
                button2.Text = CurrentMacro;

            }
        }

        public void SetStatus(string s)
        {
            button3.Text = s;
        }

        private void checkBox5_CheckedChanged( object sender, EventArgs e )
        {

        }

        private void checkBox3_CheckedChanged( object sender, EventArgs e )
        {

        }

        private void Form1_Load( object sender, EventArgs e )
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click( object sender, EventArgs e )
        {

        }

        private void forceLeft_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            stopMonitoring = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }
    }
}