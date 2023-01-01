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
using System.Linq;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Video;

namespace OpenRA.Mods.Cnc.VideoLoaders
{
	public class WsaLoader : IVideoLoader
	{
		public bool TryParseVideo(Stream s, bool useFramePadding, out IVideo video)
		{
			video = null;

			if (s.Length == 0)
				return false;

			if (!IsWsa(s))
				return false;

			video = new WsaVideo(s, useFramePadding);
			return true;
		}

		static bool IsWsa(Stream s)
		{
			var start = s.Position;

			var frames = s.ReadUInt16();
			if (frames <= 1) // TODO: find a better way to differentiate .shp icons
				return false;

			/* var x = */ s.ReadUInt16();
			/* var y = */ s.ReadUInt16();
			var width = s.ReadUInt16();
			var height = s.ReadUInt16();

			if (width <= 0 || height <= 0)
				return false;

			/*var delta = */ s.ReadUInt16(); /* + 37;*/

			var flags = s.ReadUInt16();

			var offsets = new uint[frames + 2];
			for (var i = 0; i < offsets.Length; i++)
				offsets[i] = s.ReadUInt32();

			if (flags == 1)
			{
				/* var palette = */ s.ReadBytes(768);
				for (var i = 0; i < offsets.Length; i++)
					offsets[i] += 768;
			}

			s.Position = start;

			return s.Length == offsets.Last();
		}
	}
}
