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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CheckPlayers : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			var playerNames = map.Players.Values.Select(p => p.Name);
			foreach (var player in map.Players)
				foreach (var ally in player.Value.Allies)
					if (!playerNames.Contains(ally))
						emitError("Allies contains player {0} that is not in list.".F(ally));

			foreach (var player in map.Players)
				foreach (var enemy in player.Value.Enemies)
					if (!playerNames.Contains(enemy))
						emitError("Enemies contains player {0} that is not in list.".F(enemy));

			var races = map.Rules.Actors["world"].Traits.WithInterface<CountryInfo>().Select(c => c.Race);
			foreach (var player in map.Players)
				if (!string.IsNullOrWhiteSpace(player.Value.Race) && player.Value.Race != "Random" && !races.Contains(player.Value.Race))
					emitError("Invalid race {0} chosen for player {1}.".F(player.Value.Race, player.Value.Name));
		}
	}
}

