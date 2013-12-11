#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Graphics
{
	public static class SequenceProvider
	{
		static Dictionary<string, Dictionary<string, Sequence>> units;

		public static void Initialize(string[] sequenceFiles, List<MiniYamlNode> sequenceNodes)
		{
			units = new Dictionary<string, Dictionary<string, Sequence>>();

			var sequences = sequenceFiles
				.Select(s => MiniYaml.FromFile(s))
				.Aggregate(sequenceNodes, MiniYaml.MergeLiberal);

			foreach (var s in sequences)
				LoadSequencesForUnit(s.Key, s.Value);
		}

		static void LoadSequencesForUnit(string unit, MiniYaml sequences)
		{
			Game.modData.LoadScreen.Display();
			try
			{
				var seq = sequences.NodesDict.ToDictionary(x => x.Key, x => new Sequence(unit,x.Key,x.Value));
				units.Add(unit, seq);
			}
			catch (FileNotFoundException)
			{
				// Do nothing; we can crash later if we actually wanted art
			} 
		}

		public static Sequence GetSequence(string unitName, string sequenceName)
		{
			try { return units[unitName][sequenceName]; }
			catch (KeyNotFoundException)
			{
				if (units.ContainsKey(unitName))
					throw new InvalidOperationException(
						"Unit `{0}` does not have a sequence `{1}`".F(unitName, sequenceName));
				else
					throw new InvalidOperationException(
						"Unit `{0}` does not have all sequences defined.".F(unitName));
			}
		}

		public static bool HasSequence(string unit, string seq)
		{
			if (!units.ContainsKey(unit))
				throw new InvalidOperationException(
					"Unit `{0}` does not have sequence `{1}` defined.".F(unit, seq));

			return units[unit].ContainsKey(seq);
		}

		public static IEnumerable<string> Sequences(string unit)
		{
			if (!units.ContainsKey(unit))
				throw new InvalidOperationException(
					"Unit `{0}` does not have all sequences defined.".F(unit));

			return units[unit].Keys;
		}
	}
}
