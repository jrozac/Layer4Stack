# C# Stack 4 Tcp Client and Server

Framework 4.5 is required to use the solution present in this repository. However also framework 4.0 should work.

## Architecture

The purpose of this library is to simplify the implementation of various Tcp client/server communication scenarios. The library abstracts the C# native Tcp communication classes and brings an event-driven implementation of Tcp client/server communication, which minimizes code replication.

There are three main externally available brick types in the library architecture:
- **Services**. They wrap around the native `TcpClient` and `TcpListener` classes for easier use within a custom application. For each Tcp client a `TcpClientService` class has to be initialized and for each Tcp server a `TcpServerService` class has to be initialized. 
- **Data processors**. Data processors define how raw data being received/sent to/from server/client have to be processed before being sent to event handlers.
- **Events Handlers**. They implement functions which are called upon events in services occur. Such events are: client connected, client disconnected, data received, data sent, etc. Server and client require separate implementations of event handlers as different events occur.

Services are links among data processors and events handlers. Data processors define how raw data are processed, while event handlers define how data items are used. The figure below shows the high-level workflow of data handling valid for both types of services - client and server. 

![Simplified architecture overview](docs/img/architecture.png "Simplified architecture overview")

In a two-way communication there are two asynchronous processes - sending data and receiving data. Data items (complete meaningful data - e.g. text messages) are sent within multiple raw data chunks. In case of sending a data item, the service calls the data processor method `FilterSendData` (1), which filters the data item and returns the filtered raw result (2). After that, the service sends that raw result to the network (3) and triggers the event handler method `HandleSentData` (4). In case when receiving data, data items are received in chunks (5). For each chunk the data processor method `ProcessReceivedRawData` is called (6). After that, the service calls the `GetNewData` data processor method to check if a data item was completely received (7). It is the data processor responsibility to extract data items from raw data chunks, glue them together and prepare data items as they were expected to be received. When a data item is completely received, the `GetNewData` methods returns the data item bytes (8). In that case the service calls the event handler method `HandleReceivedData` (9).

## Example application 

To create a custom application there are three main steps which have to be completed:
- Implement a proprietary data processor or use one of the already implemented. 
- Implement a server event handler and a client event handler.
- Initialize a server and/or a client class to start the server and/or the client.

The aim of the example application is to support the exchange of text messages among a server and multiple clients. Text messages are custom texts ending with a predefined terminator. A terminator can be a custom value, however for the example provided, a line separator is used.


## Data processors

The library already implements a message data processor which is used to build up the example application. However custom data processors may be implemented. 

### Custom data processors 

Implementation of a custom data processor requires to implement a custom configuration class and a custom processor class.  Separate instances of data processors are initialized by the client and the server service upon a client is connected. There are two instances per client, one on the client side and one on the server side. 

A custom configuration class has to implement the `IDataProcessorConfig` where basic properties can be implemented or succeeded by extending the `DataProcessorConfigBase` base class. The basic config properties are:
 - **ProcessorType**. The type of the data processor class to which the config class is linked to.
 - **BufferSize**. Size of raw data chunks being read from the requests stream when receiving data. This value is used by the service.

A custom processor class has to implement the `IDataProcessor` interface and optionally  extend the  `DataProcessorBase` base class. There are three main methods that need to be implemented by the data processor:
 - **FilterSendData**. This method is called before data is sent to client or server. The purpose is to modify the data before being sent.
 - **ProcessReceivedRawData**. This method is called every time after a raw data chunk is received. This method should check for data start and data end and save raw data chunks to private class variables until a complete data item is received.
 - **GetNewData**. This method is called every time after a raw data chunk has been processed. This function should return null if there are no complete data received or a complete data item if received.

### Message data processor example

Message data processor is already implemented as a part of the library and can be used as an illustration on how to write custom data processors.  There are two classes implementing the message data processor, the configuration class `MessageDataProcessorConfig` and the processor class `MessageDataProcessor`.

Message config class displayed below extends the `DataProcessorConfigBase` base class. This was basic properties are already covered by the base class. Furthermore a new property called `MessageTerminator` is added. It represents a byte array used to terminate the message. The value of such terminator can be for example set to new line terminator.

```
    /// <summary>
    /// Message data processor config 
    /// </summary>
    public class MessageDataProcessorConfig : DataProcessorConfigBase<MessageDataProcessor>, IDataProcessorConfig
    {

        /// <summary>
        /// Message terminator
        /// </summary>
        public byte[] MessageTerminator { get; set; }

    }
```

Related to the configuration class above, a message data processor displayed below is implemented. The roles of the implemented methods of the `MessageDataProcessor` class are:
 - **FilterSendData**. This methods takes the input string (as a byte array), ads the message terminator and returns the glued value.
 - **ProcessReceivedRawData**.  This method searches for message terminators inside received raw data chunks. Until a message terminator is not found, it glues the received data and stores the glued value to local variable. 
 - **GetNewData**. This method checks if a complete data item is received. If received it returns the data item without the message terminator. Otherwise it returns null value.

```
    /// <summary>
    /// Message data processor
    /// </summary>
    public class MessageDataProcessor : DataProcessorBase, IDataProcessor
    {

        /// <summary>
        /// Currently receiving message 
        /// </summary>
        private byte[] _message = new byte[10000];


        /// <summary>
        /// Currently receiveing message size
        /// </summary>
        private int _messageBufferSize = 0;


        /// <summary>
        /// Last Message size
        /// </summary>
        private int _lastMessageSize = 0;


        /// <summary>
        /// Last completely received message 
        /// </summary>
        private byte[] _lastMessage;


        /// <summary>
        /// Gets local config
        /// </summary>
        private MessageDataProcessorConfig _internalConfig {
            get {

                return (MessageDataProcessorConfig)Config;
            }
        }


        /// <summary>
        /// Gets message terminator position
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needle"></param>
        /// <returns></returns>
        private int GetMessageTerminatorPosition(byte[] haystack)
        {
            var len = _internalConfig.MessageTerminator.Length;
            var limit = haystack.Length - len;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (_internalConfig.MessageTerminator[k] != haystack[i + k]) break;
                }
                if (k == len) return i;
            }
            return -1;
        }


        /// <summary>
        /// Gets last completely received data item
        /// </summary>
        /// <returns></returns>
        public byte[] GetNewData()
        {
            byte[] retVal = new byte[_lastMessageSize];
            Buffer.BlockCopy(_lastMessage, 0, retVal, 0, _lastMessageSize);
            _lastMessage = null;
            return retVal;
        }


        /// <summary>
        /// Processes received data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        public void ProcessReceivedRawData(byte[] buffer, int length)
        {

            // check for termintor
            int terminatorPos = GetMessageTerminatorPosition(buffer);

            // copy buffer to message
            Buffer.BlockCopy(buffer, 0, _message, _messageBufferSize, terminatorPos != -1 ? terminatorPos : length);
            _messageBufferSize += terminatorPos != -1 ? terminatorPos : length;

            // message received
            if (terminatorPos != -1)
            {

                // set received message
                byte[] receivedMessage = new byte[_messageBufferSize];
                Buffer.BlockCopy(_message, 0, receivedMessage, 0, _messageBufferSize);

                // message received
                _lastMessage = _message;
                _lastMessageSize = _messageBufferSize;

                // clear message
                _message = new byte[10000];
                _messageBufferSize = 0;

                // add remaining read to message
                if (length > terminatorPos + _internalConfig.MessageTerminator.Length)
                {
                    Buffer.BlockCopy(
                        buffer,
                        terminatorPos + _internalConfig.MessageTerminator.Length,
                        _message,
                        0, length - terminatorPos - _internalConfig.MessageTerminator.Length);
                }

            }

            // clear buffer
            buffer = new byte[256];

        }


        /// <summary>
        /// Filters outgoing message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] FilterSendData(byte[] msg)
        {
            // set a message terminator
            byte[] msgBlcok = new byte[msg.Length + _internalConfig.MessageTerminator.Length];
            Buffer.BlockCopy(msg, 0, msgBlcok, 0, msg.Length);
            Buffer.BlockCopy(_internalConfig.MessageTerminator, 0, msgBlcok, msg.Length, _internalConfig.MessageTerminator.Length);

            return msgBlcok;
        }
    }

```

## Event handlers
Event handlers have to be implemented according to provided interfaces `IClientEventHandler` and `IServerEventHandler`.  Events are triggered by client and server services. 

### Client event handler
To use the client functionality, it is required to implement the `IClientEventHandler` interface. During the code execution, events are fired and functions implemented in a custom class implementation are triggered. Below is an example which mainly display event data in console. The code is pretty straightforward. 

```
/// <summary>
/// Client event handler
/// </summary>
public class ClientEventHandler : IClientEventHandler
{

	/// <summary>
	/// Client connected to server
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="info"></param>
	public void HandleClientConnected(IClientService senderObj, ClientInfoModel info)
	{
		Console.WriteLine(string.Format("Client connected to server {0} on port {1}.", info.IpAddress, info.Port));
	}


	/// <summary>
	/// Client failed to connect to server
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="info"></param>
	public void HandleClientConnectionFailure(IClientService senderObj, ClientInfoModel info)
	{
		Console.WriteLine(string.Format("Client failed to connect to server {0} on port {1}.", info.IpAddress, info.Port));
	}


	/// <summary>
	/// Client disconnected from server.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="info"></param>
	public void HandleClientDisconnected(IClientService senderObj, ClientInfoModel info)
	{
		Console.WriteLine(string.Format("Client disconnected from server {0} on port {1}.", info.IpAddress, info.Port));
	}


	/// <summary>
	/// Data received handler. Fired after processed by DataProcessor.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="data"></param>
	public void HandleReceivedData(IClientService senderObj, DataModel data)
	{
		string msg = Encoding.UTF8.GetString(data.Payload);
		Console.WriteLine(string.Format("A message received from server is {0}.", msg));
	}

	/// <summary>
	/// Data sent handler.Fired after processed by DataProcessor.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="data"></param>
	public void HandleSentData(IClientService senderObj, DataModel data)
	{
		string msg = Encoding.UTF8.GetString(data.Payload);
		Console.WriteLine(string.Format("A message sent to server is {0}.", msg));
	}

}

```

### Sever event handler

To use the server functionality, it is required to implement the `IServerEventHandler` interface. During the code execution events are fired and functions implemented in a custom class implementation are triggered. Below is an example implementation which mainly displays events in console. The code is pretty straightforward.

```
/// <summary>
/// Server event handler
/// </summary>
public class ServerEventHandler : IServerEventHandler
{

	/// <summary>
	/// It displays a new client connected message.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="info"></param>
	public void HandleClientConnected(IServerService senderObj, ClientInfoModel info)
	{
		Console.WriteLine(string.Format("Client {0} with IP {1} connected on port {2}.", info.Id, info.IpAddress, info.Port));
	}


	/// <summary>
	/// It displays a client disconnected message.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="info"></param>
	public void HandleClientDisconnected(IServerService senderObj, ClientInfoModel info)
	{
		Console.WriteLine(string.Format("Client {0} with IP {1} disconnected on port {2}.", info.Id, info.IpAddress, info.Port));
	}


	/// <summary>
	/// Handles received data.
	/// It disconnects a client if "exit" received.
	/// It stops the server if "stop" recieved.
	/// In all other cases it reverses the string and sends it back to client.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="data"></param>
	public void HandleReceivedData(IServerService senderObj, DataModel data)
	{
		// log received message 
		string msg = Encoding.UTF8.GetString(data.Payload);
		Console.WriteLine("A message '{0}' was received from client {1}.", data.ClientId, msg);

		// disconnect client if exit received 
		if(msg.Trim().ToLowerInvariant() == "exit")
		{
			senderObj.DisconnectClient(data.ClientId);
			return;
		}

		// stop server if stop received
		if(msg.Trim().ToLowerInvariant() == "stop")
		{
			senderObj.Stop();
			return;
		}

		// reverse message 
		char[] charArray = msg.ToCharArray();
		Array.Reverse(charArray);
		msg = new string(charArray);

		// send reversed
		senderObj.SendToClient(data.ClientId, Encoding.UTF8.GetBytes(msg));
	}


	/// <summary>
	/// Displays received data.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="data"></param>
	public void HandleSentData(IServerService senderObj, DataModel data)
	{
		string msg = Encoding.UTF8.GetString(data.Payload);
		Console.WriteLine("A message '{0}' was sent to client {1}.", data.ClientId, msg);
	}


	/// <summary>
	/// Displays a message server started.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="config"></param>
	public void HandleServerStarted(IServerService senderObj, ServerConfigModel config)
	{
		Console.WriteLine("Server started.");
	}


	/// <summary>
	/// Displays a message server failed to start
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="config"></param>
	public void HandleServerStartFailure(IServerService senderObj, ServerConfigModel config)
	{
		Console.WriteLine("Server failed to start.");
	}


	/// <summary>
	/// Displays a message stopped
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="config"></param>
	public void HandleServerStopped(IServerService senderObj, ServerConfigModel config)
	{
		Console.WriteLine("Server stopped.");
	}

}
```

Multiple clients can be connected to one server. For this reason, events methods attributes object do include the identification value for each client connected. Therefore only one implementation of `IServerEventHandler` is allowed and it should handle multiple clients.

## Example program

Using the already implemented message data processor and implemented event handlers it is not that difficult to build up a demo client/server program. The code below implements a simple message exchange server and client. If the program is run with argument `server`, it runs in server mode an accepts for localhost connections on port 1234. Otherwise it runs in client mode and tries to connect to the server. There can be multiple clients connected to one server.

```

    /// <summary>
    /// Example program
    /// </summary>
    class Program
    {

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
        private static byte[] _delimiter = new byte[]{ 13 };

        
        /// <summary>
        /// Runs client 
        /// </summary>
        static void RunClient()
        {

            // setup client
            TcpClientService client = new TcpClientService
            {
                // client config
                ClientConfig = new ClientConfigModel
                {
                    IpAddress = _ipAddress,
                    Port = _port
                },

                // event handler
                EventHandler = new ClientEventHandler(),

                // data processor 
                DataProcessorConfig = new MessageDataProcessorConfig
                {
                    MessageTerminator = _delimiter
                }
            };

            // UI commands 
            Console.WriteLine("========================================================================");
            Console.WriteLine("Commands");
            Console.WriteLine("quit - Quits the program");
            Console.WriteLine("========================================================================");
            Console.WriteLine();

            // connect
            client.Connect();

            // read user input for messages to send to the server
            while(true) {

                string msg = Console.ReadLine();
                if(!string.IsNullOrWhiteSpace(msg))
                {
                    // send
                    client.Send(Encoding.UTF8.GetBytes(msg));
                    
                    // stop on exit message
                    if(msg.ToLowerInvariant() == "quit")
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
            TcpServerService server = new TcpServerService
            {
                // server config 
                ServerConfig = new ServerConfigModel
                {
                    IpAddress = _ipAddress,
                    Port = _port
                },

                // event handler 
                EventHandler = new ServerEventHandler(),

                // data processor 
                DataProcessorConfig = new MessageDataProcessorConfig
                {
                    MessageTerminator = _delimiter
                }
            };

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
                if(string.IsNullOrWhiteSpace(cmd))
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
                } else if(cmd == "status")
                {
                    Console.WriteLine(server.Started ? "Server is running." : "Server is stopped.");
                } else
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

            if(args.Length > 0 && args[0].Trim().ToLowerInvariant() == "server")
            {
                Console.WriteLine("Running server. Use 'stack4demo client' command to start the client.");
                RunServer();
            } else
            {
                Console.WriteLine("Running server. Use 'stack4demo server' command to start the server.");
                RunClient();
            }
            
        }
    }

```
