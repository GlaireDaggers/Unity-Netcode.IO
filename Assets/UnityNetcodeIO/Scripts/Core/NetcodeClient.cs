namespace UnityNetcodeIO
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	using UnityNetcodeIO.Internal;

	/// <summary>
	/// Event handler for incoming network messages
	/// </summary>
	public class NetcodeMessageEvent : UnityEvent<NetcodeClient, NetcodePacket> { }

	/// <summary>
	/// Status of a netcode.io client
	/// </summary>
	public enum NetcodeClientStatus
	{
		Disconnected,
		Connected,

		SendingConnectionRequest,
		SendingConnectionResponse,

		ConnectionDenied,
		ConnectionRequestTimeout,
		ConnectionResponseTimeout,
		ConnectionTimedOut,
		ConnectTokenExpired,
		InvalidConnectToken,
	}

	/// <summary>
	/// Represents a Netcode.IO client
	/// </summary>
	public class NetcodeClient : MonoBehaviour
	{
		#region Public Fields

		[System.NonSerialized]
		public int Handle = -1;

		[System.NonSerialized]
		public NetcodeMessageEvent NetworkMessageEvent = new NetcodeMessageEvent();

		#endregion

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
		protected Queue<NetcodePacket> packetQueue = new Queue<NetcodePacket>();
		protected NetcodeClientStatus clientStatus = NetcodeClientStatus.Disconnected;
		
		// state stuff.
		protected bool isConnecting = false;
		protected System.Action pendingClientConnectCallback;
		protected System.Action<string> pendingClientConnectFailedCallback;

		#endregion

		#region Internal Methods

		// Called when this client receives a packet
		internal void ReceivePacket(NetcodePacket packet)
		{
			// push packet onto the queue
			lock (packetQueue)
			{
				packetQueue.Enqueue(packet);
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Set this client's tickrate
		/// </summary>
		public void SetTickrate( int tickrate)
		{
			NetcodePluginWrapper.netcodeio_clientSetTickRate(this.Handle, tickrate);
		}

		/// <summary>
		/// Query for this client's status
		/// </summary>
		public void QueryStatus( System.Action<NetcodeClientStatus> callback )
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

		#endregion

		#region Coroutines

		protected bool isQueryingStatus = false;
		protected IEnumerator doQueryStatus( System.Action<NetcodeClientStatus> callback )
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
		protected void OnNetcodeIOClientStatusAvailable(string status)
		{
			isQueryingStatus = false;

			switch (status)
			{
				case "connected":
					this.clientStatus = NetcodeClientStatus.Connected;
					break;
				case "connectionDenied":
					this.clientStatus = NetcodeClientStatus.ConnectionDenied;
					break;
				case "connectionRequestTimeout":
					this.clientStatus = NetcodeClientStatus.ConnectionRequestTimeout;
					break;
				case "connectionResponseTimeout":
					this.clientStatus = NetcodeClientStatus.ConnectionResponseTimeout;
					break;
				case "connectionTimedOut":
					this.clientStatus = NetcodeClientStatus.ConnectionTimedOut;
					break;
				case "connectTokenExpired":
					this.clientStatus = NetcodeClientStatus.ConnectTokenExpired;
					break;
				case "disconnected":
					this.clientStatus = NetcodeClientStatus.Disconnected;
					break;
				case "invalidConnectToken":
					this.clientStatus = NetcodeClientStatus.InvalidConnectToken;
					break;
				case "sendingConnectionRequest":
					this.clientStatus = NetcodeClientStatus.SendingConnectionRequest;
					break;
				case "sendingConnectionResponse":
					this.clientStatus = NetcodeClientStatus.SendingConnectionResponse;
					break;
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
		}

		#endregion
	}
}