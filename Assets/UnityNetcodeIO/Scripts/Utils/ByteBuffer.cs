namespace UnityNetcodeIO
{
	/// <summary>
	/// Holds a resizable array of bytes
	/// </summary>
	public class ByteBuffer
	{
		/// <summary>
		/// The number of bytes in the buffer
		/// </summary>
		public int Length
		{
			get { return size; }
		}

		/// <summary>
		/// The internal byte array
		/// </summary>
		public byte[] InternalBuffer
		{
			get { return _buffer; }
		}

		protected byte[] _buffer;
		protected int size;

		public ByteBuffer(int size = 0)
		{
			_buffer = new byte[size];
			this.size = size;
		}

		/// <summary>
		/// Resize the buffer
		/// </summary>
		public void SetSize(int newSize)
		{
			if (_buffer.Length < newSize)
			{
				byte[] newBuffer = new byte[newSize];
				System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _buffer.Length);

				_buffer = newBuffer;
			}

			size = newSize;
		}

		/// <summary>
		/// Copy bytes into the buffer
		/// </summary>
		public void BufferCopy(byte[] source, int src, int dest, int length)
		{
			System.Buffer.BlockCopy(source, src, _buffer, dest, length);
		}

		/// <summary>
		/// Copy bytes into the buffer
		/// </summary>
		public void BufferCopy(ByteBuffer source, int src, int dest, int length)
		{
			System.Buffer.BlockCopy(source._buffer, src, _buffer, dest, length);
		}

		/// <summary>
		/// Copy bytes from source array into buffer
		/// </summary>
		public unsafe void MemoryCopy(byte[] source, int src, int dest, int length)
		{
			fixed (byte* srcPtr = &source[src])
			{
				MemoryCopy(srcPtr, dest, length);
			}
		}

		/// <summary>
		/// Copy bytes from source pointer into buffer
		/// </summary>
		public unsafe void MemoryCopy(byte* source, int dest, int length)
		{
			byte* destPtr = (byte*)GetPointer(0);

			int remaining = length;
			while (remaining > 0)
			{
				if (remaining >= sizeof(ulong))
				{
					*((ulong*)destPtr) = *((ulong*)source);
					destPtr += sizeof(ulong);
					source += sizeof(ulong);

					remaining -= sizeof(ulong);
				}
				else if (remaining >= sizeof(uint))
				{
					*((uint*)destPtr) = *((uint*)source);
					destPtr += sizeof(uint);
					source += sizeof(uint);

					remaining -= sizeof(uint);
				}
				else if (remaining >= sizeof(ushort))
				{
					*((ushort*)destPtr) = *((ushort*)source);
					destPtr += sizeof(ushort);
					source += sizeof(ushort);

					remaining -= sizeof(ushort);
				}
				else
				{
					*destPtr++ = *source++;
					remaining--;
				}
			}
		}

		/// <summary>
		/// Get a pointer to a buffer element
		/// </summary>
		public unsafe void* GetPointer(int index)
		{
			fixed (void* ptr = &_buffer[index])
			{
				return ptr;
			}
		}

		/// <summary>
		/// Get or set a byte in the buffer
		/// </summary>
		public byte this[int index]
		{
			get
			{
				if (index < 0 || index > size) throw new System.IndexOutOfRangeException();
				return _buffer[index];
			}
			set
			{
				if (index < 0 || index > size) throw new System.IndexOutOfRangeException();
				_buffer[index] = value;
			}
		}
	}
}