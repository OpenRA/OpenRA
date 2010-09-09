#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public static class SequenceProvider
	{
		static Dictionary<string, Dictionary<string, Sequence>> units;

		public static void Initialize(string[] sequenceFiles)
		{
			units = new Dictionary<string, Dictionary<string, Sequence>>();
			
			var sequences = sequenceFiles
				.Select(s => MiniYaml.FromFile(s))
				.Aggregate(MiniYaml.Merge);

			foreach (var s in sequences)
				LoadSequencesForUnit(s.Key, s.Value);
		}

		static void LoadSequencesForUnit(string unit, MiniYaml sequences)
		{
			Game.modData.LoadScreen.Display();
			try {
				var seq = sequences.NodesDict.ToDictionary(x => x.Key, x => new Sequence(unit,x.Key,x.Value));
				units.Add(unit, seq);
			} catch (FileNotFoundException) {} // Do nothing; we can crash later if we actually wanted art	
		}

		public static MiniYaml SaveSequencesForUnit(string unitname)
		{
			var ret = new List<MiniYamlNode>();
			foreach (var s in units[unitname])
				ret.Add(new MiniYamlNode(s.Key, s.Value.Save()));
			
			return new MiniYaml(null, ret);
		}
		
		public static Sequence GetSequence(string unitName, string sequenceName)
		{
			try { return units[unitName][sequenceName]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException(
					"Unit `{0}` does not have a sequence `{1}`".F(unitName, sequenceName));
			}
		}

		public static bool HasSequence(string unit, string seq)
		{
			return units[unit].ContainsKey(seq);
		}
	}
}
