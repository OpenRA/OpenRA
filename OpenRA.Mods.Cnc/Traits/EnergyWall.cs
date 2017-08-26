#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Will open and be passable for actors that appear friendly when there are no enemies in range.")]
	public class EnergyWallInfo : ITemporaryBlockerInfo, IObservesVariablesInfo, IRulesetLoaded, Requires<BuildingInfo>
	{
		[FieldLoader.Require]
		[WeaponReference]
		[Desc("The weapon to attack units on top of the wall with when activated.")]
		public readonly string Weapon = null;

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition to activate this trait.")]
		public readonly BooleanExpression ActiveCondition = null;

		public object Create(ActorInitializer init) { return new EnergyWall(init.Self, this); }

		public WeaponInfo WeaponInfo { get; private set; }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			WeaponInfo weaponInfo;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weaponInfo;
		}
	}

	public class EnergyWall : IObservesVariables, ITick, ITemporaryBlocker, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		public readonly EnergyWallInfo Info;
		public readonly IEnumerable<CPos> Footprint;

		readonly Building building;
		IEnumerable<CPos> blockedPositions;

		// Initial state is active to match Building adding the influence to the ActorMap
		// This will be updated by ConditionsChanged at actor creation.
		bool active = true;

		public EnergyWall(Actor self, EnergyWallInfo info)
		{
			Info = info;

			building = self.Trait<Building>();
			blockedPositions = building.Info.Tiles(self.Location);
			Footprint = blockedPositions;
		}

		public virtual IEnumerable<VariableObserver> GetVariableObservers()
		{
			if (Info.ActiveCondition != null)
				yield return new VariableObserver(ActiveConditionChanged, Info.ActiveCondition.Variables);
		}

		void ActiveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			if (Info.ActiveCondition == null)
				return;

			var wasActive = active;
			active = Info.ActiveCondition.Evaluate(conditions);

			if (!wasActive && active)
				self.World.ActorMap.AddInfluence(self, building);
			else if (wasActive && !active)
				self.World.ActorMap.RemoveInfluence(self, building);
		}

		void ITick.Tick(Actor self)
		{
			if (!active)
				return;

			foreach (var loc in blockedPositions)
			{
				var blockers = self.World.ActorMap.GetActorsAt(loc).Where(a => !a.IsDead && a != self);
				foreach (var blocker in blockers)
					Info.WeaponInfo.Impact(Target.FromActor(blocker), self, Enumerable.Empty<int>());
			}
		}

		bool ITemporaryBlocker.IsBlocking(Actor self, CPos cell)
		{
			return active && blockedPositions.Contains(cell);
		}

		bool ITemporaryBlocker.CanRemoveBlockage(Actor self, Actor blocking)
		{
			return !active;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			blockedPositions = Footprint;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			blockedPositions = Enumerable.Empty<CPos>();
		}
	}
}
