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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawns units when collected.")]
	class GiveUnitCrateActionInfo : CrateActionInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("The list of units to spawn.")]
		public readonly string[] Units = Array.Empty<string>();

		[Desc("Factions that are allowed to trigger this action.")]
		public readonly HashSet<string> ValidFactions = new HashSet<string>();

		[Desc("Override the owner of the newly spawned unit: e.g. Creeps or Neutral")]
		public readonly string Owner = null;

		public override object Create(ActorInitializer init) { return new GiveUnitCrateAction(init.Self, this); }
	}

	class GiveUnitCrateAction : CrateAction
	{
		readonly Actor self;
		readonly GiveUnitCrateActionInfo info;
		readonly List<CPos> usedCells = new List<CPos>();

		public GiveUnitCrateAction(Actor self, GiveUnitCrateActionInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;
			if (info.Units.Length == 0)
				throw new YamlException("A GiveUnitCrateAction does not specify any units to give. This might be because the yaml is referring to 'Unit' rather than 'Units'.");
		}

		public bool CanGiveTo(Actor collector)
		{
			if (collector.Owner.NonCombatant)
				return false;

			if (info.ValidFactions.Count > 0 && !info.ValidFactions.Contains(collector.Owner.Faction.InternalName))
				return false;

			foreach (var unit in info.Units)
			{
				// avoid dumping tanks in the sea, and ships on dry land.
				if (!GetSuitableCells(collector.Location, unit).Any())
					return false;
			}

			return true;
		}

		public override int GetSelectionShares(Actor collector)
		{
			if (!CanGiveTo(collector))
				return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			foreach (var u in info.Units)
			{
				var unit = u; // avoiding access to modified closure

				var location = ChooseEmptyCellNear(collector, unit);
				if (location != null)
				{
					usedCells.Add(location.Value);
					collector.World.AddFrameEndTask(
					w => w.CreateActor(unit, new TypeDictionary
					{
						new LocationInit(location.Value),
						new OwnerInit(info.Owner ?? collector.Owner.InternalName)
					}));
				}
			}

			base.Activate(collector);
		}

		IEnumerable<CPos> GetSuitableCells(CPos near, string unitName)
		{
			var ip = self.World.Map.Rules.Actors[unitName].TraitInfo<IPositionableInfo>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if (ip.CanEnterCell(self.World, self, near + new CVec(i, j)))
						yield return near + new CVec(i, j);
		}

		CPos? ChooseEmptyCellNear(Actor a, string unit)
		{
			var possibleCells = GetSuitableCells(a.Location, unit).Where(c => !usedCells.Contains(c)).ToList();
			if (possibleCells.Count == 0)
				return null;

			return possibleCells.Random(self.World.SharedRandom);
		}
	}
}
