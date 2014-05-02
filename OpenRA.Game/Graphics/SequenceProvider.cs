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
	public static class SequenceProvider
	{
		public static Sequence GetSequence(string unitName, string sequenceName)
		{
			return Game.modData.SequenceProvider.GetSequence(unitName, sequenceName);
		}

		public static bool HasSequence(string unitName, string sequenceName)
		{
			return Game.modData.SequenceProvider.HasSequence(unitName, sequenceName);
		}

		public static IEnumerable<string> Sequences(string unitName)
		{
			return Game.modData.SequenceProvider.Sequences(unitName);
		}
	}

	public class ModSequenceProvider
	{
		readonly ModData modData;

		readonly Dictionary<string, Lazy<Dictionary<string, Sequence>>> sequenceCache = new Dictionary<string, Lazy<Dictionary<string, Sequence>>>();
		Dictionary<string, Lazy<Dictionary<string, Sequence>>> sequences;

		public ModSequenceProvider(ModData modData)
		{
			this.modData = modData;
		}

		public void ActivateMap(Map map)
		{
			sequences = Load(modData.Manifest.Sequences, map.Tileset, map.Sequences);
		}

		public Dictionary<string, Lazy<Dictionary<string, Sequence>>> Load(string[] sequenceFiles, string tileset, List<MiniYamlNode> sequenceNodes)
		{
			Game.modData.LoadScreen.Display();

			var nodes = sequenceFiles
				.Select(s => MiniYaml.FromFile(s))
				.Aggregate(sequenceNodes, MiniYaml.MergeLiberal);

			var items = new Dictionary<string, Lazy<Dictionary<string, Sequence>>>();
			foreach (var node in nodes)
			{
				// Sequence loading uses the active SpriteLoader that depends on the current map's tileset

				var key = tileset + node.Value.ToLines(node.Key).JoinWith("|");

				Lazy<Dictionary<string, Sequence>> t;
				if (sequenceCache.TryGetValue(key, out t))
				{
					items.Add(node.Key, t);
				}
				else
				{
					t = Exts.Lazy(() => node.Value.NodesDict.ToDictionary(x => x.Key, x => new Sequence(node.Key, x.Key, x.Value)));
					sequenceCache.Add(key, t);
					items.Add(node.Key, t);
				}
			}

			return items;
		}

		public Sequence GetSequence(string unitName, string sequenceName)
		{
			try { return sequences[unitName].Value[sequenceName]; }
			catch (KeyNotFoundException)
			{
				if (sequences.ContainsKey(unitName))
					throw new InvalidOperationException(
						"Unit `{0}` does not have a sequence `{1}`".F(unitName, sequenceName));
				else
					throw new InvalidOperationException(
						"Unit `{0}` does not have all sequences defined.".F(unitName));
			}
		}

		public bool HasSequence(string unitName, string sequenceName)
		{
			if (!sequences.ContainsKey(unitName))
				throw new InvalidOperationException(
					"Unit `{0}` does not have sequence `{1}` defined.".F(unitName, sequenceName));

			return sequences[unitName].Value.ContainsKey(sequenceName);
		}

		public IEnumerable<string> Sequences(string unitName)
		{
			if (!sequences.ContainsKey(unitName))
				throw new InvalidOperationException(
					"Unit `{0}` does not have all sequences defined.".F(unitName));

			return sequences[unitName].Value.Keys;
		}
	}
}
