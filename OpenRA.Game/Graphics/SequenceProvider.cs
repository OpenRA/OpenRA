#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

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
		int Tick { get; }
		int ZOffset { get; }
		int ShadowStart { get; }
		int ShadowZOffset { get; }
		int[] Frames { get; }

		Sprite GetSprite(int frame);
		Sprite GetSprite(int frame, int facing);
		Sprite GetShadow(int frame, int facing);
	}

	public interface ISpriteSequenceLoader
	{
		Action<string> OnMissingSpriteError { get; set; }
		IReadOnlyDictionary<string, ISpriteSequence> ParseSequences(ModData modData, TileSet tileSet, SpriteCache cache, MiniYamlNode node);
	}

	public class SequenceProvider
	{
		readonly Lazy<Sequences> sequences;
		public readonly SpriteCache SpriteCache;

		public SequenceProvider(SequenceCache cache, Map map)
		{
			sequences = Exts.Lazy(() => cache.LoadSequences(map));
			SpriteCache = cache.SpriteCache;
		}

		public ISpriteSequence GetSequence(string unitName, string sequenceName)
		{
			UnitSequences unitSeq;
			if (!sequences.Value.TryGetValue(unitName, out unitSeq))
				throw new InvalidOperationException("Unit `{0}` does not have any sequences defined.".F(unitName));

			ISpriteSequence seq;
			if (!unitSeq.Value.TryGetValue(sequenceName, out seq))
				throw new InvalidOperationException("Unit `{0}` does not have a sequence named `{1}`".F(unitName, sequenceName));

			return seq;
		}

		public bool HasSequence(string unitName)
		{
			return sequences.Value.ContainsKey(unitName);
		}

		public bool HasSequence(string unitName, string sequenceName)
		{
			UnitSequences unitSeq;
			if (!sequences.Value.TryGetValue(unitName, out unitSeq))
				throw new InvalidOperationException("Unit `{0}` does not have any sequences defined.".F(unitName));

			return unitSeq.Value.ContainsKey(sequenceName);
		}

		public IEnumerable<string> Sequences(string unitName)
		{
			UnitSequences unitSeq;
			if (!sequences.Value.TryGetValue(unitName, out unitSeq))
				throw new InvalidOperationException("Unit `{0}` does not have any sequences defined.".F(unitName));

			return unitSeq.Value.Keys;
		}

		public void Preload()
		{
			SpriteCache.SheetBuilder.Current.CreateBuffer();
			foreach (var unitSeq in sequences.Value.Values)
				foreach (var seq in unitSeq.Value.Values) { }
			SpriteCache.SheetBuilder.Current.ReleaseBuffer();
		}
	}

	public sealed class SequenceCache : IDisposable
	{
		readonly ModData modData;
		readonly TileSet tileSet;
		readonly Lazy<SpriteCache> spriteCache;
		public SpriteCache SpriteCache { get { return spriteCache.Value; } }

		readonly Dictionary<string, UnitSequences> sequenceCache = new Dictionary<string, UnitSequences>();

		public SequenceCache(ModData modData, TileSet tileSet)
		{
			this.modData = modData;
			this.tileSet = tileSet;

			// Every time we load a tile set, we create a sequence cache for it
			spriteCache = Exts.Lazy(() => new SpriteCache(modData.SpriteLoaders, new SheetBuilder(SheetType.Indexed)));
		}

		public Sequences LoadSequences(Map map)
		{
			using (new Support.PerfTimer("LoadSequences"))
				return Load(map != null ? map.SequenceDefinitions : new List<MiniYamlNode>());
		}

		Sequences Load(List<MiniYamlNode> sequenceNodes)
		{
			var sequenceFiles = modData.Manifest.Sequences;

			var partial = sequenceFiles
				.Select(s => MiniYaml.FromFile(s))
				.Aggregate(sequenceNodes, MiniYaml.MergePartial);

			var nodes = MiniYaml.ApplyRemovals(partial);
			var items = new Dictionary<string, UnitSequences>();
			foreach (var n in nodes)
			{
				// Work around the loop closure issue in older versions of C#
				var node = n;

				var key = node.Value.ToLines(node.Key).JoinWith("|");

				UnitSequences t;
				if (sequenceCache.TryGetValue(key, out t))
					items.Add(node.Key, t);
				else
				{
					t = Exts.Lazy(() => modData.SpriteSequenceLoader.ParseSequences(modData, tileSet, SpriteCache, node));
					sequenceCache.Add(key, t);
					items.Add(node.Key, t);
				}
			}

			return new ReadOnlyDictionary<string, UnitSequences>(items);
		}

		public void Dispose()
		{
			if (spriteCache.IsValueCreated)
				spriteCache.Value.SheetBuilder.Dispose();
		}
	}
}
