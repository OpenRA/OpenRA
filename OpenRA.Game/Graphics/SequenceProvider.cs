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

		public SequenceProvider(Map map)
		{
			this.sequences = Exts.Lazy(() => map.Rules.TileSets[map.Tileset].Data.SequenceCache.LoadSequences(map));
		}

		public Sequence GetSequence(string unitName, string sequenceName)
		{
			try
			{
				return sequences.Value[unitName].Value[sequenceName];
			}
			catch (KeyNotFoundException)
			{
				if (sequences.Value.ContainsKey(unitName))
					throw new InvalidOperationException("Unit `{0}` does not have a sequence `{1}`".F(unitName, sequenceName));
				else
					throw new InvalidOperationException("Unit `{0}` does not have all sequences defined.".F(unitName));
			}
		}

		public bool HasSequence(string unitName, string sequenceName)
		{
			if (!sequences.Value.ContainsKey(unitName))
				throw new InvalidOperationException("Unit `{0}` does not have sequence `{1}` defined.".F(unitName, sequenceName));

			return sequences.Value[unitName].Value.ContainsKey(sequenceName);
		}

		public IEnumerable<string> Sequences(string unitName)
		{
			if (!sequences.Value.ContainsKey(unitName))
				throw new InvalidOperationException("Unit `{0}` does not have all sequences defined.".F(unitName));

			return sequences.Value[unitName].Value.Keys;
		}
	}

	public class SequenceCache
	{
		readonly ModData modData;
		readonly TileSet tileSet;

		readonly Dictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>> sequenceCache = new Dictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>>();

		public Action OnProgress = () => { if (Game.modData != null && Game.modData.LoadScreen != null) Game.modData.LoadScreen.Display(); };

		public SequenceCache(ModData modData, TileSet tileSet)
		{
			this.modData = modData;
			this.tileSet = tileSet;
		}

		public IReadOnlyDictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>> LoadSequences(Map map)
		{
			using (new Support.PerfTimer("LoadSequences"))
				return Load(map.SequenceDefinitions);
		}

		IReadOnlyDictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>> Load(List<MiniYamlNode> sequenceNodes)
		{
			OnProgress();

			var sequenceFiles = modData.Manifest.Sequences;

			var nodes = sequenceFiles
				.Select(s => MiniYaml.FromFile(s))
				.Aggregate(sequenceNodes, MiniYaml.MergeLiberal);

			var items = new Dictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>>();
			foreach (var node in nodes)
			{
				var key = node.Value.ToLines(node.Key).JoinWith("|");

				Lazy<IReadOnlyDictionary<string, Sequence>> t;
				if (sequenceCache.TryGetValue(key, out t))
				{
					items.Add(node.Key, t);
				}
				else
				{
					t = Exts.Lazy(() => (IReadOnlyDictionary<string, Sequence>)new ReadOnlyDictionary<string, Sequence>(
						node.Value.NodesDict.ToDictionary(x => x.Key, x => 
							new Sequence(tileSet.Data.SpriteLoader, node.Key, x.Key, x.Value))));
					sequenceCache.Add(key, t);
					items.Add(node.Key, t);
				}

				OnProgress();
			}

			return new ReadOnlyDictionary<string, Lazy<IReadOnlyDictionary<string, Sequence>>>(items);
		}
	}
}
