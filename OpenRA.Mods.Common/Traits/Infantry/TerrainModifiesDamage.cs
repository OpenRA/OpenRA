 #region Copyright & License Information
 /*
  * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
  * This file is part of OpenRA, which is free software. It is made
  * available to you under the terms of the GNU General Public License
  * as published by the Free Software Foundation. For more information,
  * see COPYING.
  */
 #endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TerrainModifiesDamageInfo : ITraitInfo
	{
		[FieldLoader.LoadUsing("LoadPercents")]
		[Desc("Damage percentage for specific terrain types. 120 = 120%, 80 = 80%, etc.")]
		public readonly Dictionary<string, int> TerrainModifier = null;

		[Desc("Modify healing damage? For example: A friendly medic.")]
		public readonly bool ModifyHealing = false;

		public object Create(ActorInitializer init) { return new TerrainModifiesDamage(init.Self, this); }

		static object LoadPercents(MiniYaml y)
		{
			MiniYaml percents;

			if (!y.ToDictionary().TryGetValue("TerrainModifier", out percents))
				return new Dictionary<string, int>();

			return percents.Nodes.ToDictionary(
				kv => FieldLoader.GetValue<string>("(key)", kv.Key),
				kv => FieldLoader.GetValue<int>("(value)", kv.Value.Value));
		}
	}

	public class TerrainModifiesDamage : IDamageModifier
	{
		public readonly TerrainModifiesDamageInfo Info;

		readonly Actor self;

		public TerrainModifiesDamage(Actor self, TerrainModifiesDamageInfo info)
		{
			Info = info;
			this.self = self;
		}

		public int GetDamageModifier(Actor attacker, DamageWarhead warhead)
		{
			var percent = 100;
			if (attacker.Owner.IsAlliedWith(self.Owner) && warhead.Damage < 0 && !Info.ModifyHealing)
				return percent;

			var world = self.World;
			var map = world.Map;
			var tileSet = world.TileSet;

			var tiles = map.MapTiles.Value;
			var pos = map.CellContaining(self.CenterPosition);
			var terrainType = tileSet[tileSet.GetTerrainIndex(tiles[pos])].Type;

			if (!Info.TerrainModifier.ContainsKey(terrainType))
				return percent;

			return Info.TerrainModifier[terrainType];
		}
	}
}
