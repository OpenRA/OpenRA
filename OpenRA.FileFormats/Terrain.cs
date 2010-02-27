#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileFormats
{
	public class Terrain
	{
		public readonly List<byte[]> TileBitmapBytes = new List<byte[]>();

		public Terrain( Stream stream )
		{
			int Width, Height;

			BinaryReader reader = new BinaryReader( stream );
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();

			if( Width != 24 || Height != 24 )
				throw new InvalidDataException( string.Format( "{0}x{1}", Width, Height ) );

			/*NumTiles = */reader.ReadUInt16();
			reader.ReadUInt16();
			/*XDim = */reader.ReadUInt16();
			/*YDim = */reader.ReadUInt16();
			/*uint FileSize = */reader.ReadUInt32();
			uint ImgStart = reader.ReadUInt32();
			reader.ReadUInt32();
			reader.ReadUInt32();
			int IndexEnd = reader.ReadInt32();
			reader.ReadUInt32();
			int IndexStart = reader.ReadInt32();

			stream.Position = IndexStart;

			foreach( byte b in new BinaryReader(stream).ReadBytes(IndexEnd - IndexStart) )
			{
				if (b != 255)
				{
					stream.Position = ImgStart + b * 24 * 24;
					TileBitmapBytes.Add(new BinaryReader(stream).ReadBytes(24 * 24));
				}
				else
					TileBitmapBytes.Add(null);
			}
		}
	}
}
