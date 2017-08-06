namespace UnityNetcodeIO.Internal
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;

	using UnityEngine;

#if !UNITY_WEBGL || UNITY_EDITOR
	using NetcodeIO.NET;
#endif

	/// <summary>
	/// Wrapper around Netcode.IO API
	/// </summary>
	public class NetcodePluginWrapper
	{
		/// <summary>
		/// Protocol ID shared between client and servers
		/// </summary>
		public static ulong ProtocolID = 0x1122334455667788L;

#if UNITY_WEBGL && !UNITY_EDITOR

		[DllImport("__Internal")]
		public static extern void netcodeio_init();
	
		[DllImport("__Internal")]
		public static extern void netcodeio_query_support(string handlerObject);

		[DllImport("__Internal")]
		public static extern int netcodeio_createClient(string protocol, string handlerObject);

		[DllImport("__Internal")]
		public static extern void netcodeio_destroyClient(int handle);

		[DllImport("__Internal")]
		public static extern void netcodeio_connectClient(int handle, byte[] token, int tokenlength, string handlerObject);

		[DllImport("__Internal")]
		public static extern void netcodeio_clientSetTickRate(int handle, int tickrate);

		[DllImport("__Internal")]
		public static extern void netcodeio_clientSend(int handle, byte[] packetBuffer, int packetBufferLength, string handlerObject);

		[DllImport("__Internal")]
		public static extern void netcodeio_getClientState(int handle, string handlerObject);

		[DllImport("__Internal")]
		public static extern void netcodeio_clientAddListener(int handle, Action<int, int, IntPtr, int> callback);

#else
		public static void netcodeio_init() { }

		public static void netcodeio_query_support(string handlerObject)
		{
			GameObject.Find(handlerObject).SendMessage("OnNetcodeIOStatusAvailable", (int)NetcodeIOSupportStatus.Unavailable);
		}

		public static int netcodeio_createClient(string protocol, string handlerObject) { throw new NotImplementedException(); }
		public static void netcodeio_destroyClient(int handle) { throw new NotImplementedException(); }
		public static void netcodeio_connectClient(int handle, byte[] token, int tokenlength, string handlerObject) { throw new NotImplementedException(); }
		public static void netcodeio_clientSetTickRate(int handle, int tickrate) { throw new NotImplementedException(); }
		public static void netcodeio_clientSend(int handle, byte[] packetBuffer, int packetBufferLength, string handlerObject) { throw new NotImplementedException(); }
		public static void netcodeio_getClientState(int handle, string handlerObject) { throw new NotImplementedException(); }
		public static void netcodeio_clientAddListener(int handle, Action<int, int, IntPtr, int> callback) { throw new NotImplementedException(); }

#endif
	}
}