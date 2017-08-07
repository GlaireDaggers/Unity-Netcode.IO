#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_WEBGL_PLUGIN
#endif

namespace UnityNetcodeIO
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	using UnityNetcodeIO.Internal;
	using NetcodeIO.NET;

	/// <summary>
	/// Event handler for incoming network messages
	/// </summary>
	public class NetcodeMessageEvent : UnityEvent<NetcodeClient, NetcodePacket> { }

	/// <summary>
	/// Status of a netcode.io client
	/// </summary>
	public enum NetcodeClientStatus
	{
		/// <summary>
		/// The client is not connected to a server
		/// </summary>
		Disconnected = 0,

		/// <summary>
		/// The client has connected to a server
		/// </summary>
		Connected = 3,

		/// <summary>
		/// The client is currently sending a connection request
		/// </summary>
		SendingConnectionRequest = 1,

		/// <summary>
		/// The client is currently sending a challenge response
		/// </summary>
		SendingConnectionResponse = 2,

		/// <summary>
		/// The client's connection request was denied
		/// </summary>
		ConnectionDenied = -1,

		/// <summary>
		/// The server did not respond to the client's connection request
		/// </summary>
		ConnectionRequestTimeout = -2,

		/// <summary>
		/// The server did not respond to the client's challenge response
		/// </summary>
		ConnectionResponseTimeout = -3,

		/// <summary>
		/// The client has not received any messages from the server within timeout period
		/// </summary>
		ConnectionTimedOut = -4,

		/// <summary>
		/// The connect token is invalid
		/// </summary>
		InvalidConnectToken = -5,

		/// <summary>
		/// The connect token has expired
		/// </summary>
		ConnectTokenExpired = -6,
	}

	// shared stuff
	public partial class NetcodeClient : MonoBehaviour
	{
		#region Public Fields

		[System.NonSerialized]
		public NetcodeMessageEvent NetworkMessageEvent = new NetcodeMessageEvent();

		#endregion

		#region Public Properties

		public int Handle { get; set; }

		#endregion

		#region Protected fields

		// packet queue for thread safety
		protected Queue<NetcodePacket> packetQueue = new Queue<NetcodePacket>();
		protected Queue<System.Action> callbackQueue = new Queue<System.Action>();

		#endregion

		#region Public methods

		public void AddPayloadListener(UnityAction<NetcodeClient, NetcodePacket> listener)
		{
			NetworkMessageEvent.AddListener(listener);
		}

		public void ReceivePacket(NetcodePacket packet)
		{
			// push packet onto the queue
			lock (packetQueue)
			{
				packetQueue.Enqueue(packet);
			}
		}

		#endregion

		#region Unity Methods

		protected void Update()
		{
			// process packets in the queue
			lock (packetQueue)
			{
				while (packetQueue.Count > 0)
				{
					// note: packet payload is returned to pool after user callbacks
					// user callbacks should not keep any references to packet payload

					var packet = packetQueue.Dequeue();
					NetworkMessageEvent.Invoke(this, packet);
					packet.Release();
				}
			}

			lock (callbackQueue)
			{
				while (callbackQueue.Count > 0)
				{
					callbackQueue.Dequeue()();
				}
			}
		}

		#endregion

		#region Private Methods

		private void pushCallback(System.Action callback)
		{
			lock (callbackQueue)
				callbackQueue.Enqueue(callback);
		}

		#endregion
	}

#if USE_WEBGL_PLUGIN

	public partial class NetcodeClient : MonoBehaviour
	{
	#region Public Properties

		/// <summary>
		/// The last known status of this client
		/// </summary>
		public NetcodeClientStatus Status
		{
			get { return clientStatus; }
		}

	#endregion

	#region Protected Fields

		// packet queue for thread safety
		protected NetcodeClientStatus clientStatus = NetcodeClientStatus.Disconnected;

		// state stuff.
		protected bool isConnecting = false;
		protected System.Action pendingClientConnectCallback;
		protected System.Action<string> pendingClientConnectFailedCallback;

	#endregion

	#region Public Methods

		public void Dispose()
		{
			NetcodePluginWrapper.netcodeio_destroyClient(Handle);
			Destroy(gameObject);
		}

		/// <summary>
		/// Set this client's tickrate
		/// </summary>
		public void SetTickrate(int tickrate)
		{
			NetcodePluginWrapper.netcodeio_clientSetTickRate(this.Handle, tickrate);
		}

		/// <summary>
		/// Query for this client's status
		/// </summary>
		public void QueryStatus(System.Action<NetcodeClientStatus> callback)
		{
			StartCoroutine(doQueryStatus(callback));
		}

		/// <summary>
		/// Connect to a netcode.io server using the connection token
		/// </summary>
		public void Connect(byte[] connectToken, System.Action clientConnected, System.Action<string> connectFailedCallback)
		{
			if (isConnecting)
				throw new System.InvalidOperationException("Client is already attempting to connect!");

			isConnecting = true;
			this.pendingClientConnectCallback = clientConnected;
			this.pendingClientConnectFailedCallback = connectFailedCallback;
			NetcodePluginWrapper.netcodeio_connectClient(this.Handle, connectToken, connectToken.Length, gameObject.name);
		}

		/// <summary>
		/// Sends data to the netcode.io server
		/// </summary>
		public void Send(byte[] data)
		{
			NetcodePluginWrapper.netcodeio_clientSend(this.Handle, data, data.Length, gameObject.name);
		}

		/// <summary>
		/// Sends data to the netcode.io server
		/// </summary>
		public void Send(byte[] data, int length)
		{
			NetcodePluginWrapper.netcodeio_clientSend(this.Handle, data, length, gameObject.name);
		}

	#endregion

	#region Coroutines

		protected bool isQueryingStatus = false;
		protected IEnumerator doQueryStatus(System.Action<NetcodeClientStatus> callback)
		{
			isQueryingStatus = true;
			NetcodePluginWrapper.netcodeio_getClientState(this.Handle, gameObject.name);

			while (isQueryingStatus) yield return null;

			callback(this.Status);
		}

	#endregion

	#region Callback Handlers

		// called when sending a message fails
		protected void OnNetcodeIOClientSendFailed(string err)
		{
			Debug.LogError("Client failed to send message: " + err);
		}

		// called when a connection fails
		protected void OnNetcodeIOClientConnectFailed(string err)
		{
			isConnecting = false;
			pendingClientConnectFailedCallback(err);
		}

		// called when the client connects
		protected void OnNetcodeIOClientConnected()
		{
			isConnecting = false;
			pendingClientConnectCallback();
		}

		// called when client status is available
		protected void OnNetcodeIOClientStatusAvailable(int statusCode)
		{
			isQueryingStatus = false;
			this.clientStatus = (NetcodeClientStatus)statusCode;
		}

	#endregion
	}

#else

	public partial class NetcodeClient : MonoBehaviour
	{
		#region Public Properties

		/// <summary>
		/// The last known status of this client
		/// </summary>
		public NetcodeClientStatus Status
		{
			get { return (NetcodeClientStatus)internalClient.State; }
		}

		#endregion

		#region Protected Fields

		internal Client internalClient;

		#endregion

		#region Public Methods

		public void Dispose()
		{
			internalClient.Disconnect();
			Destroy(gameObject);
		}

		/// <summary>
		/// Set this client's tickrate
		/// </summary>
		public void SetTickrate(int tickrate)
		{
			internalClient.Tickrate = tickrate;
		}

		/// <summary>
		/// Query for this client's status
		/// </summary>
		public void QueryStatus(System.Action<NetcodeClientStatus> callback)
		{
			callback((NetcodeClientStatus)internalClient.State);
		}

		/// <summary>
		/// Connect to a netcode.io server using the connection token
		/// </summary>
		public void Connect(byte[] connectToken, System.Action clientConnected, System.Action<string> connectFailedCallback)
		{
			internalClient.OnStateChanged += (state) =>
			{
				Debug.Log("State changed to: " + (int)state);

				if (state == ClientState.Connected)
					pushCallback(clientConnected);
				else if ((int)state < 0)
					pushCallback(() => { connectFailedCallback("Failed to connect: " + state.ToString()); });
			};

			internalClient.OnMessageReceived += (payload, length) =>
			{
				var packet = new NetcodePacket();
				packet.ClientID = internalClient.ClientIndex;

				packet.PacketBuffer = BufferPool.GetBuffer(length);
				packet.PacketBuffer.BufferCopy(payload, 0, 0, length);

				ReceivePacket(packet);
			};

			internalClient.Connect(connectToken);
		}

		/// <summary>
		/// Sends data to the netcode.io server
		/// </summary>
		public void Send(byte[] data)
		{
			internalClient.Send(data, data.Length);
		}

		/// <summary>
		/// Sends data to the netcode.io server
		/// </summary>
		public void Send(byte[] data, int length)
		{
			internalClient.Send(data, length);
		}

		#endregion
	}

#endif
}