using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace LoadCellWF
{
    public class LoadCellController
    {
        public struct CommandStructure
        {
            public string name;
            public string description;
            public string parameters;
            public string returns;
            public int timeout;
            public int response;
        }
        public bool RunLoop { get; set; }
        public SerialPort LoadCellPort = null;
        public SerialMessenger _cmdMessenger = null;
        long _beginTime = 0;                                        // Start time, 1st item of sequence received 
        long _endTime = 0;                                          // End time, last item of sequence received 
        public Socket client;
        public Dictionary<string, int> CommandNumber = new Dictionary<string, int>();
        public PipeClient pipeClient;
        public string comport = null;

        public CommandStructure[] commandStructure = new CommandStructure[] {
        new CommandStructure{name="Acknowledge", description="0 Command to acknowledge that cmd was received",parameters="",returns="", timeout=1000, response= -1},
        new CommandStructure{name="Error",  description="1 Command to report errors",parameters="",returns="i", timeout=1000, response= -1},
        new CommandStructure{name="GetFwVerNum", description="2 Command to get firmware version as a float",parameters="",returns="f", timeout=1000, response= -1},
        new CommandStructure{name="GetFwVerStr", description="3 Command to get firmware version as a string",parameters="",returns="s", timeout=1000, response= -1},
        new CommandStructure{name="SetDACRawVoltage",description="4 Command to set DAC value (e.g. \"1\" sets to 5V for 5V/native unit) -- added in V1.0.0",parameters="if",returns="", timeout=1000, response= -1},
        new CommandStructure{name="GetADCRawVoltage",description="5 Command to get ADC value in unscaled units -- added in V1.0.0",parameters="if",returns="", timeout=1000, response= -1},
        new CommandStructure{name="SetDACScalingFactor", description="6 Command to set Ratio of Scaled to Native (e.g. 5)",parameters="if",returns="", timeout=1000, response= -1},
        new CommandStructure{name="SetDACScaledVoltage", description="7 Command to set DAC scaled value -- added in V1.0.0",parameters="if",returns="", timeout=1000, response= -1},
        new CommandStructure{name="SetADCVoltageScalingFactor", description="8 Command to set Ratio of Scaled to Native Voltage, -- added in V1.0.0",parameters="if",returns="", timeout=1000, response= -1},
        new CommandStructure{name="GetADCScaledVoltage", description="9 Command to get the current (scaled) value reported by the unit -- added in V1.0.0",parameters="if",returns="f", timeout=1000, response= -1},
        new CommandStructure{name="GetADCScaledVoltage3", description="10 Command to get the current (scaled) values (3 channels) reported by the unit -- added in V1.0.0",parameters="",returns="fff", timeout=1000, response= -1},
        new CommandStructure{name="ADCScaledVoltageStreamDisable", description="12 Command to stop streaming of scaled values",parameters="",returns="", timeout=1000, response= -1},
        new CommandStructure{name="SetHX711OffsetValue", description="13 Command to set offset -- added in V1.1.0",parameters="if",returns="", timeout=1000, response= -1},
        new CommandStructure{name="SetHX711ScalingFactor", description="14 Command to set scale factor -- added in V1.1.0",parameters="if",returns="", timeout=1000, response= -1},
        new CommandStructure{name="GetHX711ScaledValue", description="15 Command to get the current (scaled) value reported by the unit -- added in V1.0.0",parameters="if",returns="f", timeout=1000, response= -1},
        new CommandStructure{name="GetHX711ScaledValue3", description="16 Command to get the current (scaled) values (3 channels) reported by the unit -- added in V1.1.0",parameters="",returns="fff", timeout=1000, response= -1},
        new CommandStructure{name="HX711ScaledValueStreamEnable", description="17 Command to enable streaming of scaled values -- added in V1.1.0",parameters="",returns="", timeout=1000, response= -1},
        new CommandStructure{name="HX711ScaledValueStreamDisable", description="18 Command to stop streaming of scaled values -- added in V1.1.0",parameters="",returns="", timeout=1000, response= -1},
        new CommandStructure{name="GetStatus", description="19 Command to get current module status",parameters="",returns="i", timeout=1000, response= -1},
        };
        public MacroRunner macroRunner;
        public string CurrentMacro;
        public Form1 parent;
        public LoadCellController( string runthis, Form1 parentIn )
        {
            parent = parentIn;
            int i = 0;
            foreach (CommandStructure command in commandStructure)
            {
                CommandNumber.Add( command.name, i++ );
            }
            CurrentMacro = runthis;
            serialSetup();

            /* Write CommandStructure
            string json=Newtonsoft.Json.JsonConvert.SerializeObject( commandStructure, Formatting.Indented );
            string docPath = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
            using (StreamWriter outputFile = new StreamWriter( Path.Combine( docPath, "commandStructure.txt" ) ))
            {
                foreach (string line in json.Split('\n'))
                {
                    outputFile.WriteLine( line );
                }
            
            }
            */
            /*Read commandStructure
            string docPath = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
            string json=System.IO.File.ReadAllText( @Path.Combine( docPath, "commandStructure.txt" ) );
            CommandStructure[] loaded = new CommandStructure[] { };
            loaded= (CommandStructure[])Newtonsoft.Json.JsonConvert.DeserializeObject( json );
            */
        }

        private void serialSetup()
        {
            LoadCellPort = new SerialPort();
            try
            {
                //get name of comport associated to PumpValve (as obtained by Listports.py)
                ComPortMap map = new ComPortMap();
                comport = map.GetComPort( "LOADCELL" );
                LoadCellPort.PortName = comport;
                LoadCellPort.BaudRate = 115200;
                LoadCellPort.DataBits = 8;
                LoadCellPort.StopBits = StopBits.One;
                LoadCellPort.Parity = Parity.None;
                LoadCellPort.ReadTimeout = 500;
                LoadCellPort.WriteTimeout = 500;
                LoadCellPort.Handshake = Handshake.None;
                LoadCellPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show( comport + " Exception: " + ex.Message );
            }

        }


        // This is the list of recognized commands. These can be commands that can either be sent or received. 


        // In order to receive, attach a callback function to these events

        private delegate void SetControlPropertyThreadSafeDelegate(
                         System.Windows.Forms.Control control,
                         string propertyName,
                         object propertyValue );

        public void SetControlPropertyThreadSafe(
            Control control,
            string propertyName,
            object propertyValue )
        {
            if (control.InvokeRequired)
            {
                control.Invoke( new SetControlPropertyThreadSafeDelegate
                ( SetControlPropertyThreadSafe ),
                new object[] { control, propertyName, propertyValue } );
            }
            else
            {
                control.GetType().InvokeMember(
                    propertyName,
                    BindingFlags.SetProperty,
                    null,
                    control,
                    new object[] { propertyValue } );
            }
        }








        async public Task SocketMode( string[] CmdLineArgs )
        {
            PipeClient pipeClient = new PipeClient();
            var mr = new MacroRunner( this, pipeClient, null );
            //Thread macroThread = new Thread( new ThreadStart( mr.RunMacro ) );
            mr.RunMacro();
        }
    }
}