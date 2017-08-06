#if WEBGL && !UNITY_EDITOR
#define USE_WEBGL_PLUGIN
#endif

namespace UnityNetcodeIO
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	using UnityNetcodeIO.Internal;
	using NetcodeIO.NET;

	/// <summary>
	/// Event handler for remote clients connecting
	/// </summary>
	public class NetcodeRemoteClientConnectedEvent : UnityEvent<RemoteClient> { }

	/// <summary>
	/// Event handler for remote clients disconnecting
	/// </summary>
	public class NetcodeRemoteClientDisconnectedEvent : UnityEvent<RemoteClient> { }

	/// <summary>
	/// Event handler for remote clients sending payloads
	/// </summary>
	public class NetcodeRemoteClientMessageEvent : UnityEvent<RemoteClient, byte[], int> { }

	/// <summary>
	/// A netcode.io server
	/// </summary>
	public class NetcodeServer : MonoBehaviour
	{
		internal Server internalServer;
		protected Queue<Action> callbackQueue = new Queue<Action>();

		#region Public fields

		[System.NonSerialized]
		internal NetcodeRemoteClientConnectedEvent ClientConnectedEvent = new NetcodeRemoteClientConnectedEvent();

		[System.NonSerialized]
		public NetcodeRemoteClientDisconnectedEvent ClientDisconnectedEvent = new NetcodeRemoteClientDisconnectedEvent();

		[System.NonSerialized]
		public NetcodeRemoteClientMessageEvent ClientMessageEvent = new NetcodeRemoteClientMessageEvent();

		#endregion

		#region Internal methods

		internal void ReceivePacket(RemoteClient sender, byte[] payload, int payloadLength)
		{
			pushCallback(() =>
			{
				ClientMessageEvent.Invoke(sender, payload, payloadLength);
			});
		}

		internal void ClientConnected(RemoteClient client)
		{
			pushCallback(() =>
			{
				ClientConnectedEvent.Invoke(client);
			});
		}

		internal void ClientDisconnected(RemoteClient client)
		{
			pushCallback(() =>
			{
				ClientDisconnectedEvent.Invoke(client);
			});
		}

		#endregion

		#region Public Methods

		public void Dispose()
		{
			StopServer();

			if (this != null && gameObject != null)
				Destroy(gameObject);
		}

		public void StartServer()
		{
			internalServer.Start();
		}

		public void SendPayload(RemoteClient client, byte[] payload, int payloadLength)
		{
			internalServer.SendPayload(client, payload, payloadLength);
		}

		public void Disconnect(RemoteClient client)
		{
			internalServer.Disconnect(client);
		}

		public void StopServer()
		{
			internalServer.Stop();
		}

		#endregion

		#region Protected methods

		protected void pushCallback(Action callback)
		{
			lock (callbackQueue)
				callbackQueue.Enqueue(callback);
		}

		#endregion

		#region Unity Methods

		private void Update()
		{
			lock (callbackQueue)
			{
				while (callbackQueue.Count > 0)
				{
					callbackQueue.Dequeue()();
				}
			}
		}

		#endregion
	}
}