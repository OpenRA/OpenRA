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
		public readonly string Unit = null;
		public readonly string Owner = null;

		public override object Create(ActorInitializer init) { return new GiveUnitCrateAction(init.self, this); }
	}

	class GiveUnitCrateAction : CrateAction
	{
		GiveUnitCrateActionInfo Info;

		public GiveUnitCrateAction(Actor self, GiveUnitCrateActionInfo info)
			: base(self, info) { Info = info; }

		public override int GetSelectionShares(Actor collector)
		{
			var bi = Rules.Info[Info.Unit].Traits.GetOrDefault<BuildableInfo>();

			// this unit is not buildable by the collector's country, so
			// don't give them free ones either.
			if (Info.Owner == null && bi != null && !bi.Owner.Contains(collector.Owner.Country.Race)) return 0;

			// avoid dumping tanks in the sea, and ships on dry land.
			if (!GetSuitableCells(collector.Location).Any()) return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			var location = ChooseEmptyCellNear(collector);
			if (location != null)
				collector.World.AddFrameEndTask(
					w => w.CreateActor(Info.Unit, new TypeDictionary
					{
						new LocationInit( location.Value ),
						new OwnerInit( Info.Owner ?? collector.Owner.InternalName )
					}));

			base.Activate(collector);
		}

		IEnumerable<int2> GetSuitableCells(int2 near)
		{
			var mi = Rules.Info[Info.Unit].Traits.Get<MobileInfo>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if (mi.CanEnterCell(self.World, self.Owner, near + new int2(i, j), null, true))
						yield return near + new int2(i, j);
		}

		int2? ChooseEmptyCellNear(Actor a)
		{
			var possibleCells = GetSuitableCells(a.Location).ToArray();
			if (possibleCells.Length == 0)
				return null;

			return possibleCells.Random(self.World.SharedRandom);
		}
	}
}
