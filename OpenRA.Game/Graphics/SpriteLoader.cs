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
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public interface ISpriteLoader
	{
		bool TryParseSprite(Stream s, out ISpriteFrame[] frames);
	}

	public interface ISpriteFrame
	{
		Size Size { get; }
		Size FrameSize { get; }
		float2 Offset { get; }
		byte[] Data { get; }
		bool DisableExportPadding { get; }
	}

	public class SpriteCache
	{
		public readonly SheetBuilder SheetBuilder;
		readonly Cache<string, Sprite[]> sprites;

		public SpriteCache(ISpriteLoader[] loaders, string[] exts, SheetBuilder sheetBuilder)
		{
			SheetBuilder = sheetBuilder;
			// Include extension-less version
			exts = exts.Append("").ToArray();
			sprites = new Cache<string, Sprite[]>(filename => SpriteLoader.GetSprites(filename, exts, loaders, sheetBuilder));
		}

		public Sprite[] this[string filename] { get { return sprites[filename]; } }
	}

	public class FrameCache
	{
		readonly Cache<string, ISpriteFrame[]> frames;

		public FrameCache(ISpriteLoader[] loaders, string[] exts)
		{
			frames = new Cache<string, ISpriteFrame[]>(filename => SpriteLoader.GetFrames(filename, exts, loaders));
		}

		public ISpriteFrame[] this[string filename] { get { return frames[filename]; } }
	}

	public static class SpriteLoader
	{
		public static Sprite[] GetSprites(string filename, string[] exts, ISpriteLoader[] loaders, SheetBuilder sheetBuilder)
		{
			return GetFrames(filename, exts, loaders).Select(a => sheetBuilder.Add(a)).ToArray();
		}

		public static ISpriteFrame[] GetFrames(string filename, string[] exts, ISpriteLoader[] loaders)
		{
			using (var stream = GlobalFileSystem.OpenWithExts(filename, exts))
			{
				ISpriteFrame[] frames;
				foreach (var loader in loaders)
					if (loader.TryParseSprite(stream, out frames))
						return frames;

				throw new InvalidDataException(filename + " is not a valid sprite file");
			}
		}
	}
}
