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
	public enum SpriteType { Unknown, ShpD2, TmpTD, TmpRA, TmpTS }
	public static class SpriteSource
	{
		static bool IsTmpRA(Stream s)
		{
			var start = s.Position;

			s.Position += 20;
			var a = s.ReadUInt32();
			s.Position += 2;
			var b = s.ReadUInt16();

			s.Position = start;
			return a == 0 && b == 0x2c73;
		}

		static bool IsTmpTD(Stream s)
		{
			var start = s.Position;

			s.Position += 16;
			var a = s.ReadUInt32();
			var b = s.ReadUInt32();

			s.Position = start;
			return a == 0 && b == 0x0D1AFFFF;
		}

		static bool IsTmpTS(Stream s)
		{
			var start = s.Position;
			s.Position += 8;
			var sx = s.ReadUInt32();
			var sy = s.ReadUInt32();

			// Find the first frame
			var offset = s.ReadUInt32();

			if (offset > s.Length - 52)
			{
				s.Position = start;
				return false;
			}

			s.Position = offset + 12;
			var test = s.ReadUInt32();

			s.Position = start;
			return test == sx * sy / 2 + 52;
		}

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
			if (IsTmpRA(s))
				return SpriteType.TmpRA;

			if (IsTmpTD(s))
				return SpriteType.TmpTD;

			if (IsTmpTS(s))
				return SpriteType.TmpTS;

			if (IsShpD2(s))
				return SpriteType.ShpD2;

			return SpriteType.Unknown;
		}

		public static ISpriteSource LoadSpriteSource(Stream s, string filename)
		{
			var type = DetectSpriteType(s);
			switch (type)
			{
				case SpriteType.TmpRA:
					return new TmpRAReader(s);
				case SpriteType.TmpTD:
					return new TmpTDReader(s);
				case SpriteType.TmpTS:
					return new TmpTSReader(s);
				case SpriteType.ShpD2:
					return new ShpD2Reader(s);
				case SpriteType.Unknown:
				default:
					throw new InvalidDataException(filename + " is not a valid sprite file");
			}
		}
	}
}
