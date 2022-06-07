#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	using Sequences = IReadOnlyDictionary<string, Lazy<IReadOnlyDictionary<string, ISpriteSequence>>>;
	using UnitSequences = Lazy<IReadOnlyDictionary<string, ISpriteSequence>>;

	public interface ISpriteSequence
	{
		string Name { get; }
		int Start { get; }
		int Length { get; }
		int Stride { get; }
		int Facings { get; }
		int InterpolatedFacings { get; }
		int Tick { get; }
		int ZOffset { get; }
		int ShadowStart { get; }
		int ShadowZOffset { get; }
		int[] Frames { get; }
		Rectangle Bounds { get; }
		bool IgnoreWorldTint { get; }
		float Scale { get; }

		Sprite GetSprite(int frame);
		Sprite GetSprite(int frame, WAngle facing);
		(Sprite, WAngle) GetSpriteWithRotation(int frame, WAngle facing);
		Sprite GetShadow(int frame, WAngle facing);
		float GetAlpha(int frame);
	}

	public interface ISpriteSequenceLoader
	{
		IReadOnlyDictionary<string, ISpriteSequence> ParseSequences(ModData modData, string tileSet, SpriteCache cache, MiniYamlNode node);
	}

	public class SequenceProvider : IDisposable
	{
		readonly ModData modData;
		readonly string tileSet;
		readonly Lazy<Sequences> sequences;
		readonly Lazy<SpriteCache> spriteCache;
		public SpriteCache SpriteCache => spriteCache.Value;

		readonly Dictionary<string, UnitSequences> sequenceCache = new Dictionary<string, UnitSequences>();

		public SequenceProvider(IReadOnlyFileSystem fileSystem, ModData modData, string tileSet, MiniYaml additionalSequences)
		{
			this.modData = modData;
			this.tileSet = tileSet;
			sequences = Exts.Lazy(() =>
			{
				using (new Support.PerfTimer("LoadSequences"))
					return Load(fileSystem, additionalSequences);
			});

			spriteCache = Exts.Lazy(() => new SpriteCache(fileSystem, modData.SpriteLoaders));
		}

		public ISpriteSequence GetSequence(string unitName, string sequenceName)
		{
			if (!sequences.Value.TryGetValue(unitName, out var unitSeq))
				throw new InvalidOperationException($"Unit `{unitName}` does not have any sequences defined.");

			if (!unitSeq.Value.TryGetValue(sequenceName, out var seq))
				throw new InvalidOperationException($"Unit `{unitName}` does not have a sequence named `{sequenceName}`");

			return seq;
		}

		public IEnumerable<string> Images => sequences.Value.Keys;

		public bool HasSequence(string unitName)
		{
			return sequences.Value.ContainsKey(unitName);
		}

		public bool HasSequence(string unitName, string sequenceName)
		{
			if (!sequences.Value.TryGetValue(unitName, out var unitSeq))
				throw new InvalidOperationException($"Unit `{unitName}` does not have any sequences defined.");

			return unitSeq.Value.ContainsKey(sequenceName);
		}

		public IEnumerable<string> Sequences(string unitName)
		{
			if (!sequences.Value.TryGetValue(unitName, out var unitSeq))
				throw new InvalidOperationException($"Unit `{unitName}` does not have any sequences defined.");

			return unitSeq.Value.Keys;
		}

		Sequences Load(IReadOnlyFileSystem fileSystem, MiniYaml additionalSequences)
		{
			var nodes = MiniYaml.Load(fileSystem, modData.Manifest.Sequences, additionalSequences);
			var items = new Dictionary<string, UnitSequences>();
			foreach (var node in nodes)
			{
				// Nodes starting with ^ are inheritable but never loaded directly
				if (node.Key.StartsWith(ActorInfo.AbstractActorPrefix, StringComparison.Ordinal))
					continue;

				var key = node.Value.ToLines(node.Key).JoinWith("|");

				if (sequenceCache.TryGetValue(key, out var t))
					items.Add(node.Key, t);
				else
				{
					t = Exts.Lazy(() => modData.SpriteSequenceLoader.ParseSequences(modData, tileSet, SpriteCache, node));
					sequenceCache.Add(key, t);
					items.Add(node.Key, t);
				}
			}

			return items;
		}

		public void Preload()
		{
			foreach (var sb in SpriteCache.SheetBuilders.Values)
				sb.Current.CreateBuffer();

			foreach (var unitSeq in sequences.Value.Values)
				foreach (var seq in unitSeq.Value.Values) { }

			foreach (var sb in SpriteCache.SheetBuilders.Values)
				sb.Current.ReleaseBuffer();
		}

		public void Dispose()
		{
			if (spriteCache.IsValueCreated)
				foreach (var sb in SpriteCache.SheetBuilders.Values)
					sb.Dispose();
		}
	}
}
