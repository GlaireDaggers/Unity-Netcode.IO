namespace UnityNetcodeIO.Internal
{
	using System.Collections.Generic;

	/// <summary>
	/// Pool implementation for generic lists
	/// </summary>
	public static class ListPool<T>
	{
		private static Queue<List<T>> pool = new Queue<List<T>>();

		/// <summary>
		/// Get or create a new list
		/// </summary>
		public static List<T> GetList(int capacity = -1)
		{
			List<T> ret = null;

			if (pool.Count > 0)
			{
				ret = pool.Dequeue();
				if (ret.Capacity < capacity)
					ret.Capacity = capacity;
			}
			else
			{
				ret = new List<T>();
			}

			return ret;
		}

		/// <summary>
		/// Return a list to the pool
		/// </summary>
		public static void ReturnList(List<T> list)
		{
			list.Clear();
			pool.Enqueue(list);
		}
	}

}