#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Applies an upgrade to actors within a specified range.")]
	public class UpgradeActorsNearInfo : ITraitInfo
	{
		[Desc("The upgrades to grant.")]
		public readonly string[] Upgrades = { };

		[Desc("The range to search for actors to upgrade.")]
		public readonly WRange Range = WRange.FromCells(3);

		[Desc("What diplomatic stances are affected.")]
		public readonly Stance ValidStances = Stance.Ally;

		[Desc("Grant the upgrades apply to this actor.")]
		public readonly bool AffectsParent = false;

		public readonly string EnableSound = null;
		public readonly string DisableSound = null;

		public object Create(ActorInitializer init) { return new UpgradeActorsNear(init.self, this); }
	}

	public class UpgradeActorsNear : ITick, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOtherProduction
	{
		readonly UpgradeActorsNearInfo info;
		readonly Actor self;

		int proximityTrigger;
		WPos cachedPosition;
		WRange cachedRange;
		WRange desiredRange;

		bool cachedDisabled = true;

		public UpgradeActorsNear(Actor self, UpgradeActorsNearInfo info)
		{
			this.info = info;
			this.self = self;
			cachedRange = info.Range;
		}

		public void AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, ActorEntered, ActorExited);
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
		}

		public void Tick(Actor self)
		{
			var disabled = self.IsDisabled();

			if (cachedDisabled != disabled)
			{
				Sound.Play(disabled ? info.DisableSound : info.EnableSound, self.CenterPosition);
				desiredRange = disabled ? WRange.Zero : info.Range;
				cachedDisabled = disabled;
			}

			if (self.CenterPosition != cachedPosition || desiredRange != cachedRange)
			{
				cachedPosition = self.CenterPosition;
				cachedRange = desiredRange;
				self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, cachedPosition, cachedRange);
			}
		}

		void ActorEntered(Actor a)
		{
			if (a.Destroyed)
				return;

			if (a == self && !info.AffectsParent)
				return;

			var stance = self.Owner.Stances[a.Owner];
			if (!info.ValidStances.HasFlag(stance))
				return;

			var um = a.TraitOrDefault<UpgradeManager>();
			if (um != null)
				foreach (var u in info.Upgrades)
					um.GrantUpgrade(a, u, this);
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced)
		{
			// Work around for actors produced within the region not triggering until the second tick
			if ((produced.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= info.Range.Range * info.Range.Range)
			{
				var stance = self.Owner.Stances[produced.Owner];
				if (!info.ValidStances.HasFlag(stance))
					return;

				var um = produced.TraitOrDefault<UpgradeManager>();
				if (um != null)
					foreach (var u in info.Upgrades)
						um.GrantTimedUpgrade(produced, u, 1);
			}
		}

		void ActorExited(Actor a)
		{
			if (a == self || a.Destroyed)
				return;

			var stance = self.Owner.Stances[a.Owner];
			if (!info.ValidStances.HasFlag(stance))
				return;

			var um = a.TraitOrDefault<UpgradeManager>();
			if (um != null)
				foreach (var u in info.Upgrades)
					um.RevokeUpgrade(a, u, this);
		}
	}
}
