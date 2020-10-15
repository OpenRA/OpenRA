using System;
using System.Diagnostics;
using System.IO;
using Microsoft.IO;

namespace OpenRA
{
	public class MemoryStreamManager
	{
		static RecyclableMemoryStreamManager singletonManager;
		MemoryStreamManager()
		{
		}

		public static MemoryStream GetMemoryStream()
		{
			if (singletonManager == null)
			{
				Initialize();
			}

			return singletonManager.GetStream();
		}

		public static MemoryStream GetMemoryStream(byte[] buffer)
		{
			if (singletonManager == null)
			{
				Initialize();
			}

			return singletonManager.GetStream(buffer);
		}

		static void Initialize()
		{
			var blockSize = 4096;
			var maxBufferSize = blockSize * 1024 * 5;
			singletonManager = new RecyclableMemoryStreamManager(blockSize, blockSize * 512, maxBufferSize);
			singletonManager.AggressiveBufferReturn = true;
			singletonManager.MaximumFreeSmallPoolBytes = blockSize * 1024;
		}
	}
}
