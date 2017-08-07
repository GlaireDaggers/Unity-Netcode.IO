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
	public class NetcodeRemoteClientMessageEvent : UnityEvent<RemoteClient, ByteBuffer> { }

	/// <summary>
	/// A netcode.io server
	/// </summary>
	public class NetcodeServer : MonoBehaviour
	{
		private struct serverReceivedPacket
		{
			public RemoteClient sender;
			public ByteBuffer packetBuffer;
		}

		internal Server internalServer;
		private Queue<serverReceivedPacket> packetQueue = new Queue<serverReceivedPacket>();
		private Queue<RemoteClient> connectedQueue = new Queue<RemoteClient>();
		private Queue<RemoteClient> disconnectQueue = new Queue<RemoteClient>();

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
			pushPacket(sender, payload, payloadLength);
		}

		internal void ClientConnected(RemoteClient client)
		{
			lock (connectedQueue)
				connectedQueue.Enqueue(client);
		}

		internal void ClientDisconnected(RemoteClient client)
		{
			lock (disconnectQueue)
				disconnectQueue.Enqueue(client);
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

		public void SendPayload(RemoteClient client, ByteBuffer payload)
		{
			internalServer.SendPayload(client, payload.InternalBuffer, payload.Length);
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

		protected void pushPacket(RemoteClient sender, byte[] payload, int size)
		{
			ByteBuffer buffer = BufferPool.GetBuffer(size);
			buffer.BufferCopy(payload, 0, 0, size);

			serverReceivedPacket packet = new serverReceivedPacket()
			{
				sender = sender,
				packetBuffer = buffer
			};

			lock (packetQueue)
				packetQueue.Enqueue(packet);
		}

		#endregion

		#region Unity Methods

		private void Update()
		{
			lock (packetQueue)
			{
				while (packetQueue.Count > 0)
				{
					var packet = packetQueue.Dequeue();
					ClientMessageEvent.Invoke(packet.sender, packet.packetBuffer);
					BufferPool.ReturnBuffer(packet.packetBuffer);
				}
			}

			lock (connectedQueue)
			{
				while (connectedQueue.Count > 0)
				{
					ClientConnectedEvent.Invoke(connectedQueue.Dequeue());
				}
			}

			lock (disconnectQueue)
			{
				while (disconnectQueue.Count > 0)
				{
					ClientDisconnectedEvent.Invoke(disconnectQueue.Dequeue());
				}
			}
		}

		#endregion
	}
}