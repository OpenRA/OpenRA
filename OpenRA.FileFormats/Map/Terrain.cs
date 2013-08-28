#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileFormats
{
	public class Terrain
	{
		public readonly List<byte[]> TileBitmapBytes = new List<byte[]>();
		public readonly int Width;
		public readonly int Height;

		public Terrain(Stream s)
		{
			// Try loading as a cnc .tem
			Width = s.ReadUInt16();
			Height = s.ReadUInt16();

			/*NumTiles = */s.ReadUInt16();
			/*Zero1 = */s.ReadUInt16();
			/*uint Size = */s.ReadUInt32();
			var imgStart = s.ReadUInt32();
			/*Zero2 = */s.ReadUInt32();

			int indexEnd, indexStart;

			// ID1 = FFFFh for cnc
			if (s.ReadUInt16() == 65535) 
			{
				/*ID2 = */s.ReadUInt16();
				indexEnd = s.ReadInt32();
				indexStart = s.ReadInt32();
			}
			else
			{
				// Load as a ra .tem
				s.Position = 0;
				Width = s.ReadUInt16();
				Height = s.ReadUInt16();

				/*NumTiles = */s.ReadUInt16();
				s.ReadUInt16();
				/*XDim = */s.ReadUInt16();
				/*YDim = */s.ReadUInt16();
				/*uint FileSize = */s.ReadUInt32();
				imgStart = s.ReadUInt32();
				s.ReadUInt32();
				s.ReadUInt32();
				indexEnd = s.ReadInt32();
				s.ReadUInt32();
				indexStart = s.ReadInt32();
			}

			s.Position = indexStart;

			foreach (byte b in s.ReadBytes(indexEnd - indexStart))
			{
				if (b != 255)
				{
					s.Position = imgStart + b * Width * Height;
					TileBitmapBytes.Add(s.ReadBytes(Width * Height));
				}
				else
					TileBitmapBytes.Add(null);
			}
		}
	}
}
