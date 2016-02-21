#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA
{
	public class MapPlayers
	{
		public readonly Dictionary<string, PlayerReference> Players;

		public MapPlayers() : this(new List<MiniYamlNode>()) { }

		public MapPlayers(IEnumerable<MiniYamlNode> playerDefinitions)
		{
			Players = playerDefinitions.Select(pr => new PlayerReference(new MiniYaml(pr.Key, pr.Value.Nodes)))
				.ToDictionary(player => player.Name);
		}

		public MapPlayers(Ruleset rules, int playerCount)
		{
			var firstFaction = rules.Actors["world"].TraitInfos<FactionInfo>()
				.First(f => f.Selectable).InternalName;

			Players = new Dictionary<string, PlayerReference>
			{
				{
					"Neutral", new PlayerReference
					{
						Name = "Neutral",
						Faction = firstFaction,
						OwnsWorld = true,
						NonCombatant = true
					}
				},
				{
					"Creeps", new PlayerReference
					{
						Name = "Creeps",
						Faction = firstFaction,
						NonCombatant = true,
						Enemies = Exts.MakeArray(playerCount, i => "Multi{0}".F(i))
					}
				}
			};

			for (var index = 0; index < playerCount; index++)
			{
				var p = new PlayerReference
				{
					Name = "Multi{0}".F(index),
					Faction = "Random",
					Playable = true,
					Enemies = new[] { "Creeps" }
				};
				Players.Add(p.Name, p);
			}
		}

		public List<MiniYamlNode> ToMiniYaml()
		{
			return Players.Select(p => new MiniYamlNode("PlayerReference@{0}".F(p.Key),
				FieldSaver.SaveDifferences(p.Value, new PlayerReference()))).ToList();
		}
	}
}
