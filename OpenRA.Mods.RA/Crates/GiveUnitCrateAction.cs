#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Crates
{
	class GiveUnitCrateActionInfo : CrateActionInfo
	{
		[ActorReference]
		public readonly string[] Units = null;
		public readonly string Owner = null;

		public override object Create(ActorInitializer init) { return new GiveUnitCrateAction(init.self, this); }
	}

	class GiveUnitCrateAction : CrateAction
	{
		readonly GiveUnitCrateActionInfo Info;
		readonly List<CPos> usedCells = new List<CPos>();

		public GiveUnitCrateAction(Actor self, GiveUnitCrateActionInfo info)
			: base(self, info) { Info = info; }

		public override int GetSelectionShares(Actor collector)
		{
			var unit = Info.Units.First();

			// avoid dumping tanks in the sea, and ships on dry land.
			if (!GetSuitableCells(collector.Location, unit).Any()) return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			foreach (var unit in Info.Units)
			{
				var tmpUnit = unit; // avoiding access to modified closure

				var location = ChooseEmptyCellNear(collector, unit);
				if (location != null)
				{
					usedCells.Add(location.Value);
					collector.World.AddFrameEndTask(
					w => w.CreateActor(tmpUnit, new TypeDictionary
						{
							new LocationInit(location.Value),
							new OwnerInit(Info.Owner ?? collector.Owner.InternalName)
						}));
				}
			}

		    base.Activate(collector);
		}

		IEnumerable<CPos> GetSuitableCells(CPos near, string unit)
		{
			var mi = Rules.Info[unit].Traits.Get<MobileInfo>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if (mi.CanEnterCell(self.World, self, near + new CVec(i, j), null, true, true))
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
