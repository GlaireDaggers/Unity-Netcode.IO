namespace UnityNetcodeIO.Internal
{
	using System;
	using System.Runtime.InteropServices;

	using UnityEngine;

	/// <summary>
	/// Wrapper around Netcode.IO API
	/// </summary>
	public class NetcodePluginWrapper
	{
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
		// TODO: these should ideally call into a C# Netcode.IO API so calling code can seamlessly switch between standalone and WebGL
		// In the meantime, when not running on WebGL, querying for support indicates no support and all other functions throw NotImplementedException

		public static void netcodeio_init() { }
		public static void netcodeio_query_support(string handlerObject) { GameObject.Find(handlerObject).SendMessage("OnNetcodeIOStatusAvailable", 1); }
		public static int netcodeio_createClient(string protocol, string handlerObject) { throw new System.NotImplementedException(); }
		public static void netcodeio_destroyClient(int handle) { throw new System.NotImplementedException(); }
		public static void netcodeio_connectClient(int handle, byte[] token, int tokenlength, string handlerObject) { throw new System.NotImplementedException(); }
		public static void netcodeio_clientSetTickRate(int handle, int tickrate) { throw new System.NotImplementedException(); }
		public static void netcodeio_clientSend(int handle, byte[] packetBuffer, int packetBufferLength, string handlerObject) { throw new System.NotImplementedException(); }
		public static void netcodeio_getClientState(int handle, string handlerObject) { throw new System.NotImplementedException(); }
		public static void netcodeio_clientAddListener(int handle, Action<int, int, IntPtr, int> callback) { throw new System.NotImplementedException(); }

#endif
	}
}