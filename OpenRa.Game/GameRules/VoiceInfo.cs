#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using OpenRa.FileFormats;

namespace OpenRa.GameRules
{
	public class VoiceInfo
	{
		public readonly string[] SovietVariants = { ".aud" };
		public readonly string[] AlliedVariants = { ".aud" };
		public readonly string[] Select = { };
		public readonly string[] Move = { };
		public readonly string[] Attack = null;
		public readonly string[] Die = { };

		public readonly Lazy<Dictionary<string, VoicePool>> Pools;

		public VoiceInfo()
		{
			Pools = Lazy.New(() =>
				new Dictionary<string, VoicePool>
				{
					{ "Select", new VoicePool(Select) },
					{ "Move", new VoicePool(Move) },
					{ "Attack", new VoicePool( Attack ?? Move ) },
					{ "Die", new VoicePool(Die) },
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

			var i = Game.world.CosmeticRandom.Next(liveclips.Count);
			var s = liveclips[i];
			liveclips.RemoveAt(i);
			return s;
		}
	}
}
