using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadCellWF
{
    public class SerialMessenger
    {
        public class SendCommand
        {
            public bool ReqAc;
            public bool Ok;
            public string RawString;
            public int CmdId;

            public SendCommand( int cmdNumber )
            {
                CmdId = cmdNumber;

            }

            public SendCommand( int cmdNumber, int response1, int timeout )
            {
                CmdId = cmdNumber;

            }

            public void AddArgument( int i )
            {
                RawString += "," + i.ToString();
            }
            public void AddArgument( long l )
            {
                RawString += "," + l.ToString();
            }
            public void AddArgument( bool b )
            {
                RawString += "," + b.ToString();
            }
            public void AddArgument( string s )
            {
                RawString += "," + s;
            }

        }

        public class ReceivedCommand
        {
            public bool ReqAc;
            public bool Ok;
            public string RawString;
        }

        SerialPort comport;
        SerialMessenger( SerialPort com )
        {
            comport = com;
        }

        public ReceivedCommand Send( SendCommand cmd )
        {
            ReceivedCommand reslt = new ReceivedCommand();
            comport.Write( cmd.CmdId.ToString() + cmd.RawString + ";" );
            reslt.RawString = comport.ReadLine(); // read echoed characters
            if (reslt.RawString != cmd.RawString)
            {

            }
            reslt.RawString = comport.ReadLine(); //read actual response
            return reslt;

        }
        public ReceivedCommand receiveMessage()
        {
            ReceivedCommand received = new ReceivedCommand();

            return received;


        }


    }
}
