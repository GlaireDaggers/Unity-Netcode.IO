# Unity-Netcode.IO
A lightweight and easy-to-use plugin to allow Unity games to take advantage of the [Netcode.IO](https://github.com/networkprotocol/netcode.io) protocol for secure UDP communication.

# Usage
All API functions are in the UnityNetcodeIO namespace.
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
	// packet contains client ID (as originally issued by token server) and List<byte> of packet payload
	// note that the payload will be returned to a pool after this handler runs, so do not keep a reference to it!
} );
```

You can send packets to the server using `NetcodeClient.Send`:

```c#
byte[] data;
// ...
client.Send( data );
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

# Platforms
UnityNetcode.IO runs on all platforms which support raw socket communication, as well as WebGL with the use of a wrapper around this [browser extension](https://github.com/RedpointGames/netcode.io-browser) which brings Netcode.IO support to the browser.
