using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace ImageDecode
{
	public class ImageHeader
	{
		public uint Offset;
		public Format Format;
		public uint RefOffset;
		public Format RefFormat;
		public byte[] Image;

		public ImageHeader( BinaryReader reader )
		{
			Offset = reader.ReadUInt32();
			Format = (Format)( Offset >> 24 );
			Offset &= 0xFFFFFF;

			RefOffset = reader.ReadUInt16();
			RefFormat = (Format)reader.ReadUInt16();
		}
	}

	public enum Format
	{
		Format20 = 0x20,
		Format40 = 0x40,
		Format80 = 0x80,
	}

	public static class ShpReader
	{
		public static IEnumerable<byte[]> Read( Stream stream )
		{
			BinaryReader reader = new BinaryReader( stream );

			ushort numImages = reader.ReadUInt16();
			reader.ReadUInt16();
			reader.ReadUInt16();
			ushort width = reader.ReadUInt16();
			ushort height = reader.ReadUInt16();
			reader.ReadUInt32();

			List<ImageHeader> headers = new List<ImageHeader>();
			for( int i = 0 ; i < numImages ; i++ )
				headers.Add( new ImageHeader( reader ) );

			new ImageHeader( reader ); // end-of-file header
			new ImageHeader( reader ); // all-zeroes header

			foreach( ImageHeader h in headers )
			{
				reader.BaseStream.Position = h.Offset;
				switch( h.Format )
				{
					case Format.Format20:
						//throw new NotImplementedException();
						break;
					case Format.Format40:
						//throw new NotImplementedException();
						break;
					case Format.Format80:
						{
							byte[] compressedBytes = reader.ReadBytes( (int)( reader.BaseStream.Length - reader.BaseStream.Position ) );
							MemoryStream ms = new MemoryStream( compressedBytes );
							byte[] imageBytes = new byte[ width * height ];
							Format80.DecodeInto( ms, imageBytes );
							h.Image = imageBytes;
							break;
						}
					default:
						throw new InvalidDataException();
				}

				if( h.Image != null )
					yield return h.Image;
			}
		}
	}
}
