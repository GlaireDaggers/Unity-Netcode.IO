# Unity-Netcode.IO
A lightweight plugin to allow WebGL Unity games to use Netcode.IO for UDP socket communication.

This plugin wraps around the JavaScript API for this Netcode.IO browser extension: https://github.com/RedpointGames/netcode.io-browser and exposes it to Unity in a simple and easy-to-use API.

# Usage
All API functions are in the UnityNetcodeIO namespace.
First, query for Netcode.IO support in the user's browser with `NetcodeIO.QuerySupport`:

```c#
// check for Netcode.IO extension
// Will provide NetcodeIOSupportStatus enum, either:
// Available, if Netcode.IO is available and the standalone helper is installed,
// Unavailable, if Netcode.IO is unsupported (direct user to install extension)
// HelperNotInstalled, if Netcode.IO is available but the standalone helper is not installed (direct user to install the standalone helper)
NetcodeIO.QuerySupport( (supportStatus) =>
{
} );
```

Next, create a client using `NetcodeIO.CreateClient`:

```c#
// create a Netcode.IO client using the given protocol
// Protocol is either NetcodeIOClientProtocol.IPv4 or NetcodeIOClientProtocol.IPv6
NetcodeIO.CreateClient( protocol, (client)=>
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

You can add a listener for when packets are received using `NetcodeClient.NetworkMessageEvent.AddListener`:

```c#
client.NetworkMessageEvent.AddListener( (clientReceiver, packet) =>
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

And finally, you can destroy a client using `NetcodeIO.DestroyClient`:

```c#
// disconnects and destroys the client. Note that the client cannot be reused after this!
NetcodeIO.DestroyClient( client );
```

# Platforms
Currently only WebGL is supported. All other platforms will indicate no support when querying for Netcode.IO support.
However, hopefully in the not-too-distant future this will change, as I'm also working on a pure managed C# implementation of the Netcode.IO protocol with some promising initial results.
