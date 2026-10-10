#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class MapPlayersTest
	{
		[TestCase(TestName = "Players keep their definition order when removed and re-added")]
		public void PlayersKeepDefinitionOrder()
		{
			var players = new MapPlayers();
			string[] names = ["Neutral", "Creeps", "Multi0", "Multi1", "Multi2"];
			foreach (var name in names)
				players.Players.Add(name, null);

			// The map editor removes and re-adds all spawn players when updating multiple spawns at once.
			for (var i = 2; i < names.Length; i++)
				players.Players.Remove(names[i]);

			for (var i = 2; i < names.Length; i++)
				players.Players.Add(names[i], null);

			Assert.That(players.Players.Keys, Is.EqualTo(names));
		}
	}
}
