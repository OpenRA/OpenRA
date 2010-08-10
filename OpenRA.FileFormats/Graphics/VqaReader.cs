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
using System.Drawing.Imaging;

namespace OpenRA.FileFormats
{
	public class VqaReader
	{
		Stream stream;
		ushort flags;
		public readonly ushort numFrames;
		public readonly byte framerate;
		ushort numColors;
		public readonly ushort width;
		public readonly ushort height;
		ushort blockWidth;
		ushort blockHeight;
		byte cbParts;
		int2 blocks;
		UInt32[] frames;
		int currentFrame;
		
		// Stores a list of subpixels, referenced by the VPTZ chunk
		byte[] cbf; 
		
		// cbf array is updated every 8 frames
		List<byte> newcbfFormat80 = new List<byte>();			
		int cbpCount = 0;
		Color[] palette;
		
		// Contains a listof palette indices for the current frame
		byte[] framedata;

		public VqaReader( Stream stream )
		{
			this.stream = stream;
			BinaryReader reader = new BinaryReader( stream );
			// Decode FORM chunk
			if (new String(reader.ReadChars(4)) != "FORM")
				throw new InvalidDataException("Invalid vqa (invalid FORM section)");
			
			/*var length = */ reader.ReadUInt32();
			
			if (new String(reader.ReadChars(8)) != "WVQAVQHD")
				throw new InvalidDataException("Invalid vqa (not WVQAVQHD)");
			
			/* var length = */reader.ReadUInt32();
			var version = reader.ReadUInt16();
			flags = reader.ReadUInt16();
			numFrames = reader.ReadUInt16();
			width = reader.ReadUInt16();
			height = reader.ReadUInt16();
			
			blockWidth = reader.ReadByte();
			blockHeight = reader.ReadByte();
			framerate = reader.ReadByte();
			cbParts = reader.ReadByte();
			blocks = new int2(width / blockWidth, height / blockHeight);
			
			numColors = reader.ReadUInt16();
			/*var maxBlocks = */reader.ReadUInt16();
			/*var unknown1 = */reader.ReadUInt16();
			/*var unknown2 = */reader.ReadUInt32();

			cbf = new byte[width*height];
			palette = new Color[numColors];
			framedata = new byte[2*blocks.X*blocks.Y];
			
			
			// Audio?
			var freq = reader.ReadUInt16();
			var channels = reader.ReadByte();
			var bits = reader.ReadByte();
			
			/*var unknown3 = */reader.ReadChars(14);
			
			// Decode FINF chunk
			if (new String(reader.ReadChars(4)) != "FINF")
				throw new InvalidDataException("Invalid vqa (invalid FINF section)");
			
			/*var offset = */reader.ReadUInt16();
			/*var unknown4 = */reader.ReadUInt16();
			
			// Frame offsets
			frames = new UInt32[numFrames];
			for (int i = 0; i < numFrames; i++)
			{
				frames[i] = reader.ReadUInt32();
				if (frames[i] > 0x40000000) frames[i] -= 0x40000000;
				frames[i] <<= 1;
			}
			
			// Load the first frame
			currentFrame = 0;
			AdvanceFrame();
		}
		
		public void AdvanceFrame()
		{			
			// Seek to the start of the frame
			stream.Seek(frames[currentFrame], SeekOrigin.Begin);
			BinaryReader reader = new BinaryReader(stream);
			var end = (currentFrame < numFrames - 1) ? frames[currentFrame+1] : stream.Length;
	
			while(reader.BaseStream.Position < end)
			{
				var type = new String(reader.ReadChars(4));
				var length = Swap(reader.ReadUInt32());

				switch(type)
				{
					case "SND2":
						// Don't parse sound (yet); skip data
						reader.ReadBytes((int)length);
					break;
					case "VQFR":
						DecodeVQFR(reader);
					break;
					default: 
						throw new InvalidDataException("Unknown chunk {0}".F(type));
				}
				
				// Chunks are aligned on even bytes; advance by a byte if the next one is null
				if (reader.PeekChar() == 0) reader.ReadByte();
			}
			if (++currentFrame == numFrames)
				currentFrame = 0;
		}
		
		public void FrameData(ref Bitmap frame)
		{
			var bitmapData = frame.LockBits(new Rectangle(0, 0, width, height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			unsafe
			{
				int* c = (int*)bitmapData.Scan0;

				for (var y = 0; y < blocks.Y; y++)
					for (var x = 0; x < blocks.X; x++)
					{
						var px = framedata[x + y*blocks.X];
						var mod = framedata[x + (y + blocks.Y)*blocks.X];
						for (var j = 0; j < blockHeight; j++)
							for (var i = 0; i < blockWidth; i++)
							{
								var cbfi = (mod*256 + px)*8 + j*blockWidth + i;
								byte color = (mod == 0x0f) ? px : cbf[cbfi];
								*(c + ((y*blockHeight + j) * bitmapData.Stride >> 2) + x*blockWidth + i) = palette[color].ToArgb();
							}
					}
			}
			frame.UnlockBits(bitmapData);
		}
		
		// VQA Frame
		public void DecodeVQFR(BinaryReader reader)
		{			
			while(true)
			{				
				// Chunks are aligned on even bytes; may be padded with a single null
				if (reader.PeekChar() == 0) reader.ReadByte();
				var type = new String(reader.ReadChars(4));
				int subchunkLength = (int)Swap(reader.ReadUInt32());

				switch(type)
				{
					// Full compressed frame-modifier
					case "CBFZ":
						Format80.DecodeInto( reader.ReadBytes(subchunkLength), cbf );
					break;
					case "CBF0":
						cbf = reader.ReadBytes(subchunkLength);
					break;
					
					// Partial compressed frame-modifier
					case "CBPZ":
						// Partial buffer is full; dump and recreate
						if (cbpCount == cbParts)
						{
							Format80.DecodeInto( newcbfFormat80.ToArray(), cbf );
							cbpCount = 0;
							newcbfFormat80.Clear();
						}
						var bytes = reader.ReadBytes(subchunkLength);
						foreach (var b in bytes) newcbfFormat80.Add(b);
						cbpCount++;
					break;
					case "CBP0":
						// Partial buffer is full; dump and recreate
						if (cbpCount == cbParts)
						{
							cbf = newcbfFormat80.ToArray();
							cbpCount = 0;
							newcbfFormat80.Clear();
						}
						var bytes2 = reader.ReadBytes(subchunkLength);
						foreach (var b in bytes2) newcbfFormat80.Add(b);
						cbpCount++;
					break;
					// Palette
					case "CPL0":
						for (int i = 0; i < numColors; i++)
						{
							byte r = reader.ReadByte();
							byte g = reader.ReadByte();
							byte b = reader.ReadByte();
							palette[i] = Color.FromArgb(255,ToColorByte(r),ToColorByte(g),ToColorByte(b));
						}
					break;
					
					// Frame data
					case "VPTZ":
						Format80.DecodeInto( reader.ReadBytes(subchunkLength), framedata );
						// This is the last subchunk
						return;
					default:
						throw new InvalidDataException("Unknown sub-chunk {0}".F(type));
				}
			}
		}
		
		public byte ToColorByte(byte b)
		{
			return (byte)((b & 63) * 255 / 63);
		}
		
		// Change endianness of a uint32
		public UInt32 Swap(UInt32 orig)
		{
			return (UInt32)((orig & 0xff000000) >> 24) | ((orig & 0x00ff0000) >> 8) | ((orig & 0x0000ff00) << 8) | ((orig & 0x000000ff) << 24);
		}		
	}
}
