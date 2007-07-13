using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace OpenRa.FileFormats
{
	public class Terrain
	{
		public readonly List<byte[]> TileBitmapBytes = new List<byte[]>();

		public Terrain( Stream stream, Palette pal )
		{
			int Width, Height, XDim, YDim, NumTiles;

			BinaryReader reader = new BinaryReader( stream );
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();

			if( Width != 24 || Height != 24 )
				throw new InvalidDataException( string.Format( "{0}x{1}", Width, Height ) );

			NumTiles = reader.ReadUInt16();
			reader.ReadUInt16();
			XDim = reader.ReadUInt16();
			YDim = reader.ReadUInt16();
			uint FileSize = reader.ReadUInt32();
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
