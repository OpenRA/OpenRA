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

		public Terrain(Stream stream)
		{
			// Try loading as a cnc .tem
			BinaryReader reader = new BinaryReader( stream );
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();

			/*NumTiles = */reader.ReadUInt16();
			/*Zero1 = */reader.ReadUInt16();
			/*uint Size = */reader.ReadUInt32();
			uint ImgStart = reader.ReadUInt32();
			/*Zero2 = */reader.ReadUInt32();

			int IndexEnd, IndexStart;
			if (reader.ReadUInt16() == 65535) // ID1 = FFFFh for cnc
			{
				/*ID2 = */reader.ReadUInt16();
				IndexEnd = reader.ReadInt32();
				IndexStart = reader.ReadInt32();
			}
			else // Load as a ra .tem
			{
				stream.Position = 0;
				reader = new BinaryReader( stream );
				Width = reader.ReadUInt16();
				Height = reader.ReadUInt16();

				/*NumTiles = */reader.ReadUInt16();
				reader.ReadUInt16();
				/*XDim = */reader.ReadUInt16();
				/*YDim = */reader.ReadUInt16();
				/*uint FileSize = */reader.ReadUInt32();
				ImgStart = reader.ReadUInt32();
				reader.ReadUInt32();
				reader.ReadUInt32();
				IndexEnd = reader.ReadInt32();
				reader.ReadUInt32();
				IndexStart = reader.ReadInt32();
			}
			stream.Position = IndexStart;

			foreach( byte b in new BinaryReader(stream).ReadBytes(IndexEnd - IndexStart) )
			{
				if (b != 255)
				{
					stream.Position = ImgStart + b * Width * Height;
					TileBitmapBytes.Add(new BinaryReader(stream).ReadBytes(Width * Height));
				}
				else
					TileBitmapBytes.Add(null);
			}
		}
	}
}
