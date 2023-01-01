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

using System;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor receives damage from the given weapon when on the specified terrain type.")]
	public class DamagedByTerrainInfo : ConditionalTraitInfo, Requires<IHealthInfo>, Requires<IOccupySpaceInfo>
	{
		[FieldLoader.Require]
		[Desc("Amount of damage received per DamageInterval ticks.")]
		public readonly int Damage = 0;

		[Desc("Delay between receiving damage.")]
		public readonly int DamageInterval = 0;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[FieldLoader.Require]
		[Desc("Terrain types where the actor will take damage.")]
		public readonly string[] Terrain = Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new DamagedByTerrain(this); }
	}

	public class DamagedByTerrain : ConditionalTrait<DamagedByTerrainInfo>, ITick, ISync
	{
		int damageTicks;

		public DamagedByTerrain(DamagedByTerrainInfo info)
			: base(info) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || --damageTicks > 0)
				return;

			// Prevents harming cargo.
			if (!self.IsInWorld)
				return;

			var t = self.World.Map.GetTerrainInfo(self.Location);
			if (!Info.Terrain.Contains(t.Type))
				return;

			self.InflictDamage(self.World.WorldActor, new Damage(Info.Damage, Info.DamageTypes));
			damageTicks = Info.DamageInterval;
		}
	}
}
