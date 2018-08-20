#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor receives damage when in TintedCell area.")]
	class DamagedByTintedCellsInfo : ConditionalTraitInfo, Requires<HealthInfo>, IRulesetLoaded
	{
		[Desc("Receive damage from the TintedCell layer with this name.")]
		public readonly string LayerName = "radioactivity";

		[Desc("Damage received per level, per DamageInterval. (Damage = CellLevel / DamageLevel * Damage")]
		public readonly int Damage = 500;

		[Desc("How much TintedCell.Level it takes for it to inflict damage X times.")]
		public readonly int DamageLevel = 100;

		[Desc("Delay (in ticks) between receiving damage.")]
		public readonly int DamageInterval = 16;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new DamagedByTintedCells(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			base.RulesetLoaded(rules, info);

			if (DamageLevel == 0)
				throw new YamlException("DamageLevel of DamagedByTintedCells of actor \"" + info.Name + "\" cannot be 0.");

			var layers = rules.Actors["world"].TraitInfos<TintedCellsLayerInfo>()
				.Where(l => l.Name == LayerName);

			if (!layers.Any())
				throw new InvalidOperationException("There is no TintedCellsLayer named \"" + LayerName + "\" to match DamagedByTintedCells of actor \"" + info.Name + "\"");

			if (layers.Count() > 1)
				throw new InvalidOperationException("There are multiple TintedCellsLayers named \""
					+ LayerName + "\" to match DamagedByTintedCells of actor \"" + info.Name + "\"");
		}
	}

	class DamagedByTintedCells : ConditionalTrait<DamagedByTintedCellsInfo>, ITick, ISync
	{
		readonly TintedCellsLayer tcLayer;

		[Sync]
		int damageTicks;

		public DamagedByTintedCells(Actor self, DamagedByTintedCellsInfo info) : base(info)
		{
			tcLayer = self.World.WorldActor.TraitsImplementing<TintedCellsLayer>()
				.Where(l => l.Info.Name == info.LayerName)
				.First();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || --damageTicks > 0)
				return;

			// Prevents harming cargo.
			if (!self.IsInWorld)
				return;

			var level = tcLayer.GetLevel(self.Location);
			if (level <= 0)
				return;

			int dmg = level / Info.DamageLevel * Info.Damage;
			self.InflictDamage(self.World.WorldActor, new Damage(dmg, Info.DamageTypes));

			damageTicks = Info.DamageInterval;
		}
	}
}
