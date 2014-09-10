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
	[Desc("Creates duplicates of the actor that collects the crate.")]
	class DuplicateUnitCrateActionInfo : CrateActionInfo
	{
		[Desc("The maximum number of duplicates to make.")]
		public readonly int MaxAmount = 2;

		[Desc("The minimum number of duplicates to make.","Overrules MaxDuplicatesWorth.")]
		public readonly int MinAmount = 1;

		[Desc("The maximum total cost allowed for the duplicates.","Duplication stops if the total worth will exceed this number.","-1 = no limit")]
		public readonly int MaxDuplicatesWorth = -1;

		[Desc("The list of unit types we are allowed to duplicate.")]
		public readonly string[] ValidDuplicateTypes = { "Ground", "Water" };

		[Desc("Which races this crate action can occur for.")]
		public readonly string[] ValidRaces = { };

		[Desc("Is the new duplicates given to a specific owner, regardless of whom collected it?")]
		public readonly string Owner = null;

		public override object Create(ActorInitializer init) { return new DuplicateUnitCrateAction(init.self, this); }
	}

	class DuplicateUnitCrateAction : CrateAction
	{
		public readonly DuplicateUnitCrateActionInfo Info;
		readonly List<CPos> usedCells = new List<CPos>();

		public DuplicateUnitCrateAction(Actor self, DuplicateUnitCrateActionInfo info)
			: base(self, info) { Info = info; }

		public bool CanGiveTo(Actor collector)
		{
			if (Info.ValidRaces.Any() && !Info.ValidRaces.Contains(collector.Owner.Country.Race))
				return false;

			var targetable = collector.Info.Traits.GetOrDefault<ITargetableInfo>();
			if (targetable == null ||
				!Info.ValidDuplicateTypes.Intersect(targetable.GetTargetTypes()).Any())
				return false;

			if (!GetSuitableCells(collector.Location, collector.Info.Name).Any()) return false;

			return true;
		}

		public override int GetSelectionShares(Actor collector)
		{
			if (!CanGiveTo(collector)) return 0;
			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			var allowedWorthLeft = Info.MaxDuplicatesWorth;
			var dupesMade = 0;

			while ((dupesMade < Info.MaxAmount && allowedWorthLeft > 0) || dupesMade < Info.MinAmount)
			{
				//If the collector has a cost, and we have a max duplicate worth, then update how much dupe worth is left
				var unitCost = collector.Info.Traits.Get<ValuedInfo>().Cost;
				allowedWorthLeft -= Info.MaxDuplicatesWorth > 0 ? unitCost : 0;
				if (allowedWorthLeft < 0 && dupesMade >= Info.MinAmount)
					break;

				dupesMade++;

				var location = ChooseEmptyCellNear(collector, collector.Info.Name);
				if (location != null)
				{
					usedCells.Add(location.Value);
					collector.World.AddFrameEndTask(
					w => w.CreateActor(collector.Info.Name, new TypeDictionary
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

			for (var i = -3; i < 4; i++)
				for (var j = -3; j < 4; j++)
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
