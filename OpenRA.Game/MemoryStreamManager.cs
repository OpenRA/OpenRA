#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

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

		public static MemoryStream GetMemoryStream(int minSizeRequired, bool tryContiguosBuffer, string tag = null)
		{
			if (singletonManager == null)
			{
				Initialize();
			}

			return singletonManager.GetStream(tag, minSizeRequired, tryContiguosBuffer);
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
