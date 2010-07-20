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
using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	public class VoiceInfo
	{
		public readonly string[] SovietVariants = { ".aud" };
		public readonly string[] AlliedVariants = { ".aud" };
		public readonly string[] Select = { };
		public readonly string[] Move = { };
		public readonly string[] Attack = null;
		public readonly string[] Die = { };
		public readonly string[] Lost = { };

		public readonly Lazy<Dictionary<string, VoicePool>> Pools;

		public VoiceInfo( MiniYaml y )
		{
			FieldLoader.Load(this, y);

			Pools = Lazy.New(() =>
				new Dictionary<string, VoicePool>
				{
					{ "Select", new VoicePool(Select) },
					{ "Move", new VoicePool(Move) },
					{ "Attack", new VoicePool( Attack ?? Move ) },
					{ "Die", new VoicePool(Die) },
					{ "Lost", new VoicePool(Lost) },
				});
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
