# Unity-Netcode.IO
A lightweight and easy-to-use plugin to allow Unity games to take advantage of the [Netcode.IO](https://github.com/networkprotocol/netcode.io) protocol for secure UDP communication.

# Netcode.IO.NET
Unity-Netcode.IO makes use of [Netcode.IO.NET](https://github.com/KillaMaaki/Netcode.IO.NET) to provide support on most platforms except for WebGL (handled via JS plugin). Because of this, the full Netcode.IO.NET API is available to you under the `NetcodeIO.NET` namespace.

# Usage
All API functions are in the `UnityNetcodeIO` namespace.
First, query for Netcode.IO support with `UnityNetcode.QuerySupport`:

```c#
// check for Netcode.IO extension
// Will provide NetcodeIOSupportStatus enum, either:
// Available, if Netcode.IO is available and the standalone helper is installed (or if in standalone),
// Unavailable, if Netcode.IO is unsupported (direct user to install extension)
// HelperNotInstalled, if Netcode.IO is available but the standalone helper is not installed (direct user to install the standalone helper)
UnityNetcode.QuerySupport( (supportStatus) =>
{
} );
```

Next, create a client using `UnityNetcode.CreateClient`:

```c#
// create a Netcode.IO client using the given protocol
// Protocol is either NetcodeIOClientProtocol.IPv4 or NetcodeIOClientProtocol.IPv6
UnityNetcode.CreateClient( protocol, (client)=>
{
} );
```

Assuming you have a byte[] connect token and a client created, you can connect to a server using `NetcodeClient.Connect`:

```c#
client.Connect( connectToken, () =>
{
	// client connected!
}, ( err ) =>
{
	// client failed to connect, err contains error message
} );
```

You can query the status of a client using `NetcodeClient.QueryStatus`:

```c#
client.QueryStatus( (status)=>
{
} );
```

You can add a listener for when packets are received using `NetcodeClient.AddPayloadListener`:

```c#
client.AddPayloadListener( (clientReceiver, packet) =>
{
	// clientReceiver is the client receiving the packet
	// packet contains client ID (as originally issued by token server) and ByteBuffer of packet payload
	// note that the payload will be returned to a pool after this handler runs, so do not keep a reference to it!
} );
```

You can send packets to the server using `NetcodeClient.Send`:

```c#
byte[] data;
// ...
client.Send( data );	// data must be between 1 and 1200 bytes
```

You can set a client's tickrate using `NetcodeClient.SetTickrate`:

```c#
client.SetTickrate( ticksPerSecond );
```

And finally, you can destroy a client using `UnityNetcode.DestroyClient`:

```c#
// disconnects and destroys the client. Note that the client cannot be reused after this!
UnityNetcode.DestroyClient( client );
```

## Server API
The server API relies on some classes under the `NetcodeIO.NET` namespace, so be sure to include it with any code using the server API.
Note that the Server API is not compatible with WebGL - attempting to create a server will throw a `NotImplementedException`. All other platforms may use it, however.

To create a new server, use:
```c#
var server = UnityNetcode.CreateServer(
	ip,		// string public IP clients will connect to
	port,		// port clients will connect to
	maxClients,	// maximum number of clients who can connect
	privateKey );	// byte[32] private encryption key shared between token server and game server
```

To listen to the server's events:
```c#
// Called when a client connects to the server
server.ClientConnectedEvent.AddListener( callback );	// void( RemoteClient client );

// Called when a client disconnects from the server
server.ClientDisconnectedEvent.AddListener( callback );	// void( RemoteClient client );

// Called when a client sends a payload to the server
// Note that byte[] payload will be returned to a pool after the callback, so don't keep a reference to it.
server.ClientMessageEvent.AddListener( callback );	// void( RemoteClient sender, ByteBuffer payload );
```

To start and stop the server, use:
```c#
server.StartServer();	// start listening for clients
server.StopServer();	// stop server and disconnect any clients
```

To send a payload to a client, use:
```c#
server.SendPayload( remoteClient, ByteBuffer payload );	// payload must be between 1 and 1200 bytes.
```

To disconnect a client, use:
```c#
server.Disconnect( remoteClient );
```

To dispose of a server, use:
```c#
server.Dispose();
```

# ByteBuffer
Note that the client and server APIs both make use of a `ByteBuffer` class. This is a helper class which provides the functionality of a resizable byte array, with some extra helper methods on top.
You may use a ByteBuffer as if it were an array - access bytes using `buffer[index]` and check its size using `buffer.Length`.
Additionally, you can copy data from other byte arrays using `buffer.BufferCopy( sourceArray, sourceIndex, destinationIndex, bytesToCopy )`, or other ByteBuffers in another overload of the same function.

You can also use the following variations of the `ByteBuffer.MemoryCopy` function as an alternative:

```c#
// Uses unsafe code to copy bytes from a source array into the ByteBuffer. Does not do bounds checking!
buffer.MemoryCopy( byte[] source, int src, int dest, int length );

// Uses unsafe code to copy bytes from a source pointer into the ByteBuffer.
buffer.MemoryCopy( byte* source, int dest, int length );
```

Additionally, you can use the `BufferPool` class to allocate and release buffers in a memory-friendly way:

```c#
// to retrieve a buffer from the pool...
var buffer = BufferPool.GetBuffer( numBytes );

// and to return a buffer to the pool.
BufferPool.ReturnBuffer( buffer );
```

# Platforms
UnityNetcode.IO runs on all platforms which support raw socket communication, as well as WebGL with the use of a wrapper around this [browser extension](https://github.com/RedpointGames/netcode.io-browser) which brings Netcode.IO support to the browser.
