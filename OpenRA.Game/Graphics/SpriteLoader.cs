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
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	/// <summary>
	/// Describes the format of the pixel data in a ISpriteFrame.
	/// Note that the channel order is defined for little-endian bytes, so BGRA corresponds
	/// to a 32bit ARGB value, such as that returned by Color.ToArgb().
	/// </summary>
	public enum SpriteFrameType
	{
		// 8 bit index into an external palette
		Indexed8,

		// 32 bit color such as returned by Color.ToArgb() or the bmp file format
		// (remember that little-endian systems place the little bits in the first byte!)
		Bgra32,

		// Like BGRA, but without an alpha channel
		Bgr24,

		// 32 bit color in big-endian format, like png
		Rgba32,

		// Like RGBA, but without an alpha channel
		Rgb24
	}

	public interface ISpriteLoader
	{
		bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata);
	}

	public interface ISpriteFrame
	{
		SpriteFrameType Type { get; }

		/// <summary>
		/// Size of the frame's `Data`.
		/// </summary>
		Size Size { get; }

		/// <summary>
		/// Size of the entire frame including the frame's `Size`.
		/// Think of this like a picture frame.
		/// </summary>
		Size FrameSize { get; }

		float2 Offset { get; }
		byte[] Data { get; }
		bool DisableExportPadding { get; }
	}

	public class FrameCache
	{
		readonly Cache<string, ISpriteFrame[]> frames;

		public FrameCache(IReadOnlyFileSystem fileSystem, ISpriteLoader[] loaders)
		{
			frames = new Cache<string, ISpriteFrame[]>(filename => FrameLoader.GetFrames(fileSystem, filename, loaders, out _));
		}

		public ISpriteFrame[] this[string filename] => frames[filename];
	}

	public static class FrameLoader
	{
		public static ISpriteFrame[] GetFrames(IReadOnlyFileSystem fileSystem, string filename, ISpriteLoader[] loaders, out TypeDictionary metadata)
		{
			using (var stream = fileSystem.Open(filename))
			{
				var spriteFrames = GetFrames(stream, loaders, filename, out metadata);
				if (spriteFrames == null)
					throw new InvalidDataException(filename + " is not a valid sprite file!");

				return spriteFrames;
			}
		}

		public static ISpriteFrame[] GetFrames(Stream stream, ISpriteLoader[] loaders, string filename, out TypeDictionary metadata)
		{
			metadata = null;

			foreach (var loader in loaders)
				if (loader.TryParseSprite(stream, filename, out var frames, out metadata))
					return frames;

			return null;
		}
	}
}
