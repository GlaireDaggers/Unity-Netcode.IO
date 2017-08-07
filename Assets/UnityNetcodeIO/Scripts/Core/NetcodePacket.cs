namespace UnityNetcodeIO
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	using UnityNetcodeIO.Internal;

	/// <summary>
	/// Represents a packet of data
	/// </summary>
	public struct NetcodePacket
	{
		public int ClientID;
		public ByteBuffer PacketBuffer;

		public void Release()
		{
			BufferPool.ReturnBuffer(PacketBuffer);
			PacketBuffer = null;
		}
	}
}