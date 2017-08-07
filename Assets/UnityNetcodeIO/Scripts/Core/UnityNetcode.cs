#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_WEBGL_PLUGIN
#endif

namespace UnityNetcodeIO
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	using System;
	using System.Runtime.InteropServices;
	using AOT;

	using UnityNetcodeIO.Internal;
	using NetcodeIO.NET;

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
	/// Main manager for Unity Netcode.IO API
	/// </summary>
	public class UnityNetcode : MonoBehaviour
	{
		/// <summary>
		/// An instance of the Netcode.IO manager
		/// </summary>
		public static UnityNetcode Instance;

		#region Protected static methods

		// singleton bootstrapper so Netcode.IO manager is always available
		[RuntimeInitializeOnLoadMethod]
		protected static void Init()
		{
			// when the game starts, create a new instance of the netcode.io manager
			Instance = new GameObject("__netcode_io_mgr").AddComponent<UnityNetcode>();

			// set it to persist across scenes
			DontDestroyOnLoad(Instance.gameObject);

			// hide game object
			Instance.gameObject.hideFlags = HideFlags.HideInHierarchy;
		}

		// callback for when packets are received over the network
		[MonoPInvokeCallback(typeof(Action<int, int, IntPtr, int>))]
		protected static void handleClientMessage(int clientHandle, int clientID, IntPtr packetBufferPtr, int packetBufferLength)
		{
#if USE_WEBGL_PLUGIN
			unsafe
			{
				byte* byteBufferPtr = (byte*)packetBufferPtr.ToPointer();

				// grab a byte list off of the pool and copy the bytes over
				ByteBuffer byteArray = BufferPool.GetBuffer(packetBufferLength);
				byteArray.MemoryCopy(byteBufferPtr, 0, packetBufferLength);

				// create packet struct
				NetcodePacket packet = new NetcodePacket();
				packet.ClientID = clientID;
				packet.PacketBuffer = byteArray;

				// route packet to client by client handle
				var client = clients[clientHandle];
				client.ReceivePacket(packet);
			}
#endif
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

		protected static List<Client> internal_clients = new List<Client>();
		protected static List<Server> internal_servers = new List<Server>();

		// pending callbacks for creating clients
		protected static Dictionary<int, Action<NetcodeClient>> pendingClientCreatedCallbacks = new Dictionary<int, Action<NetcodeClient>>();

		protected static int clientHandle = 0;
		protected static int serverHandle = 0;

		#endregion

		#region Public Methods

		/// <summary>
		/// Query for Netcode.IO support
		/// </summary>
		public static void QuerySupport(System.Action<NetcodeIOSupportStatus> callback)
		{
#if USE_WEBGL_PLUGIN
			Instance.StartCoroutine(Instance.doQuerySupport(callback));
#else
			callback(NetcodeIOSupportStatus.Available);
#endif
		}

		/// <summary>
		/// Create a new Netcode.IO server
		/// </summary>
		/// <param name="ip">Public IP address clients will connect to</param>
		/// <param name="port">Port clients will connect to</param>
		/// <param name="protocolID">Protocol ID for this app (must be same as connect token server)</param>
		/// <param name="maxClients">Maximum clients which can connect</param>
		/// <param name="privateKey">The private key (shared by your game servers and token servers/backend)</param>
		public static NetcodeServer CreateServer(string ip, int port, ulong protocolID, int maxClients, byte[] privateKey)
		{
#if USE_WEBGL_PLUGIN
			throw new NotImplementedException();
#else

			NetcodeServer serverObj = new GameObject("__netcode_server_" + (serverHandle++)).AddComponent<NetcodeServer>();
			serverObj.transform.SetParent(Instance.transform);

			Server server = new Server(maxClients, ip, port, protocolID, privateKey);
			serverObj.internalServer = server;
			internal_servers.Add(server);

			server.OnClientConnected += serverObj.ClientConnected;
			server.OnClientDisconnected += serverObj.ClientDisconnected;
			server.OnClientMessageReceived += serverObj.ReceivePacket;

			return serverObj;

#endif
		}

		/// <summary>
		/// Creates a new Netcode.IO client, with a callback for when the client is created
		/// </summary>
		public static void CreateClient(NetcodeIOClientProtocol protocol, Action<NetcodeClient> clientCreatedCallback)
		{
#if USE_WEBGL_PLUGIN
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
#else
			NetcodeClient clientObj = new GameObject("__netcode_client_" + (clientHandle++)).AddComponent<NetcodeClient>();
			clientObj.transform.SetParent(Instance.transform);

			Client client = new Client();
			clientObj.internalClient = client;
			internal_clients.Add(client);

			clientCreatedCallback(clientObj);
#endif
		}

		/// <summary>
		/// Destroys a netcode client
		/// </summary>
		public static void DestroyClient(NetcodeClient client)
		{
			clients.Remove(client.Handle);
			client.Dispose();
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
#if USE_WEBGL_PLUGIN
			// initialize the plugin wrapper
			NetcodePluginWrapper.netcodeio_init();
#endif
		}

		protected void OnDestroy()
		{
			foreach (Client client in internal_clients)
				client.Disconnect();

			foreach (Server server in internal_servers)
				server.Stop();

			internal_servers.Clear();
			internal_clients.Clear();
		}

		#endregion

		#region Netcode Callback Handlers

		// called when status is received from netcode.io bindings
		protected void OnNetcodeIOStatusAvailable(int status)
		{
			UnityNetcode.status = (NetcodeIOSupportStatus)status;
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