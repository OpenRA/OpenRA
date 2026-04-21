#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Video;

namespace OpenRA.Mods.Cnc.VideoLoaders
{
	public class VqaLoader : IVideoLoader
	{
		public bool TryParseVideo(Stream s, bool useFramePadding, out IVideo video)
		{
			video = null;

			if (s.Length == 0)
				return false;

			if (!IsWestwoodVqa(s))
				return false;

			video = new VqaVideo(s, useFramePadding);
			return true;
		}

		static bool IsWestwoodVqa(Stream s)
		{
			var start = s.Position;

			if (s.ReadASCII(4) != "FORM")
			{
				s.Position = start;
				return false;
			}

			var length = s.ReadUInt32();
			if (length == 0)
			{
				s.Position = start;
				return false;
			}

			if (s.ReadASCII(4) != "WVQA")
			{
				s.Position = start;
				return false;
			}

			s.Position = start;
			return true;
		}
	}
}
