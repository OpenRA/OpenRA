using System;
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
				singletonManager = new RecyclableMemoryStreamManager();
			}

			return singletonManager.GetStream();
		}

		public static MemoryStream GetMemoryStream(byte[] buffer)
		{
			if (singletonManager == null)
			{
				singletonManager = new RecyclableMemoryStreamManager();
			}

			return singletonManager.GetStream(buffer);
		}
	}
}
