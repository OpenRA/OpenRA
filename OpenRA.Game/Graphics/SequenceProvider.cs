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
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class SequenceProvider
	{
		readonly Lazy<IReadOnlyDictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>>> sequences;
		public readonly SpriteLoader SpriteLoader;

		public SequenceProvider(SequenceCache cache, Map map)
		{
			this.sequences = Exts.Lazy(() => cache.LoadSequences(map));
			this.SpriteLoader = cache.SpriteLoader;
		}

		public Sequence GetSequence(string unitName, string sequenceName)
		{
			Lazy<IReadOnlyDictionary<string, Sequence>> unitSeq;
			if (!sequences.Value.TryGetValue(unitName, out unitSeq))
				throw new InvalidOperationException("Unit `{0}` does not have any sequences defined.".F(unitName));

			Sequence seq;
			if (!unitSeq.Value.TryGetValue(sequenceName, out seq))
				throw new InvalidOperationException("Unit `{0}` does not have a sequence named `{1}`".F(unitName, sequenceName));

			return seq;
		}

		public bool HasSequence(string unitName, string sequenceName)
		{
			Lazy<IReadOnlyDictionary<string, Sequence>> unitSeq;
			if (!sequences.Value.TryGetValue(unitName, out unitSeq))
				throw new InvalidOperationException("Unit `{0}` does not have any sequences defined.".F(unitName));

			return unitSeq.Value.ContainsKey(sequenceName);
		}

		public IEnumerable<string> Sequences(string unitName)
		{
			Lazy<IReadOnlyDictionary<string, Sequence>> unitSeq;
			if (!sequences.Value.TryGetValue(unitName, out unitSeq))
				throw new InvalidOperationException("Unit `{0}` does not have any sequences defined.".F(unitName));

			return unitSeq.Value.Keys;
		}

		public void Preload()
		{
			foreach (var unitSeq in sequences.Value.Values)
			{
				try
				{
					foreach (var seq in unitSeq.Value.Values);
				}
				catch (FileNotFoundException ex)
				{
					Log.Write("debug", ex.Message);
				}
			}
		}
	}

	public class SequenceCache
	{
		readonly ModData modData;
		readonly Lazy<SpriteLoader> spriteLoader;
		public SpriteLoader SpriteLoader { get { return spriteLoader.Value; } }

		readonly Dictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>> sequenceCache = new Dictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>>();

		public SequenceCache(ModData modData, TileSet tileSet)
		{
			this.modData = modData;

			spriteLoader = Exts.Lazy(() => new SpriteLoader(tileSet.Extensions, new SheetBuilder(SheetType.Indexed)));
		}

		public IReadOnlyDictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>> LoadSequences(Map map)
		{
			using (new Support.PerfTimer("LoadSequences"))
				return Load(map.SequenceDefinitions);
		}

		IReadOnlyDictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>> Load(List<MiniYamlNode> sequenceNodes)
		{
			var sequenceFiles = modData.Manifest.Sequences;

			var nodes = sequenceFiles
				.Select(s => MiniYaml.FromFile(s))
				.Aggregate(sequenceNodes, MiniYaml.MergeLiberal);

			var items = new Dictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>>();
			foreach (var n in nodes)
			{
				// Work around the loop closure issue in older versions of C#
				var node = n;

				var key = node.Value.ToLines(node.Key).JoinWith("|");

				Lazy<IReadOnlyDictionary<string, Sequence>> t;
				if (sequenceCache.TryGetValue(key, out t))
					items.Add(node.Key, t);
				else
				{
					t = Exts.Lazy(() => (IReadOnlyDictionary<string, Sequence>)new ReadOnlyDictionary<string, Sequence>(
						node.Value.ToDictionary().ToDictionary(x => x.Key, x => 
						{
							using (new Support.PerfTimer("new Sequence(\"{0}\")".F(node.Key), 20))
								return new Sequence(spriteLoader.Value, node.Key, x.Key, x.Value);
						})));
					sequenceCache.Add(key, t);
					items.Add(node.Key, t);
				}
			}

			return new ReadOnlyDictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>>(items);
		}
	}
}
