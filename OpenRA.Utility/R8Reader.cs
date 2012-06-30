#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 * It also incorporates parts of http://code.google.com/p/dune2000plusone
 * which is licensed under the BSD 2-Clause License.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System;

namespace OpenRA.Utility
{
	public class R8Image
	{
		public int Width;
		public int Height;

		public byte FrameWidth;
		public byte FrameHeight;

		public int ImageHandle;
		public int PaletteHandle;
		
		public byte[] Image;

		public int OffsetX;
		public int OffsetY;

		public R8Image(BinaryReader reader, int Frame)
		{
			var offset = reader.BaseStream.Position;
			var ID = reader.ReadByte(); // 0 = no data, 1 = picture with palette, 2 = picture with current palette
			while (ID == 0)
				ID = reader.ReadByte();
			Width = reader.ReadInt32(); //Width of picture
			Height = reader.ReadInt32(); //Height of picture
			OffsetX = reader.ReadInt32(); //Offset on X axis from left border edge of virtual frame
			OffsetY = reader.ReadInt32(); //Offset on Y axis from top border edge of virtual frame
			ImageHandle = reader.ReadInt32(); // 0 = no picture
			PaletteHandle = reader.ReadInt32(); // 0 = no palette
			var Bpp = reader.ReadByte(); // Bits per Pixel
			FrameHeight = reader.ReadByte(); // Height of virtual frame
			FrameWidth = reader.ReadByte(); // Width of virtual frame
			var Align = reader.ReadByte(); //Alignment on even border

			Console.WriteLine("Offset: {0}",offset);
			Console.WriteLine("ID: {0}",ID);
			Console.WriteLine("Width: {0}",Width);
			Console.WriteLine("Height: {0}",Height);
			Console.WriteLine("OffsetX: {0}",OffsetX);
			Console.WriteLine("OffsetY: {0}",OffsetY);
			Console.WriteLine("ImageHandle: {0}",ImageHandle);
			Console.WriteLine("PaletteHandle: {0}",PaletteHandle);
			Console.WriteLine("Bpp: {0}",Bpp);
			Console.WriteLine("FrameWidth: {0}",FrameWidth);
			Console.WriteLine("FrameHeight: {0}",FrameHeight);
			Console.WriteLine("Align: {0}",Align);

			// Load image
			if (Bpp == 8)
				Image = new byte[Width*Height];
			else
				throw new InvalidDataException("Error: {0} bits per pixel are not supported.".F(Bpp));

			
			if (ID == 1 && PaletteHandle != 0)
			{
				// read and ignore custom palette
				reader.ReadInt32(); //Memory
				reader.ReadInt32(); //Handle

				for (int i = 0; i < Width*Height; i++)
					Image[i] = reader.ReadByte();
				for (int i = 0; i < 256; i++)
					reader.ReadUInt16();
			}
			else if (ID == 2 && PaletteHandle != 0) // image with custom palette
			{
				for (int i = 0; i < Width*Height; i++)
					Image[i] = reader.ReadByte();
			}
			else //standard palette or 16 Bpp
			{
				for (int i = 0; i < Width*Height; i++)
					Image[i] = reader.ReadByte();
			}
		}
	}

	public class R8Reader : IEnumerable<R8Image>
	{
		private readonly List<R8Image> headers = new List<R8Image>();

		public readonly int Frames;
		public R8Reader( Stream stream )
		{
			BinaryReader reader = new BinaryReader( stream );

			Frames = 0;
			while (reader.BaseStream.Position < stream.Length)
			{
				try
				{
					Console.WriteLine("Frame {0}: {1}",Frames, reader.BaseStream.Position);
					headers.Add( new R8Image( reader, Frames ) );
					Frames++;
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					break;
				}
			}
		}

		public R8Image this[ int index ]
		{
			get { return headers[ index ]; }
		}

		public IEnumerator<R8Image> GetEnumerator()
		{
			return headers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
