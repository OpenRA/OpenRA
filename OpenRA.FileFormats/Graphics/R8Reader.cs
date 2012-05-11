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
using System;

namespace OpenRA.FileFormats
{
	public class R8Image
	{
		public int Width;
		public int Height;
		public int OffsetX;
		public int OffsetY;
		
		public byte FrameWidth;
		public byte FrameHeight;
		
		public int ImageHandle;
		public int PaletteHandle;
		
		public byte[] Image;

		public R8Image( BinaryReader reader, int Frame )
		{
			while (reader.PeekChar() == 0) // skip alignment byte
				reader.ReadByte();
			
			var offset = reader.BaseStream.Position;
			var ID = reader.ReadByte();
			if (ID == 0)
				throw new InvalidDataException("Header with no data?");
			Width = reader.ReadInt32();
			Height = reader.ReadInt32();
			OffsetX = reader.ReadInt32();
			OffsetY = reader.ReadInt32();
			ImageHandle = reader.ReadInt32();
			PaletteHandle = reader.ReadInt32();
			var Bpp = reader.ReadByte();
			
			//if (Bpp != 8)
			//	throw new InvalidDataException("{0} != 8 bits per pixel".F(Bpp));
			
			FrameWidth = reader.ReadByte();
			FrameHeight = reader.ReadByte();
			
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
			

			if (reader.PeekChar() == 0) // skip alignment byte
				reader.ReadByte();
			// Load image
			Image = new byte[Width*Height];

			// Load (and ignore) custom palette
			if (ID == 1 && PaletteHandle != 0)
			{
				reader.ReadInt32(); // The memory under a palette was allocated  (???)
				reader.ReadInt32(); // Handle to colors array (in memory)
				for (int i = 0; i < Width*Height; i++)
					Image[i] = reader.ReadByte();
				for (int i = 0; i < 512; i++)
					reader.ReadByte();
			}
			else if (ID == 2 && PaletteHandle != 0) //current palette
			{
				for (int i = 0; i < Width*Height; i++)
					//reader.ReadByte();
					Image[i] = reader.ReadByte();
			}
			else //standard palette
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
