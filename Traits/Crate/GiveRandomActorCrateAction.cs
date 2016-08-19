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

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Spawns a random actor with the `EligibleForRandomActorCrate` trait when collected.")]
	class GiveRandomActorCrateActionInfo : CrateActionInfo
	{
		[Desc("Factions that are allowed to trigger this action.")]
		public readonly HashSet<string> ValidFactions = new HashSet<string>();

		[Desc("Override the owner of the newly spawned unit: e.g. Creeps or Neutral")]
		public readonly string Owner = null;

		public override object Create(ActorInitializer init) { return new GiveRandomActorCrateAction(init.Self, this); }
	}

	class GiveRandomActorCrateAction : CrateAction
	{
		readonly Actor self;
		readonly GiveRandomActorCrateActionInfo info;

		readonly IEnumerable<ActorInfo> eligibleActors;

		IEnumerable<ActorInfo> validActors;

		public GiveRandomActorCrateAction(Actor self, GiveRandomActorCrateActionInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;

			eligibleActors = self.World.Map.Rules.Actors.Values.Where(a => a.HasTraitInfo<EligibleForRandomActorCrateInfo>() && !a.Name.StartsWith("^"));
		}

		public bool CanGiveTo(Actor collector)
		{
			if (collector.Owner.NonCombatant)
				return false;

			if (info.ValidFactions.Any() && !info.ValidFactions.Contains(collector.Owner.Faction.InternalName))
				return false;

			var cells = collector.World.Map.FindTilesInCircle(self.Location, 2);

			validActors = eligibleActors.Where(a => validActor(a, cells));

			return validActors.Count() > 0 ? true : false;
		}

		bool validActor(ActorInfo a, IEnumerable<CPos> cells)
		{
			foreach (var c in cells)
			{
				var mi = a.TraitInfoOrDefault<MobileInfo>();
				if (mi != null && mi.CanEnterCell(self.World, self, c))
					return true;
			}
			return false;
		}

		public override int GetSelectionShares(Actor collector)
		{
			if (!CanGiveTo(collector))
				return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			var unit = validActors.Random(self.World.SharedRandom);

			var cells = collector.World.Map.FindTilesInCircle(self.Location, 2);

			foreach (var c in cells)
			{
				var mi = unit.TraitInfoOrDefault<MobileInfo>();
				if (mi != null && mi.CanEnterCell(self.World, self, c))
				{
					var cell = c;
					var td = new TypeDictionary
					{
						new LocationInit(cell),
						new OwnerInit(info.Owner ?? collector.Owner.InternalName)
					};

					collector.World.AddFrameEndTask(w => w.CreateActor(unit.Name, td));

					base.Activate(collector);

					return;
				}
			}
		}
	}
}
