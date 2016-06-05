#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Creates a free duplicate of produced units.")]
	public class ClonesProducedUnitsInfo : ITraitInfo, Requires<ProductionInfo>, Requires<ExitInfo>
	{
		[FieldLoader.Require]
		[Desc("Uses the \"Cloneable\" trait to determine whether or not we should clone a produced unit.")]
		public readonly HashSet<string> CloneableTypes = new HashSet<string>();

		public object Create(ActorInitializer init) { return new ClonesProducedUnits(init, this); }
	}

	public class ClonesProducedUnits : INotifyOtherProduction
	{
		readonly ClonesProducedUnitsInfo info;
		readonly Production production;
		readonly string faction;

		public ClonesProducedUnits(ActorInitializer init, ClonesProducedUnitsInfo info)
		{
			this.info = info;
			production = init.Self.Trait<Production>();
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced)
		{
			// No recursive cloning!
			if (producer.Owner != self.Owner || producer.Info.HasTraitInfo<ClonesProducedUnitsInfo>())
				return;

			var ci = produced.Info.TraitInfoOrDefault<CloneableInfo>();
			if (ci == null || !info.CloneableTypes.Overlaps(ci.Types))
				return;

			production.Produce(self, produced.Info, faction);
		}
	}
}
