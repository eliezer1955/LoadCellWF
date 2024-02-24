using Communication.Core;
using NamedPipes.Client;
using NamedPipes.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class PipeClient
{
    public SemaphoreSlim semaphore = new SemaphoreSlim( 0, 1 );
    public IClient client;
    public string lastReceive=null;
    public string lastResponse = null;
    public readonly object _writerSemaphore = new object();
    public string cmdToSend = null;
    public string name = "LoadCell";

    public PipeClient()
    {
        Start();
    }

    public  async Task<string>  receive()
    {
        while (string.IsNullOrWhiteSpace( lastReceive )) await Task.Delay( 10 );
        return lastReceive;
    }
    public async void Start()
    {

        client = new NamedPipeClient( name );
        client.ClientStarted += ( _, args )
            => Console.WriteLine( "CLIENT => Client started." );
        client.ConnectedToServer += ( _, args )
            => Console.WriteLine( "CLIENT => Client connected to server." );
        client.MessageReceived += ( _, args ) =>
        {
            lastReceive = (args as MessageReceivedEventArgs).Message;
            Console.WriteLine( $"CLIENT => Message received from server:" + lastReceive );
            semaphore.Release();
        };
        client.Disconnected += ( _, args ) =>
           Console.WriteLine( $"CLIENT => Server disconnected." );

        //System.Diagnostics.Debugger.Launch();
        await client.Connect();
        Console.WriteLine( "Connecting to server...\n" );
        semaphore.Wait();
        await client.Send( "LoadCell" );
        Console.WriteLine( "Connected!...\n" );
    }

    public async void Send( String Response )
    {
        lastResponse = Response;
        Console.WriteLine( "Responding {0} command:{1}", name, Response );
        //await _writerSemaphore.WaitAsync();
        //AsynchronousSocketListener.Send( stateObject.workSocket, lastCommand );
        //stateObject.workSocket.Send( Encoding.UTF8.GetBytes( lastCommand ) );
        lock (_writerSemaphore)
        {
            //await sw.WriteLineAsync( lastCommand );
            this.cmdToSend = Response;
        }
        while (this.cmdToSend != null)
        {
            Thread.Yield();
        }
    }
}
