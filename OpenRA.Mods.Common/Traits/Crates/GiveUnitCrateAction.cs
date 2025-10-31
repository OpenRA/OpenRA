#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawns units when collected.")]
	class GiveUnitCrateActionInfo : CrateActionInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("The list of units to spawn.")]
		public readonly string[] Units = [];

		[Desc("Factions that are allowed to trigger this action.")]
		public readonly HashSet<string> ValidFactions = [];

		[Desc("Override the owner of the newly spawned unit: e.g. Creeps or Neutral")]
		public readonly string Owner = null;

		public override object Create(ActorInitializer init) { return new GiveUnitCrateAction(init.Self, this); }
	}

	class GiveUnitCrateAction : CrateAction
	{
		readonly Actor self;
		readonly GiveUnitCrateActionInfo info;

		public GiveUnitCrateAction(Actor self, GiveUnitCrateActionInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;
			if (info.Units.Length == 0)
				throw new YamlException(
					"A GiveUnitCrateAction does not specify any units to give. " +
					"This might be because the yaml is referring to 'Unit' rather than 'Units'.");
		}

		public bool CanGiveTo(Actor collector)
		{
			if (collector.Owner.NonCombatant)
				return false;

			if (info.ValidFactions.Count > 0 && !info.ValidFactions.Contains(collector.Owner.Faction.InternalName))
				return false;

			var pathFinder = collector.World.WorldActor.TraitOrDefault<IPathFinder>();
			var locomotorsByName = collector.World.WorldActor.TraitsImplementing<Locomotor>().ToDictionary(l => l.Info.Name);
			foreach (var unit in info.Units)
			{
				// avoid dumping tanks in the sea, and ships on dry land.
				if (!GetSuitableCells(collector.Location, unit, pathFinder, locomotorsByName).Any())
					return false;
			}

			return true;
		}

		public override int GetSelectionShares(Actor collector)
		{
			if (!CanGiveTo(collector))
				return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var pathFinder = w.WorldActor.TraitOrDefault<IPathFinder>();
				var locomotorsByName = w.WorldActor.TraitsImplementing<Locomotor>().ToDictionary(l => l.Info.Name);
				foreach (var unit in info.Units)
				{
					var location = ChooseEmptyCellNear(collector, unit, pathFinder, locomotorsByName);
					if (location != null)
					{
						var actor = w.CreateActor(unit,
						[
							new LocationInit(location.Value),
							new OwnerInit(info.Owner ?? collector.Owner.InternalName)
						]);

						// Set the subcell and make sure to crush actors beneath.
						var positionable = actor.OccupiesSpace as IPositionable;
						positionable.SetPosition(actor, location.Value, positionable.GetAvailableSubCell(location.Value, ignoreActor: actor));
					}
				}
			});

			base.Activate(collector);
		}

		IEnumerable<CPos> GetSuitableCells(CPos near, string unitName, IPathFinder pathFinder, Dictionary<string, Locomotor> locomotorsByName)
		{
			var actorRules = self.World.Map.Rules.Actors[unitName];

			Locomotor locomotor = null;
			if (pathFinder != null)
			{
				var locomotorName = actorRules.TraitInfoOrDefault<MobileInfo>()?.Locomotor;
				locomotor = locomotorName != null ? locomotorsByName[locomotorName] : null;
			}

			var ip = actorRules.TraitInfo<IPositionableInfo>();
			for (var i = -1; i <= 1; i++)
			{
				for (var j = -1; j <= 1; j++)
				{
					var cell = near + new CVec(i, j);
					if (ip.CanEnterCell(self.World, self, cell) &&
						(locomotor == null || pathFinder.PathMightExistForLocomotorBlockedByImmovable(locomotor, cell, near)))
						yield return near + new CVec(i, j);
				}
			}
		}

		CPos? ChooseEmptyCellNear(Actor a, string unit, IPathFinder pathFinder, Dictionary<string, Locomotor> locomotorsByName)
		{
			return GetSuitableCells(a.Location, unit, pathFinder, locomotorsByName)
				.Cast<CPos?>()
				.RandomOrDefault(self.World.SharedRandom);
		}
	}
}
