#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace OpenRA.FileFormats
{
	public class ImageHeader
	{
		public uint Offset;
		public Format Format;

		public uint RefOffset;
		public Format RefFormat;
		public ImageHeader RefImage;

		public byte[] Image;

		public ImageHeader() { }

		public ImageHeader( BinaryReader reader )
		{
			Offset = reader.ReadUInt32();
			Format = (Format)( Offset >> 24 );
			Offset &= 0xFFFFFF;

			RefOffset = reader.ReadUInt16();
			RefFormat = (Format)reader.ReadUInt16();
		}

		public static readonly int SizeOnDisk = 8;

		public void WriteTo(BinaryWriter writer)
		{
			writer.Write(Offset | ((uint)Format << 24));
			writer.Write((ushort)RefOffset);
			writer.Write((ushort)RefFormat);
		}
	}

	public enum Format
	{
		Format20 = 0x20,
		Format40 = 0x40,
		Format80 = 0x80,
	}

	public class ShpReader : IEnumerable<ImageHeader>
	{
		public readonly int ImageCount;
		public readonly ushort Width;
		public readonly ushort Height;

		public Size Size { get { return new Size(Width, Height); } }

		readonly List<ImageHeader> headers = new List<ImageHeader>();

		int recurseDepth = 0;

		public ShpReader( Stream stream )
		{
			BinaryReader reader = new BinaryReader( stream );

			ImageCount = reader.ReadUInt16();
			reader.ReadUInt16();
			reader.ReadUInt16();
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();
			reader.ReadUInt32();

			for( int i = 0 ; i < ImageCount ; i++ )
				headers.Add( new ImageHeader( reader ) );

			new ImageHeader( reader ); // end-of-file header
			new ImageHeader( reader ); // all-zeroes header

			Dictionary<uint, ImageHeader> offsets = new Dictionary<uint, ImageHeader>();
			foreach( ImageHeader h in headers )
				offsets.Add( h.Offset, h );

			for( int i = 0 ; i < ImageCount ; i++ )
			{
				ImageHeader h = headers[ i ];
				if( h.Format == Format.Format20 )
					h.RefImage = headers[ i - 1 ];

				else if( h.Format == Format.Format40 )
				{
					if( !offsets.TryGetValue( h.RefOffset, out h.RefImage ) )
						throw new InvalidDataException( string.Format( "Reference doesnt point to image data {0}->{1}", h.Offset, h.RefOffset ) );
				}
			}

			foreach( ImageHeader h in headers )
				Decompress( stream, h );
		}

		public ImageHeader this[ int index ]
		{
			get { return headers[ index ]; }
		}

		void Decompress( Stream stream, ImageHeader h )
		{
			if( recurseDepth > ImageCount )
				throw new InvalidDataException( "Format20/40 headers contain infinite loop" );

			switch( h.Format )
			{
				case Format.Format20:
				case Format.Format40:
					{
						if( h.RefImage.Image == null )
						{
							++recurseDepth;
							Decompress( stream, h.RefImage );
							--recurseDepth;
						}

						h.Image = CopyImageData( h.RefImage.Image );
						Format40.DecodeInto(ReadCompressedData(stream, h), h.Image);
						break;
					}
				case Format.Format80:
					{
						byte[] imageBytes = new byte[ Width * Height ];
						Format80.DecodeInto( ReadCompressedData( stream, h ), imageBytes );
						h.Image = imageBytes;
						break;
					}
				default:
					throw new InvalidDataException();
			}
		}

		static byte[] ReadCompressedData( Stream stream, ImageHeader h )
		{
			stream.Position = h.Offset;
			// Actually, far too big. There's no length field with the correct length though :(
			int compressedLength = (int)( stream.Length - stream.Position );

			byte[] compressedBytes = new byte[ compressedLength ];
			stream.Read( compressedBytes, 0, compressedLength );

			//MemoryStream ms = new MemoryStream( compressedBytes );
			return compressedBytes;
		}

		byte[] CopyImageData( byte[] baseImage )
		{
			byte[] imageData = new byte[ Width * Height ];
			for( int i = 0 ; i < Width * Height ; i++ )
				imageData[ i ] = baseImage[ i ];

			return imageData;
		}

		public IEnumerator<ImageHeader> GetEnumerator()
		{
			return headers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public static ShpReader Load(string filename)
		{
			using (var s = File.OpenRead(filename))
				return new ShpReader(s);
		}
	}
}
