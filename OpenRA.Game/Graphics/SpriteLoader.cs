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

	public interface ISpriteSource
	{
		IReadOnlyList<ISpriteFrame> Frames { get; }
	}

	public class SpriteLoader
	{
		public readonly SheetBuilder SheetBuilder;
		readonly ISpriteLoader[] loaders;
		readonly Cache<string, Sprite[]> sprites;
		readonly Cache<string, ISpriteFrame[]> frames;
		readonly string[] exts;

		public SpriteLoader(ISpriteLoader[] loaders, string[] exts, SheetBuilder sheetBuilder)
		{
			this.loaders = loaders;
			SheetBuilder = sheetBuilder;

			// Include extension-less version
			this.exts = exts.Append("").ToArray();
			sprites = new Cache<string, Sprite[]>(CacheSprites);
			frames = new Cache<string, ISpriteFrame[]>(CacheFrames);
		}

		Sprite[] CacheSprites(string filename)
		{
			return frames[filename].Select(a => SheetBuilder.Add(a))
				.ToArray();
		}

		ISpriteFrame[] CacheFrames(string filename)
		{
			using (var stream = GlobalFileSystem.OpenWithExts(filename, exts))
			{
				ISpriteFrame[] frames;
				foreach (var loader in loaders)
					if (loader.TryParseSprite(stream, out frames))
						return frames;

				// Fall back to the hardcoded types (for now).
				return SpriteSource.LoadSpriteSource(stream, filename).Frames
					.ToArray();
			}
		}

		public Sprite[] LoadAllSprites(string filename) { return sprites[filename]; }
		public ISpriteFrame[] LoadAllFrames(string filename) { return frames[filename]; }
	}
}
