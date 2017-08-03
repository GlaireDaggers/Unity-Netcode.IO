namespace UnityNetcodeIO
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	using System;
	using System.Runtime.InteropServices;
	using AOT;

	using UnityNetcodeIO.Internal;

	/// <summary>
	/// Status code for Netcode.IO API
	/// </summary>
	public enum NetcodeIOSupportStatus
	{
		/// <summary>
		/// Netcode.IO is available and ready
		/// </summary>
		Available = 0,

		/// <summary>
		/// Netcode.IO is not supported
		/// </summary>
		Unavailable = 1,

		/// <summary>
		/// Netcode.IO is available, but the standalone helper is not installed
		/// </summary>
		HelperNotInstalled = 2,

		/// <summary>
		/// Status has not been queried or query has not completed
		/// </summary>
		Unknown = 3
	}

	/// <summary>
	/// Client protocol types
	/// </summary>
	public enum NetcodeIOClientProtocol
	{
		IPv4,
		IPv6,
	}

	/// <summary>
	/// Main manager for Netcode.IO
	/// </summary>
	public class NetcodeIO : MonoBehaviour
	{
		/// <summary>
		/// An instance of the Netcode.IO manager
		/// </summary>
		public static NetcodeIO Instance;

		#region Protected static methods

		// singleton bootstrapper so Netcode.IO manager is always available
		[RuntimeInitializeOnLoadMethod]
		protected static void Init()
		{
			// when the game starts, create a new instance of the netcode.io manager
			Instance = new GameObject("__netcode_io_mgr").AddComponent<NetcodeIO>();

			// set it to persist across scenes
			DontDestroyOnLoad(Instance.gameObject);

			// hide game object
			Instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
		}

		// callback for when packets are received over the network
		[MonoPInvokeCallback(typeof(Action<int, int, IntPtr, int>))]
		protected static void handleClientMessage(int clientHandle, int clientID, IntPtr packetBufferPtr, int packetBufferLength)
		{
			unsafe
			{
				byte* byteBufferPtr = (byte*)packetBufferPtr.ToPointer();

				// grab a byte list off of the pool and copy the bytes over
				List<byte> byteArray = ListPool<byte>.GetList(packetBufferLength);
				for (int i = 0; i < packetBufferLength; i++)
					byteArray.Add(*byteBufferPtr++);

				// create packet struct
				NetcodePacket packet = new NetcodePacket();
				packet.ClientID = clientID;
				packet.PacketBuffer = byteArray;

				// route packet to client by client handle
				var client = clients[clientHandle];
				client.ReceivePacket(packet);
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Last retrieved support status of Netcode.IO API
		/// </summary>
		public static NetcodeIOSupportStatus Status
		{
			get { return status; }
		}

		#endregion

		#region Protected fields

		protected static Dictionary<int, NetcodeClient> clients = new Dictionary<int, NetcodeClient>();
		protected static NetcodeIOSupportStatus status = NetcodeIOSupportStatus.Unknown;

		// pending callbacks for creating clients
		protected static Dictionary<int, Action<NetcodeClient>> pendingClientCreatedCallbacks = new Dictionary<int, Action<NetcodeClient>>();

		#endregion

		#region Public Methods

		/// <summary>
		/// Query for Netcode.IO support
		/// </summary>
		public static void QuerySupport( System.Action<NetcodeIOSupportStatus> callback )
		{
			Instance.StartCoroutine(Instance.doQuerySupport(callback));
		}

		/// <summary>
		/// Creates a new Netcode.IO client, with a callback for when the client is created
		/// </summary>
		public static void CreateClient(NetcodeIOClientProtocol protocol, Action<NetcodeClient> clientCreatedCallback)
		{
			string protocolStr = "";
			switch (protocol)
			{
				case NetcodeIOClientProtocol.IPv4:
					protocolStr = "ipv4";
					break;
				case NetcodeIOClientProtocol.IPv6:
					protocolStr = "ipv6";
					break;
			}

			int newHandle = NetcodePluginWrapper.netcodeio_createClient(protocolStr, Instance.gameObject.name);
			pendingClientCreatedCallbacks.Add(newHandle, clientCreatedCallback);
		}

		/// <summary>
		/// Destroys a netcode client
		/// </summary>
		public static void DestroyClient(NetcodeClient client)
		{
			NetcodePluginWrapper.netcodeio_destroyClient(client.Handle);
			clients.Remove(client.Handle);
			Destroy(client.gameObject);
		}

		#endregion

		#region Coroutines

		protected IEnumerator doQuerySupport(System.Action<NetcodeIOSupportStatus> callback)
		{
			NetcodePluginWrapper.netcodeio_query_support(Instance.gameObject.name);
			while (status == NetcodeIOSupportStatus.Unknown) yield return null;
			callback(status);
		}

		#endregion

		#region Unity Methods

		protected void Awake()
		{
			// initialize the plugin wrapper
			NetcodePluginWrapper.netcodeio_init();
		}

		#endregion

		#region Netcode Callback Handlers

		// called when status is received from netcode.io bindings
		protected void OnNetcodeIOStatusAvailable(int status)
		{
			NetcodeIO.status = (NetcodeIOSupportStatus)status;
		}

		// called when a netcode.io client is created
		protected void OnNetcodeIOClientCreated(int handle)
		{
			// create the NetcodeClient object
			NetcodeClient client = new GameObject("__netcode_client_" + handle).AddComponent<NetcodeClient>();
			client.transform.SetParent(transform);

			// assign handle and store client
			client.Handle = handle;
			clients.Add(handle, client);

			// add callback
			NetcodePluginWrapper.netcodeio_clientAddListener(handle, handleClientMessage);

			// pass new client object to callback
			pendingClientCreatedCallbacks[handle](client);
			pendingClientCreatedCallbacks.Remove(handle);
		}

		#endregion
	}
}