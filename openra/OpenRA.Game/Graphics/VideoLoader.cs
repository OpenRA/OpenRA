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

namespace OpenRA.Video
{
	public interface IVideoLoader
	{
		bool TryParseVideo(Stream s, bool useFramePadding, out IVideo video);
	}

	public static class VideoLoader
	{
		public static IVideo GetVideo(Stream stream, bool useFramePadding, IVideoLoader[] loaders)
		{
			foreach (var loader in loaders)
				if (loader.TryParseVideo(stream, useFramePadding, out var video))
					return video;

			return null;
		}
	}
}
