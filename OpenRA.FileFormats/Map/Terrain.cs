#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileFormats
{
	public class Terrain
	{
		public readonly List<byte[]> TileBitmapBytes = new List<byte[]>();

		public Terrain( Stream stream, int size )
		{		
			// Try loading as a cnc .tem
			BinaryReader reader = new BinaryReader( stream );
			int Width = reader.ReadUInt16();
			int Height = reader.ReadUInt16();
			
			if( Width != size || Height != size )
				throw new InvalidDataException( "{0}x{1} != {2}x{2}".F(Width, Height, size ) );
			
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
					stream.Position = ImgStart + b * size * size;
					TileBitmapBytes.Add(new BinaryReader(stream).ReadBytes(size * size));
				}
				else
					TileBitmapBytes.Add(null);
			}
		}
	}
}
