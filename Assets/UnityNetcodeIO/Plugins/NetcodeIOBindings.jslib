var netcodelib = {

	netcodeio_init: function()
	{
		this.netcodeio_clients = {};
		this.netcodeio_nextClientID = 0;
		
		// malloc a temp buffer for received packets ahead of time
		// doing this so that we don't thrash the heap with mallocs every time a packet is received
		this.netcodeio_tempPacketBuffer = _malloc( 2048 );
	},
	
	// queries the support status of the netcode.io API, providing results to a Unity-side game object via OnNetcodeIOStatusAvailable( status )
	netcodeio_query_support: function(handlerObjectStr)
	{
		var handlerObject = Pointer_stringify( handlerObjectStr );
	
		if( !window.netcode )
		{
			console.log( "Netcode.IO not supported" );
		
			// the API is not present, return status 1
			SendMessage( handlerObject, "OnNetcodeIOStatusAvailable", 1 );
		}
		
		window.netcode.isNativeHelperInstalled( function( err, isPresent )
		{
			// something went wrong while querying for native helper.
			if( err != null )
				throw( err );
		
			if( !isPresent )
			{
				console.log( "Netcode.IO native helper not present" );
			
				// the native helper is not present, return status 2
				SendMessage( handlerObject, "OnNetcodeIOStatusAvailable", 2 );
			}
			else
			{
				console.log( "Netcode.IO available and ready!" );
				
				// the API is present and the helper is available!
				SendMessage( handlerObject, "OnNetcodeIOStatusAvailable", 0 );
			}
		} );
	},

	// create a new netcode.io client. The handle of the new client is returned, and is posted to handlerObject via OnNetcodeIOClientCreated( handle )
	netcodeio_createClient: function(protocolStr, handlerObjectStr)
	{
		var handlerObject = Pointer_stringify( handlerObjectStr );
		var protocol = Pointer_stringify( protocolStr );
		
		var newHandle = this.netcodeio_nextClientID++;
		
		window.netcode.createClient( protocol, function( err, client )
		{
			// something went wrong while creating the client
			if( err != null )
				throw( err );
				
			// register client+handle pair
			this.netcodeio_clients[ newHandle ] = client;
			
			// post client handle to Unity
			SendMessage( handlerObject, "OnNetcodeIOClientCreated", newHandle );
		}.bind(this) );
		
		return newHandle;
	},
	
	// destroy a created netcode.io client by handle
	netcodeio_destroyClient: function( handle )
	{
		// invalid handle, throw error
		if( !( handle in this.netcodeio_clients ) )
			throw new Error( "Client handle not valid - could not find handle" );
			
		// retrieve client by handle
		var client = this.netcodeio_clients[ handle ];
		
		// destroy client
		client.destroy( function( err )
		{
			// something went wrong while destroying the client
			if( err != null )
				throw( err );
		} );
		
		// remove the client from the dictionary
		delete this.netcodeio_clients[ handle ];
	},
	
	// connects client to a netcode.io server using the specified auth token
	// posts results back to handlerObject via OnNetcodeIOClientConnected(), or OnNetcodeIOClientConnectFailed( err )
	netcodeio_connectClient: function( handle, tokenptr, tokenlength, handlerObjectStr )
	{
		var handlerObject = Pointer_stringify( handlerObjectStr );
		
		// invalid handle, throw error
		if( !( handle in this.netcodeio_clients ) )
			throw new Error( "Client handle not valid - could not find handle" );
			
		// retrieve client by handle
		var client = this.netcodeio_clients[ handle ];
		
		// tokenptr is pointer to heap.
		// we create a new Uint8Array and copy heap data into it before connecting
		var token = new Uint8Array( tokenlength );
		for( var i = 0; i < tokenlength; i++ )
			token[i] = HEAPU8[ tokenptr + i ];
		
		// connect
		client.connect( token, function( err )
		{
			if( err != null )
				SendMessage( handlerObject, "OnNetcodeIOClientConnectFailed", err.message );
			else
				SendMessage( handlerObject, "OnNetcodeIOClientConnected" );
		} );
	},
	
	// set the client's tickrate
	netcodeio_clientSetTickRate: function( handle, tickrate )
	{
		// invalid handle, throw error
		if( !( handle in this.netcodeio_clients ) )
			throw new Error( "Client handle not valid - could not find handle" );
			
		// retrieve client by handle
		var client = this.netcodeio_clients[ handle ];
		
		client.setTickRate( tickrate, function(err)
		{
			if( err != null )
				throw( err );
		} );
	},
	
	// makes client send a message
	// if something goes wrong, posts error to handlerObject via OnNetcodeIOClientSendFailed( err )
	netcodeio_clientSend: function( handle, packetBufferPtr, packetBufferLength, handlerObjectStr )
	{
		var handlerObject = Pointer_stringify( handlerObjectStr );
		
		// invalid handle, throw error
		if( !( handle in this.netcodeio_clients ) )
			throw new Error( "Client handle not valid - could not find handle" );
			
		// retrieve client by handle
		var client = this.netcodeio_clients[ handle ];
		
		// we create a new Uint8Array and copy heap data into it before sending
		var packetBuffer = new Uint8Array( packetBufferLength );
		for( var i = 0; i < packetBufferLength; i++ )
			packetBuffer[i] = HEAPU8[ packetBufferPtr + i ];
		
		// send
		client.send( packetBuffer, function( err )
		{
			if( err != null )
				SendMessage( handlerObject, "OnNetcodeIOClientSendFailed", err.message );
		} );
	},
	
	// gets the state of the client, posted as string to handlerObject via OnNetcodeIOClientStatusAvailable( status )
	netcodeio_getClientState: function( handle, handlerObjectStr )
	{
		var handlerObject = Pointer_stringify( handlerObjectStr );
		
		// invalid handle, throw error
		if( !( handle in this.netcodeio_clients ) )
			throw new Error( "Client handle not valid - could not find handle" );
			
		// retrieve client by handle
		var client = this.netcodeio_clients[ handle ];
		
		client.getClientState( function( err, status )
		{
			if( err != null )
				throw( err );
				
			var statusCode = 0;
			if( status == "connectTokenExpired" )
				statusCode = -6;
			else if( status == "invalidConnectToken" )
				statusCode = -5;
			else if( status == "connectionTimedOut" )
				statusCode = -4;
			else if( status == "challengeResponseTimedOut" )
				statusCode = -3;
			else if( status == "connectionRequestTimedOut" )
				statusCode = -2;
			else if( status == "connectionDenied" )
				statusCode = -1;
			else if( status == "disconnected" )
				statusCode = 0;
			else if( status == "sendingConnectionRequest" )
				statusCode = 1;
			else if( status == "sendingChallengeResponse" )
				statusCode = 2;
			else if( status == "connected" )
				statusCode = 3;
				
			SendMessage( handlerObject, "OnNetcodeIOClientStatusAvailable", statusCode );
		}.bind(this) );
	},
	
	// add a packet listener to the client, posted to (static!) listener callback (clientHandle, clientID, packetBuffer, packetBufferLength)
	// it has to be static so I can just directly call it instead of using SendMessage, which only lets me pass a single parameter either number or string.
	netcodeio_clientAddListener: function( handle, staticCallback )
	{
		// invalid handle, throw error
		if( !( handle in this.netcodeio_clients ) )
			throw new Error( "Client handle not valid - could not find handle" );
			
		// retrieve client by handle
		var client = this.netcodeio_clients[ handle ];
		
		// construct event listener function
		var listenerFunc = function( clientId, buffer )
		{
			// copy the data into our temp buffer which is allocated upfront
			var numBytes = buffer.byteLength;
			var heapPtr = new Uint8Array( Module.HEAPU8.buffer, this.netcodeio_tempPacketBuffer, numBytes );
			heapPtr.set( buffer );
			
			// then pass to C#
			Runtime.dynCall( 'viiii', staticCallback, [ handle, clientId, this.netcodeio_tempPacketBuffer, numBytes ] );
		}.bind(this);
		
		// add the event listener
		client.addEventListener( "receive", listenerFunc );
	},
	
};

// add to JS library
mergeInto(LibraryManager.library, netcodelib);