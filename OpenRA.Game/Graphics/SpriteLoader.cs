#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	/// <summary>
	/// Describes the format of the pixel data in a ISpriteFrame.
	/// Note that the channel order is defined for little-endian bytes, so BGRA corresponds
	/// to a 32bit ARGB value, such as that returned by Color.ToArgb()!
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
		bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata);
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

	public class SpriteCache
	{
		public readonly Cache<SheetType, SheetBuilder> SheetBuilders;
		readonly ISpriteLoader[] loaders;
		readonly IReadOnlyFileSystem fileSystem;

		readonly Dictionary<string, List<Sprite[]>> sprites = new Dictionary<string, List<Sprite[]>>();
		readonly Dictionary<string, ISpriteFrame[]> unloadedFrames = new Dictionary<string, ISpriteFrame[]>();
		readonly Dictionary<string, TypeDictionary> metadata = new Dictionary<string, TypeDictionary>();

		public SpriteCache(IReadOnlyFileSystem fileSystem, ISpriteLoader[] loaders)
		{
			SheetBuilders = new Cache<SheetType, SheetBuilder>(t => new SheetBuilder(t));

			this.fileSystem = fileSystem;
			this.loaders = loaders;
		}

		/// <summary>
		/// Returns the first set of sprites with the given filename.
		/// If getUsedFrames is defined then the indices returned by the function call
		/// are guaranteed to be loaded.  The value of other indices in the returned
		/// array are undefined and should never be accessed.
		/// </summary>
		public Sprite[] this[string filename, Func<int, IEnumerable<int>> getUsedFrames = null]
		{
			get
			{
				var allSprites = sprites.GetOrAdd(filename);
				var sprite = allSprites.FirstOrDefault();

				if (!unloadedFrames.TryGetValue(filename, out var unloaded))
					unloaded = null;

				// This is the first time that the file has been requested
				// Load all of the frames into the unused buffer and initialize
				// the loaded cache (initially empty)
				if (sprite == null)
				{
					unloaded = FrameLoader.GetFrames(fileSystem, filename, loaders, out var fileMetadata);
					unloadedFrames[filename] = unloaded;
					metadata[filename] = fileMetadata;

					sprite = new Sprite[unloaded.Length];
					allSprites.Add(sprite);
				}

				// HACK: The sequence code relies on side-effects from getUsedFrames
				var indices = getUsedFrames != null ? getUsedFrames(sprite.Length) :
					Enumerable.Range(0, sprite.Length);

				// Load any unused frames into the SheetBuilder
				if (unloaded != null)
				{
					foreach (var i in indices)
					{
						if (unloaded[i] != null)
						{
							sprite[i] = SheetBuilders[SheetBuilder.FrameTypeToSheetType(unloaded[i].Type)].Add(unloaded[i]);
							unloaded[i] = null;
						}
					}

					// All frames have been loaded
					if (unloaded.All(f => f == null))
						unloadedFrames.Remove(filename);
				}

				return sprite;
			}
		}

		/// <summary>
		/// Returns a TypeDictionary containing any metadata defined by the frame
		/// or null if the frame does not define metadata.
		/// </summary>
		public TypeDictionary FrameMetadata(string filename)
		{
			if (!metadata.TryGetValue(filename, out var fileMetadata))
			{
				FrameLoader.GetFrames(fileSystem, filename, loaders, out fileMetadata);
				metadata[filename] = fileMetadata;
			}

			return fileMetadata;
		}
	}

	public class FrameCache
	{
		readonly Cache<string, ISpriteFrame[]> frames;

		public FrameCache(IReadOnlyFileSystem fileSystem, ISpriteLoader[] loaders)
		{
			frames = new Cache<string, ISpriteFrame[]>(filename => FrameLoader.GetFrames(fileSystem, filename, loaders, out _));
		}

		public ISpriteFrame[] this[string filename] { get { return frames[filename]; } }
	}

	public static class FrameLoader
	{
		public static ISpriteFrame[] GetFrames(IReadOnlyFileSystem fileSystem, string filename, ISpriteLoader[] loaders, out TypeDictionary metadata)
		{
			using (var stream = fileSystem.Open(filename))
			{
				var spriteFrames = GetFrames(stream, loaders, out metadata);
				if (spriteFrames == null)
					throw new InvalidDataException(filename + " is not a valid sprite file!");

				return spriteFrames;
			}
		}

		public static ISpriteFrame[] GetFrames(Stream stream, ISpriteLoader[] loaders, out TypeDictionary metadata)
		{
			metadata = null;

			foreach (var loader in loaders)
				if (loader.TryParseSprite(stream, out var frames, out metadata))
					return frames;

			return null;
		}
	}
}
