using Layer4Stack.DataProcessors;
using Layer4Stack.Models;
using Layer4Stack.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Layer4StackCmdDemo
{

    /// <summary>
    /// Example program
    /// </summary>
    public static class Program
    {

        /// <summary>
        /// Logger factory
        /// </summary>
        private static ILoggerFactory _loggerFactory = new LoggerFactory();

        /// <summary>
        /// Server IP address 
        /// </summary>
        private static string _ipAddress = "127.0.0.1";

        /// <summary>
        /// Port to listen / connect to 
        /// </summary>
        private static int _port = 1234;

        /// <summary>
        /// Message delimiter 
        /// </summary>
        private static byte[] _delimiter = new byte[] { 13 };

        /// <summary>
        /// Runs client 
        /// </summary>
        static void RunClient()
        {

            // setup client
            var config = new ClientConfig(_ipAddress, _port, false);
            TcpClientService client = new TcpClientService(new ClientEventHandler(), config, _loggerFactory, EnumDataProcessorType.Hsm);

            // UI commands 
            Console.WriteLine("========================================================================");
            Console.WriteLine("Commands");
            Console.WriteLine("quit - Quits the program");
            Console.WriteLine("========================================================================");
            Console.WriteLine();

            // connect
            client.Connect();

            // read user input for messages to send to the server
            while (true)
            {

                string msg = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    // send
                    client.Send(Encoding.UTF8.GetBytes(msg));

                    // stop on exit message
                    if (msg.ToLowerInvariant() == "quit")
                    {
                        break;
                    }
                }
            }

            // disconnect
            client.Disconnect();

            // wait for user
            Console.ReadLine();

        }

        /// <summary>
        /// Runs server
        /// </summary>
        static void RunServer()
        {

            // setup server
            var config = new ServerConfig(_ipAddress, _port);
            var handler = new ServerEventHandler();
            TcpServerService server = new TcpServerService(handler, config, _loggerFactory, EnumDataProcessorType.Hsm);

            // setup handler
            handler.EventRequestDisconnect += (obj,client) => server.DisconnectClient(client);
            handler.EventRequestStop += (obj, client) => server.Stop();

            // UI commands 
            Console.WriteLine("========================================================================");
            Console.WriteLine("Commands");
            Console.WriteLine("quit - Quits the program");
            Console.WriteLine("start - Starts the server");
            Console.WriteLine("stop - Stops the server");
            Console.WriteLine("status - Displays the server status");
            Console.WriteLine("Any other input is threated like a message which is sent to all clients.");
            Console.WriteLine("========================================================================");
            Console.WriteLine();

            // start server
            server.Start();

            // read for UI commands
            while (true)
            {
                string cmd = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(cmd))
                {
                    continue;
                }

                if (cmd == "stop")
                {
                    server.Stop();
                }
                else if (cmd == "start")
                {
                    server.Start();
                }
                else if (cmd == "quit")
                {
                    break;
                }
                else if (cmd == "status")
                {
                    Console.WriteLine(server.Started ? "Server is running." : "Server is stopped.");
                }
                else
                {
                    server.SendToAll(Encoding.UTF8.GetBytes(cmd));
                }
            }

            // stop server if not stopped
            server.Stop();

            // wait for enter
            Console.WriteLine("Press Enter to quit the program.");
            Console.ReadLine();

        }

        /// <summary>
        /// Program main
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            if (args.Length > 0 && args[0].Trim().ToLowerInvariant() == "server")
            {
                Console.WriteLine("Running server. Use 'stack4demo client' command to start the client.");
                RunServer();
            }
            else
            {
                Console.WriteLine("Running server. Use 'stack4demo server' command to start the server.");
                RunClient();
            }

        }
    }
}
