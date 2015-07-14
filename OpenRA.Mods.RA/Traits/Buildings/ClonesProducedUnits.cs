#region Copyright & License Information
/*
  * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
  * This file is part of OpenRA, which is free software. It is made
  * available to you under the terms of the GNU General Public License
  * as published by the Free Software Foundation. For more information,
  * see COPYING.
  */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Creates a free duplicate of produced units.")]
	public class ClonesProducedUnitsInfo : ITraitInfo, Requires<ProductionInfo>, Requires<ExitInfo>
	{
		[FieldLoader.Require]
		[Desc("Uses the \"Cloneable\" trait to determine whether or not we should clone a produced unit.")]
		public readonly string[] CloneableTypes = { };

		public object Create(ActorInitializer init) { return new ClonesProducedUnits(init, this); }
	}

	public class ClonesProducedUnits : INotifyOtherProduction
	{
		readonly ClonesProducedUnitsInfo info;
		readonly Production production;
		readonly string race;

		public ClonesProducedUnits(ActorInitializer init, ClonesProducedUnitsInfo info)
		{
			this.info = info;
			production = init.Self.Trait<Production>();
			race = init.Contains<RaceInit>() ? init.Get<RaceInit, string>() : init.Self.Owner.Country.InternalName;
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced)
		{
			// No recursive cloning!
			if (producer.Owner != self.Owner || producer.HasTrait<ClonesProducedUnits>())
				return;

			var ci = produced.Info.Traits.GetOrDefault<CloneableInfo>();
			if (ci == null || !info.CloneableTypes.Intersect(ci.Types).Any())
				return;

			production.Produce(self, produced.Info, race);
		}
	}
}
