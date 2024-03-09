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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public sealed class SpriteCache : IDisposable
	{
		public readonly Dictionary<SheetType, SheetBuilder> SheetBuilders;
		readonly ISpriteLoader[] loaders;
		readonly IReadOnlyFileSystem fileSystem;

		readonly Dictionary<int, (int[] Frames, MiniYamlNode.SourceLocation Location, bool Premultiplied)> spriteReservations = new();
		readonly Dictionary<int, (int[] Frames, MiniYamlNode.SourceLocation Location)> frameReservations = new();
		readonly Dictionary<string, List<int>> reservationsByFilename = new();

		readonly Dictionary<int, ISpriteFrame[]> resolvedFrames = new();
		readonly Dictionary<int, Sprite[]> resolvedSprites = new();

		readonly Dictionary<int, (string Filename, MiniYamlNode.SourceLocation Location)> missingFiles = new();

		int nextReservationToken = 1;

		public SpriteCache(IReadOnlyFileSystem fileSystem, ISpriteLoader[] loaders, int bgraSheetSize, int indexedSheetSize, int bgraSheetMargin = 1, int indexedSheetMargin = 1)
		{
			SheetBuilders = new Dictionary<SheetType, SheetBuilder>
			{
				{ SheetType.Indexed, new SheetBuilder(SheetType.Indexed, indexedSheetSize, indexedSheetMargin) },
				{ SheetType.BGRA, new SheetBuilder(SheetType.BGRA, bgraSheetSize, bgraSheetMargin) }
			};

			this.fileSystem = fileSystem;
			this.loaders = loaders;
		}

		public int ReserveSprites(string filename, IEnumerable<int> frames, MiniYamlNode.SourceLocation location, bool premultiplied = false)
		{
			var token = nextReservationToken++;
			spriteReservations[token] = (frames?.ToArray(), location, premultiplied);
			reservationsByFilename.GetOrAdd(filename, _ => new List<int>()).Add(token);
			return token;
		}

		public int ReserveFrames(string filename, IEnumerable<int> frames, MiniYamlNode.SourceLocation location)
		{
			var token = nextReservationToken++;
			frameReservations[token] = (frames?.ToArray(), location);
			reservationsByFilename.GetOrAdd(filename, _ => new List<int>()).Add(token);
			return token;
		}

		static ISpriteFrame[] GetFrames(IReadOnlyFileSystem fileSystem, string filename, ISpriteLoader[] loaders, out TypeDictionary metadata)
		{
			metadata = null;
			if (!fileSystem.TryOpen(filename, out var stream))
				return null;

			using (stream)
			{
				foreach (var loader in loaders)
					if (loader.TryParseSprite(stream, filename, out var frames, out metadata))
						return frames;

				return null;
			}
		}

		public void LoadReservations(ModData modData)
		{
			foreach (var sb in SheetBuilders.Values)
				sb.Current.CreateBuffer();

			var pendingResolve = new List<(string Filename, int FrameIndex, bool Premultiplied, ISpriteFrame Frame, Sprite[] SpritesForToken)>();
			foreach (var (filename, tokens) in reservationsByFilename)
			{
				modData.LoadScreen?.Display();
				var loadedFrames = GetFrames(fileSystem, filename, loaders, out _);
				foreach (var token in tokens)
				{
					if (frameReservations.TryGetValue(token, out var rf))
					{
						if (loadedFrames != null)
						{
							if (rf.Frames != null)
							{
								var resolved = new ISpriteFrame[loadedFrames.Length];
								foreach (var i in rf.Frames)
									resolved[i] = loadedFrames[i];
								resolvedFrames[token] = resolved;
							}
							else
								resolvedFrames[token] = loadedFrames;
						}
						else
						{
							resolvedFrames[token] = null;
							missingFiles[token] = (filename, rf.Location);
						}
					}

					if (spriteReservations.TryGetValue(token, out var rs))
					{
						if (loadedFrames != null)
						{
							var resolved = new Sprite[loadedFrames.Length];
							resolvedSprites[token] = resolved;
							var frames = rs.Frames ?? Enumerable.Range(0, loadedFrames.Length);

							foreach (var i in frames)
								pendingResolve.Add((filename, i, rs.Premultiplied, loadedFrames[i], resolved));
						}
						else
						{
							resolvedSprites[token] = null;
							missingFiles[token] = (filename, rs.Location);
						}
					}
				}
			}

			// When the sheet builder is adding sprites, it reserves height for the tallest sprite seen along the row.
			// We can achieve better sheet packing by keeping sprites with similar heights together.
			var orderedPendingResolve = pendingResolve.OrderBy(x => x.Frame.Size.Height);

			var spriteCache = new Dictionary<(string Filename, int FrameIndex, bool Premultiplied), Sprite>(pendingResolve.Count);
			foreach (var (filename, frameIndex, premultiplied, frame, spritesForToken) in orderedPendingResolve)
			{
				// Premultiplied and non-premultiplied sprites must be cached separately
				// to cover the case where the same image is requested in both versions.
				spritesForToken[frameIndex] = spriteCache.GetOrAdd(
					(filename, frameIndex, premultiplied),
					_ =>
					{
						var sheetBuilder = SheetBuilders[SheetBuilder.FrameTypeToSheetType(frame.Type)];
						return sheetBuilder.Add(frame, premultiplied);
					});

				modData.LoadScreen?.Display();
			}

			spriteReservations.Clear();
			frameReservations.Clear();
			reservationsByFilename.Clear();

			foreach (var sb in SheetBuilders.Values)
				sb.Current.ReleaseBuffer();
		}

		public Sprite[] ResolveSprites(int token)
		{
			var resolved = resolvedSprites[token];
			resolvedSprites.Remove(token);
			if (missingFiles.TryGetValue(token, out var r))
				throw new FileNotFoundException($"{r.Location}: {r.Filename} not found", r.Filename);

			return resolved;
		}

		public ISpriteFrame[] ResolveFrames(int token)
		{
			var resolved = resolvedFrames[token];
			resolvedFrames.Remove(token);
			if (missingFiles.TryGetValue(token, out var r))
				throw new FileNotFoundException($"{r.Location}: {r.Filename} not found", r.Filename);

			return resolved;
		}

		public IEnumerable<(string Filename, MiniYamlNode.SourceLocation Location)> MissingFiles => missingFiles.Values.ToHashSet();

		public void Dispose()
		{
			foreach (var sb in SheetBuilders.Values)
				sb.Dispose();
		}
	}
}
