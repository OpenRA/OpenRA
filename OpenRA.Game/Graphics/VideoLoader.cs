#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;

namespace OpenRA.Video
{
	public interface IVideoLoader
	{
		bool TryParseVideo(Stream s, out IVideo video);
	}

	public static class VideoLoader
	{
		public static IVideo GetVideo(Stream stream, IVideoLoader[] loaders)
		{
			foreach (var loader in loaders)
				if (loader.TryParseVideo(stream, out var video))
					return video;

			return null;
		}
	}
}
