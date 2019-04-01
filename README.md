# C# Stack 4 Tcp Client and Server

Framework 4.6.1 or Core 2.1 is required to use the solution present in this repository. Used nugets:
- Microsoft.Extensions.Logging
- NETStandard.Library

Implementation based on framework 4.5 and log4net dependency id deprecated. A legacy release and tag v1.0.9 were bade for backward compatibility.

## Architecture

The purpose of this library is to simplify the implementation of various Tcp client/server communication scenarios. The library abstracts the C# native Tcp communication classes and brings an event-driven implementation of Tcp client/server communication, which minimizes code replication.

There are three main externally available brick types in the library architecture:
- **Services**. They wrap around the native `TcpClient` and `TcpListener` classes for easier use within a custom application. For each Tcp client a `TcpClientService` class has to be initialized and for each Tcp server a `TcpServerService` class has to be initialized. 
- **Data processors**. Data processors define how raw data being received/sent to/from server/client have to be processed before being sent to event handlers. There are already two data processors to exchange text messages implemented. However, a custom data processor could be implemented separately.
- **Events Handlers**. They implement functions which are called upon events in services occur. Such events are: client connected, client disconnected, data received, data sent, etc. Server and client require separate implementations of event handlers as different events occur.

Services are links among data processors and events handlers. Data processors define how raw data are processed, while event handlers define how data items are used. The figure below shows the high-level workflow of data handling valid for both types of services - client and server. 

![Simplified architecture overview](docs/img/architecture.png "Simplified architecture overview")

In a two-way communication there are two asynchronous processes - sending data and receiving data. Data items (complete meaningful data - e.g. text messages) are sent within multiple raw data chunks. In case of sending a data item, the service calls the data processor method `FilterSendData` (1), which filters the data item and returns the filtered raw result (2). After that, the service sends that raw result to the network (3) and triggers the event handler method `HandleSentData` (4). In case when receiving data, data items are received in chunks (5). For each chunk the data processor method `ProcessReceivedRawData` is called (6). In case when a full data item is received (or more of them) the function returns a list of messages, otherwise it returns null (7). It is the data processor responsibility to extract data items from raw data chunks, glue them together and prepare data items as they were expected to be. Upon data item is recieved the service calls the event handler method `HandleReceivedData` (8). In case of multiple messages, the method is called multiple times.

## Example application 

To create a custom application there are three main steps which have to be completed:
- Implement a proprietary data processor or use one of the already implemented. 
- Implement a server event handler and a client event handler.
- Initialize a server and/or a client class to start the server and/or the client.

The aim of the example application is to support the exchange of text messages among a server and multiple clients. Text messages are custom texts ending with a predefined terminator. A terminator can be a custom value, however for the example provided, a line separator is used.

A demo usage implementation is available in the folder 'Demo'

## Data processors

The library already implements two message data processors where one used to build up the example application. However custom data processors may be implemented. 

### Custom data processors 

A custom processor class has to implement the `IDataProcessor` interface. There are three main methods that need to be implemented by the data processor:
 - **FilterSendData**. This method is called before data is sent to client or server. The purpose is to modify the data before being sent.
 - **ProcessReceivedRawData**. This method is called every time after a raw data chunk is received. This method should check for data start and data end and save raw data chunks to private class variables until a complete data item is received.
 - **GetIdentifier**. This method is called when a message is received. As the communication is asynchronous messages can be matched by custom ids. This method should extract the id from raw message and return it.
 - **Reset**. This method should reset internal data. It is called upon service restart or reconnect.

### Message data processor example

Message data processors are already implemented as a part of the library and can be used as an illustration on how to write custom data processors. Simple message data processor is an example of data processor, which packs/extracts data according to the following rules:
- First byte of the message is always 0.
- Second byte represent the message length (up to 255).
- Other bytes represent the message. 
- Encoding  is ASCII.

```cs
/// <summary>
/// Simple message data processor
/// </summary>
public class SimpleMessageDataProcessor : IDataProcessor
{

	/// <summary>
	/// Create hsm processor
	/// </summary>
	/// <param name="logger"></param>
	/// <param name="bufferLength"></param>
	/// <returns></returns>
	public static SimpleMessageDataProcessor CreateProcessor(
		ILogger<SimpleMessageDataProcessor> logger, 
		int bufferLength = 500, int headerLength = 1, 
		Func<byte[], byte[]> func = null) =>
		new SimpleMessageDataProcessor(logger, bufferLength, new byte[1] { 0 }, headerLength, func ?? ((msg) => msg.Length >= 5 ? msg.ToList().Take(4).ToArray() : null));

	/// <summary>
	/// Delimiter
	/// </summary>
	private readonly byte[] _delimiter;

	/// <summary>
	/// Length header size 
	/// </summary>
	private readonly int _headerSize;

	/// <summary>
	/// Id get function
	/// </summary>
	private readonly Func<byte[], byte[]> _getIdFunc;

	/// <summary>
	/// Logger
	/// </summary>
	private readonly ILogger<SimpleMessageDataProcessor> _logger;

	/// <summary>
	/// Recieved buffer
	/// </summary>
	private readonly byte[] _buffer;

	/// <summary>
	/// Buffer current length
	/// </summary>
	private int _bufferLength;

	/// <summary>
	/// Constructor with injected properties 
	/// </summary>
	/// <param name="logger"></param>
	/// <param name="bufferLength"></param>
	/// <param name="delimiter"></param>
	/// <param name="headerSize"></param>
	/// <param name="getIdFunc">Id get function. Used to extract the if from raw message.</param>
	public SimpleMessageDataProcessor(ILogger<SimpleMessageDataProcessor> logger, int bufferLength, byte[] delimiter, int headerSize, Func<byte[], byte[]> getIdFunc = null)
	{

		// check arguments
		if(_delimiter?.Length == 0 || (_headerSize > 3 && _headerSize <= 0) && _bufferLength < _delimiter.Length + headerSize + 1)
		{
			throw new ArgumentException("Invalid setup");
		}

		_delimiter = delimiter;
		_headerSize = headerSize;
		_logger = logger;
		_getIdFunc = getIdFunc;
		_buffer = new byte[bufferLength];
	}

	/// <summary>
	/// Filter send data 
	/// </summary>
	/// <param name="msg"></param>
	/// <returns></returns>
	public byte[] FilterSendData(byte[] msg)
	{
		// length check
		if(msg.Length > MaxSize)
		{
			_logger.LogError("Message is too long.");
			return null;
		}

		// set length header 
		var header = DataProcessorUtil.LengthToHeader(msg.Length, _headerSize);
		var ret = new byte[msg.Length + 1 + _headerSize];
		ret[0] = 0;
		Buffer.BlockCopy(header, 0, ret, 1, _headerSize);
		Buffer.BlockCopy(msg, 0, ret, 1 + _headerSize, msg.Length);
		return ret;
	}

	/// <summary>
	/// Get identifier
	/// </summary>
	/// <param name="msg"></param>
	/// <returns></returns>
	public byte[] GetIdentifier(byte[] msg)
	{
		return _getIdFunc?.Invoke(msg);
	}

	/// <summary>
	/// Process buffer
	/// </summary>
	/// <param name="recieved"></param>
	/// <param name="length"></param>
	/// <returns></returns>
	public IEnumerable<byte[]> ProcessReceivedRawData(byte[] recieved, int length)
	{

		// check package length
		if(length > _buffer.Length)
		{
			_logger.LogError("Recieved is too long.");
			return null;
		}

		// check buffer lenght 
		else if(_bufferLength + length > _buffer.Length)
		{
			_logger.LogError("Buffer overdraw. Deleting buffer.");
			_bufferLength = 0;
			return null;
		}

		// copy message to buffer
		Buffer.BlockCopy(recieved, 0, _buffer, _bufferLength, length);
		_bufferLength = _bufferLength + length;

		// find starts of messages 
		var starts = _buffer.FindOccurrences(_delimiter, _bufferLength, ContainerSize);
		if (!starts.Any())
		{
			return new List<byte[]>();
		}

		// get intervals 
		var intervals = starts.Where(pos => _bufferLength >= pos + ContainerSize &&
			_bufferLength >= pos + ContainerSize + DataProcessorUtil.HeaderToLength(_buffer.Slice(pos + 1, _headerSize))).
			Select(pos => new Tuple<int, int>(pos + ContainerSize,
				(int)DataProcessorUtil.HeaderToLength(_buffer.Slice(pos + 1, _headerSize)))).
			ToArray();

		// no intervals found
		if (!intervals.Any())
		{
			return new List<byte[]>();
		}

		// fix intervals to not overlap 
		intervals = Enumerable.Range(0, intervals.Length).Select(i => {
			var cur = intervals[i];
			if (i + 1 >= intervals.Length)
			{
				return cur;
			}
			var next = intervals[i + 1];
			var endPos = cur.Item1 + cur.Item2 - 1;
			if (endPos >= next.Item1)
			{
				_logger.LogError("Overlapping messages");
				return new Tuple<int, int>(cur.Item1, next.Item1 - cur.Item1);
			}
			return cur;
		}).ToArray();

		// get messages 
		var msgs = _buffer.SliceMulti(intervals);

		// move buffer
		var start = intervals.Last().Item1 + intervals.Last().Item2 + 1;
		if(start >= _bufferLength)
		{
			_bufferLength = 0;
		} else
		{
			var len = _bufferLength - start;
			_bufferLength = _buffer.ReplaceWith(_buffer, start, 0, len);
		}

		// return 
		return msgs;
	}

	/// <summary>
	/// Reset 
	/// </summary>
	public void Reset()
	{
		_bufferLength = 0;
	}

	/// <summary>
	/// Container size 
	/// </summary>
	private int ContainerSize => _headerSize + _delimiter.Length;

	/// <summary>
	/// Max message size 
	/// </summary>
	private int MaxSize => (int) Math.Pow(byte.MaxValue+1, _headerSize)-1;

}
```

## Event handlers
Event handlers have to be implemented according to provided interfaces `IClientEventHandler` and `IServerEventHandler`.  Events are triggered by client and server services. 

### Client event handler
To use the client functionality, it is required to implement the `IClientEventHandler` interface. During the code execution, events are fired and functions implemented in a custom class implementation are triggered. Below is an example which mainly display event data in console. The code is pretty straightforward. 

```cs
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
	public void HandleClientConnected(ClientInfo info)
	{
		Console.WriteLine(string.Format("Client connected to server {0} on port {1}.", info.IpAddress, info.Port));
	}

	/// <summary>
	/// Client failed to connect to server
	/// </summary>
	/// <param name="info"></param>
	public void HandleClientConnectionFailure(ClientInfo info)
	{
		Console.WriteLine(string.Format("Client failed to connect to server {0} on port {1}.", info.IpAddress, info.Port));
	}

	/// <summary>
	/// Client disconnected from server.
	/// </summary>
	/// <param name="info"></param>
	public void HandleClientDisconnected(ClientInfo info)
	{
		Console.WriteLine(string.Format("Client disconnected from server {0} on port {1}.", info.IpAddress, info.Port));
	}

	/// <summary>
	/// Data received handler. Fired after processed by DataProcessor.
	/// </summary>
	/// <param name="data"></param>
	/// <param name="rpcResponse"></param>
	/// <returns></returns>
	public byte[] HandleReceivedData(DataContainer data, bool rpcResponse = false)
	{
		string msg = Encoding.UTF8.GetString(data.Payload);
		Console.WriteLine(string.Format("A message received from server is {0}.", msg));
		return null;
	}

	/// <summary>
	/// Data sent handler.Fired after processed by DataProcessor.
	/// </summary>
	/// <param name="data"></param>
	public void HandleSentData(DataContainer data)
	{
		string msg = Encoding.UTF8.GetString(data.Payload);
		Console.WriteLine(string.Format("A message sent to server is {0}.", msg));
	}

}
```

### Sever event handler

To use the server functionality, it is required to implement the `IServerEventHandler` interface. During the code execution events are fired and functions implemented in a custom class implementation are triggered. Below is an example implementation which mainly displays events in console. The code is pretty straightforward.

```cs
/// <summary>
/// Server event handler
/// </summary>
public class ServerEventHandler : IServerEventHandler
{

	/// <summary>
	/// Request disconnect event
	/// </summary>
	public EventHandler<string> EventRequestDisconnect;

	/// <summary>
	/// Request stop server
	/// </summary>
	public EventHandler<string> EventRequestStop;

	/// <summary>
	/// It displays a new client connected message.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="info"></param>
	public void HandleClientConnected(ClientInfo info)
	{
		Console.WriteLine(string.Format("Client {0} with IP {1} connected on port {2}.", info.Id, info.IpAddress, info.Port));
	}

	/// <summary>
	/// It displays a client disconnected message.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="info"></param>
	public void HandleClientDisconnected(ClientInfo info)
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
	/// <returns></returns>
	public byte[] HandleReceivedData(DataContainer data)
	{
		// log received message 
		string msg = Encoding.UTF8.GetString(data.Payload);
		Console.WriteLine("A message '{0}' was received from client {1}.", data.ClientId, msg);

		// disconnect client if exit received 
		if (msg.Trim().ToLowerInvariant() == "exit")
		{
			EventRequestDisconnect?.Invoke(this, data.ClientId);
			return null;
		}

		// stop server if stop received
		if (msg.Trim().ToLowerInvariant() == "stop")
		{
			EventRequestStop?.Invoke(this, data.ClientId);
			return null;
		}

		// reverse message 
		char[] charArray = msg.ToCharArray();
		Array.Reverse(charArray);
		msg = new string(charArray);
		return Encoding.UTF8.GetBytes(msg);
	}

	/// <summary>
	/// Displays received data.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="data"></param>
	public void HandleSentData(DataContainer data)
	{
		string msg = Encoding.UTF8.GetString(data.Payload);
		Console.WriteLine("A message '{0}' was sent to client {1}.", data.ClientId, msg);
	}

	/// <summary>
	/// Displays a message server started.
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="config"></param>
	public void HandleServerStarted(ServerConfig config)
	{
		Console.WriteLine("Server started.");
	}

	/// <summary>
	/// Displays a message server failed to start
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="config"></param>
	public void HandleServerStartFailure(ServerConfig config)
	{
		Console.WriteLine("Server failed to start.");
	}

	/// <summary>
	/// Displays a message stopped
	/// </summary>
	/// <param name="senderObj"></param>
	/// <param name="config"></param>
	public void HandleServerStopped(ServerConfig config)
	{
		Console.WriteLine("Server stopped.");
	}

}
```

Multiple clients can be connected to one server. For this reason, events methods attributes object do include the identification value for each client connected. Therefore only one implementation of `IServerEventHandler` is allowed and it should handle multiple clients.

## Example program

Using the already implemented message data processor and implemented event handlers it is not that difficult to build up a demo client/server program. The code below implements a simple message exchange server and client. If the program is run with argument `server`, it runs in server mode an accepts for localhost connections on port 1234. Otherwise it runs in client mode and tries to connect to the server. There can be multiple clients connected to one server.

```cs
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
```
