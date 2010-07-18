#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace OpenRA.FileFormats
{
	public enum Dune2ImageFlags : int
	{
		F80_F2 = 0,
		F2 = 2,
		L16_F80_F2_1 = 1,
		L16_F80_F2_2 = 3,
		Ln_F80_F2 = 5
	}

	public class Dune2ImageHeader
	{
		public readonly Dune2ImageFlags Flags;
		public readonly int Width;
		public readonly int Height;
		public readonly int Slices;
		public readonly int FileSize;
		public readonly int DataSize;

		public readonly byte[] LookupTable;
		public byte[] Image;

		public Dune2ImageHeader(BinaryReader reader)
		{
			Flags = (Dune2ImageFlags)reader.ReadUInt16();
			Slices = reader.ReadByte();
			Width = reader.ReadUInt16();
			Height = reader.ReadByte();
			FileSize = reader.ReadUInt16();
			DataSize = reader.ReadUInt16();

			if (Flags == Dune2ImageFlags.L16_F80_F2_1 ||
				Flags == Dune2ImageFlags.L16_F80_F2_2 ||
				Flags == Dune2ImageFlags.Ln_F80_F2)
			{
				int n = Flags == Dune2ImageFlags.Ln_F80_F2 ? reader.ReadByte() : (byte)16;
				LookupTable = new byte[n];
				for (int i = 0; i < n; i++)
					LookupTable[i] = reader.ReadByte();
			}
			else
			{
				LookupTable = new byte[256];
				for (int i = 0; i < 256; i++)
					LookupTable[i] = (byte)i;
				LookupTable[1] = 0x7f;
				LookupTable[2] = 0x7e;
				LookupTable[3] = 0x7d;
				LookupTable[4] = 0x7c;
			}
		}

		public Size Size
		{
			get { return new Size(Width, Height); }
		}
	}

	public class Dune2ShpReader : IEnumerable<Dune2ImageHeader>
	{
		public readonly int ImageCount;

		List<Dune2ImageHeader> headers = new List<Dune2ImageHeader>();

		public Dune2ShpReader(Stream stream)
		{
			BinaryReader reader = new BinaryReader(stream);

			ImageCount = reader.ReadUInt16();
			
			//Last offset is pointer to end of file.
			uint[] offsets = new uint[ImageCount + 1];

			uint temp = reader.ReadUInt32();

			//If fourth byte in file is non-zero, the offsets are two bytes each.
			bool twoByteOffsets = (temp & 0xFF0000) > 0; 
			if (twoByteOffsets)
			{
				offsets[0] = ((temp & 0xFFFF0000) >> 16) + 2; //Offset does not account for image count bytes
				offsets[1] = (temp & 0xFFFF) + 2;
			}
			else
				offsets[0] = temp + 2;

			for (int i = twoByteOffsets ? 2 : 1; i < ImageCount + 1; i++)
				offsets[i] = (twoByteOffsets ? reader.ReadUInt16() : reader.ReadUInt32()) + 2;

			for (int i = 0; i < ImageCount; i++)
			{
				reader.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
				Dune2ImageHeader header = new Dune2ImageHeader(reader);
				byte[] imgData = reader.ReadBytes(header.FileSize);
				header.Image = new byte[header.Height * header.Width];

				//Decode image data
				if (header.Flags != Dune2ImageFlags.F2)
				{
					byte[] tempData = new byte[header.DataSize];
					Format80.DecodeInto(imgData, tempData);
					Format2.DecodeInto(tempData, header.Image);
				}
				else
					Format2.DecodeInto(imgData, header.Image);

				//Lookup values in lookup table
				if (header.LookupTable != null)
					for (int j = 0; j < header.Image.Length; j++)
						header.Image[j] = header.LookupTable[header.Image[j]];

				headers.Add(header);
			}
		}

		public Dune2ImageHeader this[int index]
		{
			get { return headers[index]; }
		}
	
		public IEnumerator<Dune2ImageHeader> GetEnumerator()
		{
			return headers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
