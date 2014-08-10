#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Graphics
{
	using Sequences = IReadOnlyDictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>>;
	using UnitSequences = Lazy<IReadOnlyDictionary<string, Sequence>>;

	public class SequenceProvider
	{
		readonly Lazy<Sequences> sequences;
		public readonly SpriteLoader SpriteLoader;

		public SequenceProvider(SequenceCache cache, Map map)
		{
			this.sequences = Exts.Lazy(() => cache.LoadSequences(map));
			this.SpriteLoader = cache.SpriteLoader;
		}

		public Sequence GetSequence(string unitName, string sequenceName)
		{
			UnitSequences unitSeq;
			if (!sequences.Value.TryGetValue(unitName, out unitSeq))
				throw new InvalidOperationException("Unit `{0}` does not have any sequences defined.".F(unitName));

			Sequence seq;
			if (!unitSeq.Value.TryGetValue(sequenceName, out seq))
				throw new InvalidOperationException("Unit `{0}` does not have a sequence named `{1}`".F(unitName, sequenceName));

			return seq;
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
			foreach (var unitSeq in sequences.Value.Values)
				foreach (var seq in unitSeq.Value.Values) { }
		}
	}

	public sealed class SequenceCache : IDisposable
	{
		readonly ModData modData;
		readonly Lazy<SpriteLoader> spriteLoader;
		public SpriteLoader SpriteLoader { get { return spriteLoader.Value; } }

		readonly Dictionary<string, UnitSequences> sequenceCache = new Dictionary<string, UnitSequences>();

		public SequenceCache(ModData modData, TileSet tileSet)
		{
			this.modData = modData;

			spriteLoader = Exts.Lazy(() => new SpriteLoader(tileSet.Extensions, new SheetBuilder(SheetType.Indexed)));
		}

		public Sequences LoadSequences(Map map)
		{
			using (new Support.PerfTimer("LoadSequences"))
				return Load(map.SequenceDefinitions);
		}

		Sequences Load(List<MiniYamlNode> sequenceNodes)
		{
			var sequenceFiles = modData.Manifest.Sequences;

			var nodes = sequenceFiles
				.Select(s => MiniYaml.FromFile(s))
				.Aggregate(sequenceNodes, MiniYaml.MergeLiberal);

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
					t = Exts.Lazy(() => CreateUnitSequences(node));
					sequenceCache.Add(key, t);
					items.Add(node.Key, t);
				}
			}

			return new ReadOnlyDictionary<string, UnitSequences>(items);
		}

		IReadOnlyDictionary<string, Sequence> CreateUnitSequences(MiniYamlNode node)
		{
			var unitSequences = new Dictionary<string, Sequence>();

			foreach (var kvp in node.Value.ToDictionary())
			{
				using (new Support.PerfTimer("new Sequence(\"{0}\")".F(node.Key), 20))
				{
					try
					{
						unitSequences.Add(kvp.Key, new Sequence(spriteLoader.Value, node.Key, kvp.Key, kvp.Value));
					}
					catch (FileNotFoundException ex)
					{
						Log.Write("debug", ex.Message);
					}
				}
			}

			return new ReadOnlyDictionary<string, Sequence>(unitSequences);
		}

		public void Dispose()
		{
			if (spriteLoader.IsValueCreated)
				spriteLoader.Value.SheetBuilder.Dispose();
		}
	}
}
