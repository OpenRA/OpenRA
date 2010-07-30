#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Crates
{
	class GiveUnitCrateActionInfo : CrateActionInfo
	{
		[ActorReference]
		public readonly string Unit = null;

		public override object Create(ActorInitializer init) { return new GiveUnitCrateAction(init.self, this); }
	}

	class GiveUnitCrateAction : CrateAction
	{
		GiveUnitCrateActionInfo Info;
		public GiveUnitCrateAction(Actor self, GiveUnitCrateActionInfo info)
			: base(self, info) { Info = info; }

		public override int GetSelectionShares(Actor collector)
		{
			var valuedInfo = Rules.Info[Info.Unit].Traits.Get<ValuedInfo>();
			return valuedInfo.Owner.Contains(collector.Owner.Country.Race)
				? base.GetSelectionShares(collector)
				: 0;		// this unit is not buildable by the collector's country, so
							// don't give them free ones either.
		}

		public override void Activate(Actor collector)
		{
			var location = ChooseEmptyCellNear(collector);
			if (location != null)
				collector.World.AddFrameEndTask(
					w => w.Add(new Actor(w, Info.Unit, location.Value, collector.Owner)));

			base.Activate(collector);
		}

		int2? ChooseEmptyCellNear(Actor a)
		{
			// hack: use `a`'s movement capability.
			var move = a.traits.Get<ITeleportable>();
			var loc = a.Location;

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if (move.CanEnterCell(loc + new int2(i, j)))
						return loc + new int2(i, j);

			return null;	// nowhere we can place this -- so the crate will do nothing.
		}
	}
}
