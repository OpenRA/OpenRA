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
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	public class VoiceInfo
	{
		[FieldLoader.Ignore] public readonly Dictionary<string,string[]> Variants;
		[FieldLoader.Ignore] public readonly Dictionary<string,string[]> Voices;
		public readonly string DefaultVariant = ".aud" ;
		public readonly string[] DisableVariants = { };

		static Dictionary<string, string[]> Load( MiniYaml y, string name )
		{
			return y.NodesDict.ContainsKey( name )
				? y.NodesDict[ name ].NodesDict.ToDictionary(
					a => a.Key,
					a => FieldLoader.GetValue<string[]>( "(value)", a.Value.Value ) )
				: new Dictionary<string, string[]>();
		}

		public readonly OpenRA.FileFormats.Lazy<Dictionary<string, VoicePool>> Pools;

		public VoiceInfo( MiniYaml y )
		{
			FieldLoader.Load( this, y );
			Variants = Load(y, "Variants");
			Voices = Load(y, "Voices");

			if (!Voices.ContainsKey("Attack"))
				Voices.Add("Attack", Voices["Move"]);

			if (!Voices.ContainsKey("AttackMove"))
				Voices.Add("AttackMove", Voices["Move"]);

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
