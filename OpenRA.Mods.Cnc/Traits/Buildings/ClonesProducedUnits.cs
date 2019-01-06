#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Creates a free duplicate of produced units.")]
	public class ClonesProducedUnitsInfo : ITraitInfo, Requires<ProductionInfo>, Requires<ExitInfo>
	{
		[FieldLoader.Require]
		[Desc("Uses the \"Cloneable\" trait to determine whether or not we should clone a produced unit.")]
		public readonly BitSet<CloneableType> CloneableTypes = default(BitSet<CloneableType>);

		public object Create(ActorInitializer init) { return new ClonesProducedUnits(init, this); }
	}

	public class ClonesProducedUnits : INotifyOtherProduction
	{
		readonly ClonesProducedUnitsInfo info;
		readonly Production[] productionTraits;

		public ClonesProducedUnits(ActorInitializer init, ClonesProducedUnitsInfo info)
		{
			this.info = info;
			productionTraits = init.Self.TraitsImplementing<Production>().ToArray();
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			// No recursive cloning!
			if (producer.Owner != self.Owner || producer.Info.HasTraitInfo<ClonesProducedUnitsInfo>())
				return;

			var ci = produced.Info.TraitInfoOrDefault<CloneableInfo>();
			if (ci == null || !info.CloneableTypes.Overlaps(ci.Types))
				return;

			var factionInit = init.GetOrDefault<FactionInit>();

			// Stop as soon as one production trait successfully produced
			foreach (var p in productionTraits)
			{
				if (!string.IsNullOrEmpty(productionType) && !p.Info.Produces.Contains(productionType))
					continue;

				var inits = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					factionInit ?? new FactionInit(BuildableInfo.GetInitialFaction(produced.Info, p.Faction))
				};

				if (p.Produce(self, produced.Info, productionType, inits))
					return;
			}
		}
	}
}
