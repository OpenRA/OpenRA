#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using System;

namespace OpenRA.GameRules
{
	public class VoiceInfo
	{
		public readonly Dictionary<string,string[]> Variants;
		public readonly Dictionary<string,string[]> Voices;
		public readonly string DefaultVariant = ".aud" ;
		public readonly string[] DisableVariants = { };
		
		Func<MiniYaml, string, Dictionary<string, string[]>> Load = (y,name) => (y.Nodes.ContainsKey(name))? y.Nodes[name].Nodes.ToDictionary(a => a.Key, 
			                           a => (string[])FieldLoader.GetValue( "(value)", typeof(string[]), a.Value.Value ))
						: new Dictionary<string, string[]>(); 

		public readonly Lazy<Dictionary<string, VoicePool>> Pools;

		public VoiceInfo( MiniYaml y )
		{
			FieldLoader.LoadFields(this, y.Nodes, new string[] { "DisableVariants" });
			Variants = Load(y, "Variants"); 
			Voices = Load(y, "Voices");
			
			if (!Voices.ContainsKey("Attack"))
				Voices.Add("Attack", Voices["Move"]);
			
			Pools = Lazy.New(() => Voices.ToDictionary( a => a.Key, a => new VoicePool(a.Value) ));
		}
	}

	public class VoicePool
	{
		readonly string[] clips;
		readonly List<string> liveclips = new List<string>();

		public VoicePool(params string[] clips)
		{
			this.clips = clips;
		}

		public string GetNext()
		{
			if (liveclips.Count == 0)
				liveclips.AddRange(clips);

			if (liveclips.Count == 0)
				return null;		/* avoid crashing if there's no clips at all */

			var i = Game.CosmeticRandom.Next(liveclips.Count);
			var s = liveclips[i];
			liveclips.RemoveAt(i);
			return s;
		}
	}
}
