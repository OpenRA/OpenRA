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
	class GiveUnitCrateActionInfo : CrateActionInfo
	{
		[ActorReference]
		[Desc("Unit to give")]
		public readonly string Unit = null;

		[Desc("Override the owner of the newly spawned unit: e.g Creeps or Neutral")]
		public readonly string Owner = null;

		[Desc("Races that are allowed to trigger this action")]
		public readonly string[] Race = null;

		public override object Create(ActorInitializer init) { return new GiveUnitCrateAction(init.self, this); }
	}

	class GiveUnitCrateAction : CrateAction
	{
		GiveUnitCrateActionInfo Info;

		public GiveUnitCrateAction(Actor self, GiveUnitCrateActionInfo info)
			: base(self, info) { Info = info; }

		public bool CanGiveTo(Actor collector)
		{
			if (Info.Race != null && !Info.Race.Contains(collector.Owner.Country.Race))
				return false;

			// avoid dumping tanks in the sea, and ships on dry land.
			if (!GetSuitableCells(collector.Location).Any())
				return false;

			return true;
		}

		public override int GetSelectionShares(Actor collector)
		{
			if (!CanGiveTo(collector)) return 0;
			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			var location = ChooseEmptyCellNear(collector);
			if (location != null)
				collector.World.AddFrameEndTask(
					w => w.CreateActor(Info.Unit, new TypeDictionary
					{
						new LocationInit(location.Value ),
						new OwnerInit(Info.Owner ?? collector.Owner.InternalName)
					}));

			base.Activate(collector);
		}

		IEnumerable<CPos> GetSuitableCells(CPos near)
		{
			var mi = self.World.Map.Rules.Actors[Info.Unit].Traits.Get<MobileInfo>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if (mi.CanEnterCell(self.World, self, near + new CVec(i, j), null, true, true))
						yield return near + new CVec(i, j);
		}

		CPos? ChooseEmptyCellNear(Actor a)
		{
			var possibleCells = GetSuitableCells(a.Location).ToArray();
			if (possibleCells.Length == 0)
				return null;

			return possibleCells.Random(self.World.SharedRandom);
		}
	}
}
