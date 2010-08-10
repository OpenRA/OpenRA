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
	public class VqaReader
	{
		public VqaReader( Stream stream )
		{
			BinaryReader reader = new BinaryReader( stream );

			// Decode FORM chunk
			if (new String(reader.ReadChars(4)) != "FORM")
				throw new InvalidDataException("Invalid vqa (invalid FORM section)");
			
			var fileBTF = reader.ReadUInt32();
			
			if (new String(reader.ReadChars(8)) != "WVQAVQHD")
				throw new InvalidDataException("Invalid vqa (not WVQAVQHD)");
			
			var rStartPos = reader.ReadUInt32();
			var version = reader.ReadUInt16();
			var flags = reader.ReadUInt16();
			var numFrames = reader.ReadUInt16();
			var width = reader.ReadUInt16();
			var height = reader.ReadUInt16();
			
			var blockWidth = reader.ReadByte();
			var blockHeight = reader.ReadByte();
			var framerate = reader.ReadByte();
			var cbParts = reader.ReadByte();
			
			var colors = reader.ReadUInt16();
			var maxBlocks = reader.ReadUInt16();
			/*var unknown1 = */reader.ReadUInt16();
			/*var unknown2 = */reader.ReadUInt32();
			
			// Audio?
			var freq = reader.ReadUInt16();
			var channels = reader.ReadByte();
			var bits = reader.ReadByte();
			
			/*var unknown3 = */reader.ReadChars(14);
			
			Console.WriteLine("FORM Info");
			Console.WriteLine("\tVersion: {0}",version);
			Console.WriteLine("\tFlags: {0}",flags);
			Console.WriteLine("\tFrames: {0}",numFrames);
			Console.WriteLine("\tFramerate: {0}",framerate);
			Console.WriteLine("\tSize: {0}x{1}",width,height);
			Console.WriteLine("\tBlocksize: {0}x{1}",blockWidth,blockHeight);
			Console.WriteLine("\tAudio: {0}hz, {1} channel(s), {2} bit",freq, channels, bits);

			// The next section should be the first FINF chunk
			if (new String(reader.ReadChars(4)) != "FINF")
				throw new InvalidDataException("Invalid vqa (invalid FINF section)");
		}
	}
}
