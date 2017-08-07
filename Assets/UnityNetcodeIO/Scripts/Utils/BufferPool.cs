namespace UnityNetcodeIO
{
	using System.Collections.Generic;

	/// <summary>
	/// Pool implementation for generic lists
	/// </summary>
	public static class BufferPool
	{
		private static Queue<ByteBuffer> pool = new Queue<ByteBuffer>();

		/// <summary>
		/// Get or create a new buffer
		/// </summary>
		public static ByteBuffer GetBuffer(int capacity)
		{
			ByteBuffer ret = null;

			if (pool.Count > 0)
			{
				ret = pool.Dequeue();
				ret.SetSize(capacity);
			}
			else
			{
				ret = new ByteBuffer(capacity);
			}

			return ret;
		}

		/// <summary>
		/// Return a buffer to the pool
		/// </summary>
		public static void ReturnBuffer(ByteBuffer list)
		{
			pool.Enqueue(list);
		}
	}

}