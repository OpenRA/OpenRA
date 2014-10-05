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
	public enum SpriteType { Unknown }
	public static class SpriteSource
	{
		public static SpriteType DetectSpriteType(Stream s)
		{
			return SpriteType.Unknown;
		}

		public static ISpriteSource LoadSpriteSource(Stream s, string filename)
		{
			var type = DetectSpriteType(s);
			switch (type)
			{
				case SpriteType.Unknown:
				default:
					throw new InvalidDataException(filename + " is not a valid sprite file");
			}
		}
	}
}
