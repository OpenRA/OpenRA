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
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public interface ISpriteSequence
	{
		string Name { get; }
		int Length { get; }
		int Facings { get; }
		int Tick { get; }
		int ZOffset { get; }
		int ShadowZOffset { get; }
		Rectangle Bounds { get; }
		bool IgnoreWorldTint { get; }
		float Scale { get; }
		void ResolveSprites(SpriteCache cache);
		Sprite GetSprite(int frame);
		Sprite GetSprite(int frame, WAngle facing);
		(Sprite Sprite, WAngle Rotation) GetSpriteWithRotation(int frame, WAngle facing);
		Sprite GetShadow(int frame, WAngle facing);
		float GetAlpha(int frame);
	}

	public interface ISpriteSequenceLoader
	{
		int BgraSheetSize { get; }
		int IndexedSheetSize { get; }
		IReadOnlyDictionary<string, ISpriteSequence> ParseSequences(ModData modData, string tileSet, SpriteCache cache, MiniYamlNode node);
	}

	public sealed class SequenceSet : IDisposable
	{
		readonly ModData modData;
		readonly string tileSet;
		readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, ISpriteSequence>> images;
		public SpriteCache SpriteCache { get; }

		public SequenceSet(IReadOnlyFileSystem fileSystem, ModData modData, string tileSet, MiniYaml additionalSequences)
		{
			this.modData = modData;
			this.tileSet = tileSet;
			SpriteCache = new SpriteCache(fileSystem, modData.SpriteLoaders, modData.SpriteSequenceLoader.BgraSheetSize, modData.SpriteSequenceLoader.IndexedSheetSize);
			using (new Support.PerfTimer("LoadSequences"))
				images = Load(fileSystem, additionalSequences);
		}

		public ISpriteSequence GetSequence(string image, string sequence)
		{
			if (!images.TryGetValue(image, out var sequences))
				throw new InvalidOperationException($"Image `{image}` does not have any sequences defined.");

			if (!sequences.TryGetValue(sequence, out var seq))
				throw new InvalidOperationException($"Image `{image}` does not have a sequence named `{sequence}`.");

			return seq;
		}

		public IEnumerable<string> Images => images.Keys;

		public bool HasSequence(string image, string sequence)
		{
			if (!images.TryGetValue(image, out var sequences))
				throw new InvalidOperationException($"Image `{image}` does not have any sequences defined.");

			return sequences.ContainsKey(sequence);
		}

		public IEnumerable<string> Sequences(string image)
		{
			if (!images.TryGetValue(image, out var sequences))
				throw new InvalidOperationException($"Image `{image}` does not have any sequences defined.");

			return sequences.Keys;
		}

		IReadOnlyDictionary<string, IReadOnlyDictionary<string, ISpriteSequence>> Load(IReadOnlyFileSystem fileSystem, MiniYaml additionalSequences)
		{
			var nodes = MiniYaml.Load(fileSystem, modData.Manifest.Sequences, additionalSequences);
			var images = new Dictionary<string, IReadOnlyDictionary<string, ISpriteSequence>>();
			foreach (var node in nodes)
			{
				// Nodes starting with ^ are inheritable but never loaded directly
				if (node.Key.StartsWith(ActorInfo.AbstractActorPrefix, StringComparison.Ordinal))
					continue;

				images[node.Key] = modData.SpriteSequenceLoader.ParseSequences(modData, tileSet, SpriteCache, node);
			}

			return images;
		}

		public void LoadSprites()
		{
			SpriteCache.LoadReservations(modData);
			foreach (var sequences in images.Values)
				foreach (var sequence in sequences)
					sequence.Value.ResolveSprites(SpriteCache);
		}

		public void Dispose()
		{
			SpriteCache.Dispose();
		}
	}
}
