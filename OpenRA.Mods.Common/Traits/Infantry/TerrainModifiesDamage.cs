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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TerrainModifiesDamageInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Damage percentage for specific terrain types. 120 = 120%, 80 = 80%, etc.")]
		public readonly Dictionary<string, int> TerrainModifier = null;

		[Desc("Modify healing damage? For example: A friendly medic.")]
		public readonly bool ModifyHealing = false;

		public override object Create(ActorInitializer init) { return new TerrainModifiesDamage(init.Self, this); }
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

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			if (!Info.ModifyHealing && attacker.Owner.IsAlliedWith(self.Owner) && damage != null && damage.Value < 0)
				return FullDamage;

			var world = self.World;
			var map = world.Map;

			var pos = map.CellContaining(self.CenterPosition);
			var terrainType = map.GetTerrainInfo(pos).Type;

			if (!Info.TerrainModifier.ContainsKey(terrainType))
				return FullDamage;

			return Info.TerrainModifier[terrainType];
		}
	}
}
