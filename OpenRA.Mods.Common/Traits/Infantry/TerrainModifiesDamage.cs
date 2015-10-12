﻿ #region Copyright & License Information
 /*
  * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
  * This file is part of OpenRA, which is free software. It is made
  * available to you under the terms of the GNU General Public License
  * as published by the Free Software Foundation. For more information,
  * see COPYING.
  */
 #endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TerrainModifiesDamageInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Damage percentage for specific terrain types. 120 = 120%, 80 = 80%, etc.")]
		public readonly Dictionary<string, int> TerrainModifier = null;

		[Desc("Modify healing damage? For example: A friendly medic.")]
		public readonly bool ModifyHealing = false;

		public object Create(ActorInitializer init) { return new TerrainModifiesDamage(init.Self, this); }
	}

	public class TerrainModifiesDamage : IDamageModifier
	{
		const int FullDamage = 100;

		public readonly TerrainModifiesDamageInfo Info;

		readonly Actor self;

		public TerrainModifiesDamage(Actor self, TerrainModifiesDamageInfo info)
		{
			Info = info;
			this.self = self;
		}

		public int GetDamageModifier(Actor attacker, IWarhead warhead)
		{
			var damageWh = warhead as DamageWarhead;
			if (attacker.Owner.IsAlliedWith(self.Owner) && (damageWh != null && damageWh.Damage < 0) && !Info.ModifyHealing)
				return FullDamage;

			var world = self.World;
			var map = world.Map;
			var tileSet = world.TileSet;

			var tiles = map.MapTiles.Value;
			var pos = map.CellContaining(self.CenterPosition);
			var terrainType = tileSet[tileSet.GetTerrainIndex(tiles[pos])].Type;

			if (!Info.TerrainModifier.ContainsKey(terrainType))
				return FullDamage;

			return Info.TerrainModifier[terrainType];
		}
	}
}
