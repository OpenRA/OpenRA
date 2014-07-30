#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Crates
{
	[Desc("Spawns units when collected.")]
	class GiveUnitCrateActionInfo : CrateActionInfo
	{
		[Desc("The list of units to spawn.")]
		[ActorReference]
		public readonly string[] Units = { };

		[Desc("Races that are allowed to trigger this action")]
		public readonly string[] ValidRaces = { };

		[Desc("Override the owner of the newly spawned unit: e.g. Creeps or Neutral")]
		public readonly string Owner = null;

		public override object Create(ActorInitializer init) { return new GiveUnitCrateAction(init.self, this); }
	}

	class GiveUnitCrateAction : CrateAction
	{
		public readonly GiveUnitCrateActionInfo Info;
		readonly List<CPos> usedCells = new List<CPos>();

		public GiveUnitCrateAction(Actor self, GiveUnitCrateActionInfo info)
			: base(self, info)
		{
			Info = info;
			if (!Info.Units.Any())
				throw new YamlException("A GiveUnitCrateAction does not specify any units to give. This might be because the yaml is referring to 'Unit' rather than 'Units'.");
		}

		public bool CanGiveTo(Actor collector)
		{
			if (Info.ValidRaces.Any() && !Info.ValidRaces.Contains(collector.Owner.Country.Race))
				return false;

			foreach (string unit in Info.Units)
			{
				// avoid dumping tanks in the sea, and ships on dry land.
				if (!GetSuitableCells(collector.Location, unit).Any()) return false;
			}

			return true;
		}

		public override int GetSelectionShares(Actor collector)
		{
			if (!CanGiveTo(collector)) return 0;
			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			foreach (var u in Info.Units)
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
						new OwnerInit(Info.Owner ?? collector.Owner.InternalName)
					}));
				}
			}
			base.Activate(collector);
		}

		IEnumerable<CPos> GetSuitableCells(CPos near, string unitName)
		{
			var mi = self.World.Map.Rules.Actors[unitName].Traits.Get<MobileInfo>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if (mi.CanEnterCell(self.World, self, near + new CVec(i, j)))
						yield return near + new CVec(i, j);
		}

		CPos? ChooseEmptyCellNear(Actor a, string unit)
		{
			var possibleCells = GetSuitableCells(a.Location, unit).Where(c => !usedCells.Contains(c)).ToArray();
			if (possibleCells.Length == 0)
				return null;

			return possibleCells.Random(self.World.SharedRandom);
		}
	}
}
