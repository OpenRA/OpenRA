#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.IO;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	// TODO: Most of this should be moved into the format parsers themselves.
	public enum SpriteType { Unknown, ShpD2 }
	public static class SpriteSource
	{
		static bool IsShpD2(Stream s)
		{
			var start = s.Position;

			// First word is the image count
			var imageCount = s.ReadUInt16();
			if (imageCount == 0)
			{
				s.Position = start;
				return false;
			}

			// Test for two vs four byte offset
			var testOffset = s.ReadUInt32();
			var offsetSize = (testOffset & 0xFF0000) > 0 ? 2 : 4;

			// Last offset should point to the end of file
			var finalOffset = start + 2 + offsetSize * imageCount;
			if (finalOffset > s.Length)
			{
				s.Position = start;
				return false;
			}

			s.Position = finalOffset;
			var eof = offsetSize == 2 ? s.ReadUInt16() : s.ReadUInt32();
			if (eof + 2 != s.Length)
			{
				s.Position = start;
				return false;
			}

			// Check the format flag on the first frame
			var b = s.ReadUInt16();
			s.Position = start;
			return b == 5 || b <= 3;
		}

		public static SpriteType DetectSpriteType(Stream s)
		{
			if (IsShpD2(s))
				return SpriteType.ShpD2;

			return SpriteType.Unknown;
		}

		public static ISpriteSource LoadSpriteSource(Stream s, string filename)
		{
			var type = DetectSpriteType(s);
			switch (type)
			{
				case SpriteType.ShpD2:
					return new ShpD2Reader(s);
				case SpriteType.Unknown:
				default:
					throw new InvalidDataException(filename + " is not a valid sprite file");
			}
		}
	}
}
